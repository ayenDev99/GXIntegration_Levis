using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GXIntegration.Properties
{
	public class GXConfig
	{
		public string MainDbConnection { get; set; }
		public string CountryCode { get; set; }
		public string Delimiter { get; set; }

		public int ReprocessMinutes { get; set; } = 5;

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

			var delayElem = doc.Root.Element("ReprocessMinutes");
			if (delayElem != null && int.TryParse(delayElem.Value, out int delay))
			{
				config.ReprocessMinutes = delay;
			}

			return config;
		}
	}
}
