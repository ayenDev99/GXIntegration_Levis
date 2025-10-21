using System;
using System.Xml.Linq;

namespace GXIntegration.Properties
{
	public class GXConfig
	{
		public string MainDbConnection { get; set; }
		public string CountryCode { get; set; }
		public string Delimiter { get; set; }
		public int OutApiAutoProcessTime { get; set; }
		public string OutEodAutoProcessTime { get; set; }
		public int InAutoDownloadProcessTime { get; set; }

		public static GXConfig Load(string filePath)
		{
			var config = new GXConfig();

			var doc = XDocument.Load(filePath);
			var root = doc.Root;

			if (root == null)
				throw new Exception("Config XML root is null");

			var mainDbNode = root.Element("MainDbConnection");
			if (mainDbNode != null)
				config.MainDbConnection = mainDbNode.Value;

			var countryCodeNode = root.Element("CountryCode");
			if (countryCodeNode != null)
				config.CountryCode = countryCodeNode.Value;

			var delimiterNode = root.Element("Delimiter");
			if (delimiterNode != null)
				config.Delimiter = delimiterNode.Value;

			var apiDelayElem = doc.Root.Element("OutApiAutoProcessTime");
			if (apiDelayElem != null && int.TryParse(apiDelayElem.Value, out int apiDelay))
			{
				config.OutApiAutoProcessTime = apiDelay;
			}

			// --- Outbound EOD time (HH:mm:ss) ---
			var eodTimeElem = root.Element("OutEodAutoProcessTime");
			if (eodTimeElem != null)
			{
				config.OutEodAutoProcessTime = eodTimeElem.Value.Trim();
			}
			else
			{
				throw new Exception("OutEodAutoProcessTime not found in config.xml");
			}

			var sftpDelayElem = doc.Root.Element("InAutoDownloadProcessTime");
			if (sftpDelayElem != null && int.TryParse(sftpDelayElem.Value, out int sftpDelay))
			{
				config.InAutoDownloadProcessTime = sftpDelay;
			}

			return config;
		}
	}
}
