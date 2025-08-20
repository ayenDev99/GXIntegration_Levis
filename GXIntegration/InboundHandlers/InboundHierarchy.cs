using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;
using System.IO;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundHierarchy
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		private List<Dictionary<string, string>> BuildHierarchyCollection(string filePath)
		{
			var result = new List<Dictionary<string, string>>();

			try
			{
				var lines = File.ReadAllLines(filePath);
				if (lines.Length == 0) return result;

				// First line = header
				var headers = lines[0].Split('^');

				foreach (var line in lines.Skip(1))
				{
					var parts = line.Split('^');

					var rowDict = new Dictionary<string, string>();

					for (int i = 0; i < headers.Length; i++)
					{
						string header = headers[i];
						string value = (i < parts.Length ? parts[i] : string.Empty);
						rowDict[header] = value;
					}

					result.Add(rowDict);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in BuildHierarchyCollection: {ex.Message}");
			}

			return result;
		}



	}
}
