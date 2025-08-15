using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.OutboundHandlers
{
	public class BulkXmlSender
	{
		public static async Task SendAllXmlFilesAsync(string folderPath)
		{
			string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");

			if (xmlFiles.Length == 0)
			{
				Console.WriteLine("⚠️ No XML files found in the folder.");
				return;
			}

			Console.WriteLine($"📦 Found {xmlFiles.Length} XML files. Sending...");

			foreach (string filePath in xmlFiles)
			{
				try
				{
					string rawXml = File.ReadAllText(filePath);
					bool success = await XmlApiSender.SendXmlAsync(rawXml);

					if (success)
					{
						Console.WriteLine($"✅ Sent: {Path.GetFileName(filePath)}");
					}
					else
					{
						Console.WriteLine($"❌ Failed: {Path.GetFileName(filePath)}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"🚨 Error reading {filePath}: {ex.Message}");
				}
			}

			Console.WriteLine("✅ Bulk sending complete.");
		}
	}
}
