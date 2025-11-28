using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GXIntegration_Levis.OutboundHandlers
{
	public class GlobalOutbound
	{
		public static readonly string NsIXRetail = "http://www.nrf-arts.org/IXRetail/namespace/";
		public static readonly string NsDtv = "http://www.datavantagecorp.com/xstore/";
		public static readonly string NsXsi = "http://www.w3.org/2001/XMLSchema-instance";
		public static void WriteCDataElement(XmlWriter writer, string elementName, string content)
		{
			writer.WriteStartElement(elementName);
			writer.WriteCData(content ?? "");
			writer.WriteEndElement();
		}

		public static void WriteCDataElement(XmlWriter writer, string prefix, string localName, string ns, string content)
		{
			writer.WriteStartElement(prefix, localName, ns);
			writer.WriteCData(content ?? "");
			writer.WriteEndElement();
		}

		public static void WritePosTransactionProperties(XmlWriter writer, string code, string value)
		{
			writer.WriteStartElement("dtv", "PosTransactionProperties", NsDtv);

			writer.WriteStartElement("dtv", "PosTransactionPropertyCode", NsDtv);
			writer.WriteCData(code);
			writer.WriteEndElement();

			writer.WriteStartElement("dtv", "PosTransactionPropertyValue", NsDtv);
			writer.WriteCData(value);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		public static void WriteMerchandiseHierarchy(XmlWriter writer, string level, string value)
		{
			writer.WriteStartElement("MerchandiseHierarchy");
			writer.WriteAttributeString("Level", level);
			writer.WriteCData(value ?? "");
			writer.WriteEndElement();
		}

		public static void WriteLineItemProperty(XmlWriter writer, string code, string type, string value)
		{
			writer.WriteStartElement("dtv", "LineItemProperty", NsDtv);

			WriteCDataElement(writer, "dtv", "LineItemPropertyCode", NsDtv, code);
			WriteCDataElement(writer, "dtv", "LineItemPropertyType", NsDtv, type);
			WriteCDataElement(writer, "dtv", "LineItemPropertyValue", NsDtv, value);

			writer.WriteEndElement(); // </dtv:LineItemProperty>
		}

		public static string FormatDate(DateTimeOffset? date, bool includeTime = false)
		{
			if (!date.HasValue) return "";

			if (includeTime)
			{
				return date.Value.ToString("yyyy-MM-ddTHH:mm:ss.ff");
			}
			else
			{
				return date.Value.ToString("yyyy-MM-dd");
			}
		}

		public static IEnumerable<IGrouping<string, T>> GroupBySafe<T>(IEnumerable<T> source, Func<T, string> keySelector)
		{
			return source
				.GroupBy(i => keySelector(i) ?? "UNKNOWN")
				.OrderBy(g =>
				{
					int n;
					return int.TryParse(g.Key, out n) ? n : int.MaxValue;
				});
		}

		public static async Task UploadToSftpAsync()
		{
			Logger.LogOutbound($"[EOD] Uploading files to the SFTP server...");

			var sftpConfig = GlobalHelper.LoadSftpConnection();

			if (!sftpConfig.TryGetValue("Host", out string host) ||
				!sftpConfig.TryGetValue("Port", out string port) ||
				!sftpConfig.TryGetValue("Username", out string username) ||
				!sftpConfig.TryGetValue("Password", out string password))
			{
				MessageBox.Show("[ERROR] SFTP configuration is missing. Please navigate to the 'Configuration SFTP' tab to set up the SFTP connection.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string localDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");

			var directoryMap = GlobalHelper.LoadPathMap("OutSFTPPath");

			await Task.Run(() =>
			{
				try
				{
					if (!Directory.Exists(localDirectory))
					{
						Logger.LogOutbound($"[EOD] Local directory does not exist: {localDirectory}");
						return;
					}

					var files = Directory.GetFiles(localDirectory, "*.*")
						.Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
									f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
						.ToArray();

					if (!files.Any())
					{
						Logger.LogOutbound("[EOD] No outbound files found to upload.");
						return;
					}

					int portNumber = Convert.ToInt32(port);
					using (var sftp = new SftpClient(host, portNumber, username, password))
					{
						sftp.Connect();

						foreach (var filePath in files)
						{
							try
							{
								string fileName = Path.GetFileName(filePath);

								// Pick the correct remote directory based on filename
								string remoteDirectory = GetRemoteDirectory(fileName, directoryMap);

								if (!sftp.Exists(remoteDirectory))
									sftp.CreateDirectory(remoteDirectory);

								string remotePath = remoteDirectory + fileName;

								using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
								{
									sftp.UploadFile(fileStream, remotePath, true);
								}

								ArchiveFile(filePath, localDirectory);

								Logger.LogOutbound($"[EOD] Successfully uploaded file to SFTP Path : {remoteDirectory}.");
							}
							catch (Exception ex)
							{
								Logger.LogOutbound($"Error handling file '{Path.GetFileName(filePath)}': {ex}");
							}
						}

						sftp.Disconnect();
					}
				}
				catch (Exception ex)
				{
					Logger.LogError($"SFTP Upload failed: {ex}");
				}
			});
		}

		private static string GetRemoteDirectory(string fileName, Dictionary<string, string> directoryMap)
		{
			foreach (var kvp in directoryMap)
			{
				if (kvp.Key != "DEFAULT" &&
					fileName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return kvp.Value;
				}
			}

			return directoryMap.ContainsKey("DEFAULT") ? directoryMap["DEFAULT"] : "/IN/OTHERS/";
		}

		private static void ArchiveFile(string filePath, string localDirectory)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
				{
					Logger.LogOutbound($"[ARCHIVE] File not found: {filePath}");
					return;
				}

				string fileName = Path.GetFileName(filePath);

				var match = Regex.Match(fileName, @"_(\d{8})\d{6}\.(xml|txt)$", RegexOptions.IgnoreCase);
				if (!match.Success)
				{
					Logger.LogOutbound($"[ARCHIVE] Invalid date format in filename: {fileName}");
					return;
				}

				string datePart = match.Groups[1].Value; // "08102025"
				if (!DateTime.TryParseExact(datePart, "ddMMyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
				{
					Logger.LogOutbound($"[ARCHIVE] Failed to parse date from filename: {fileName}");
					return;
				}

				string formattedDate = parsedDate.ToString("yyyyMMdd");
				string archiveRootDir = Path.Combine(localDirectory, "ARCHIVE");
				string archiveDateDir = Path.Combine(archiveRootDir, formattedDate);

				Directory.CreateDirectory(archiveDateDir);

				string archivedPath = Path.Combine(archiveDateDir, fileName);

				if (File.Exists(archivedPath))
				{
					Logger.LogOutbound($"[ARCHIVE] Overwriting existing file: {archivedPath}");
					File.Delete(archivedPath);
				}

				File.Move(filePath, archivedPath);
				//Logger.LogOutbound($"[ARCHIVE] Moved file to: {archivedPath}");
			}
			catch (Exception ex)
			{
				Logger.LogError($"[ARCHIVE] Error archiving file: {ex.Message}");
			}
		}

	}
}
