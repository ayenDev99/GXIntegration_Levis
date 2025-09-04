using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundASN
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunASNSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			try
			{			
				Logger.Log("[INBOUND] Starting ASN Sync Process...");

				string fileNameFormat = "LSPI_PRTARI_*.*";

				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) return;

				foreach (string file in files)
				{
					var result = BuildASNCollection(file);
					Logger.Log($"ASN file loaded. Rows found: {result.Count}");

					foreach (var row in result)
					{
						foreach (var kv in row)
						{
							Console.WriteLine($"{kv.Key}: {kv.Value}");
						}

						//var payload = new
						//{
						//	data = new[]
						//				{
						//					new
						//					{
						//						OriginApplication = "RProPrismWeb",
						//					}
						//				}
						//};

						//var json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);

						//Console.WriteLine("Payload:");
						//Console.WriteLine(json);
						////Logger.Log("Payload built:\n" + json);

						//string responseJson = GlobalInbound.CallPrismAPI(
						//						session,
						//						prismAddress,
						//						"/api/backoffice/inventory?action=InventorySaveItems",
						//						json,
						//						out bool issuccessful,
						//						"POST");

						////string responseJson = globalInbound.CallPrismAPI(session, prismAddress, "/api/backoffice/inventory?action=InventorySaveItems", json, out bool issuccessful, "POST");
						//Console.WriteLine("Response: " + responseJson);
					}
				}

			}
			catch (Exception ex)
			{
				Logger.Log("Error in RunASNSyncAsync: {ex.Message}");
				return;
			}
		}

		private List<Dictionary<string, string>> BuildASNCollection(string filePath)
		{
			var result = new List<Dictionary<string, string>>();

			try
			{
				using (var parser = new TextFieldParser(filePath))
				{
					parser.TextFieldType = FieldType.Delimited;
					parser.SetDelimiters("{^^}");
					parser.HasFieldsEnclosedInQuotes = true;

					while (!parser.EndOfData)
					{
						string[] fields = parser.ReadFields();

						if (fields == null || fields.Length == 0 || fields.All(f => string.IsNullOrWhiteSpace(f)))
							continue;

						// Trim all fields
						fields = fields.Select(f => f.Trim()).ToArray();

						var rowDict = new Dictionary<string, string>();

						// Corrected field indices based on sample data
						if (fields.Length > 0) rowDict["CountryCode"] = fields[0];
						if (fields.Length > 2) rowDict["ItemCode"] = fields[2];
						if (fields.Length > 16) rowDict["Currency"] = fields[16];
						if (fields.Length > 17) rowDict["UOM"] = fields[17];
						if (fields.Length > 18) rowDict["Price"] = fields[18];
						if (fields.Length > 23) rowDict["StartDate"] = fields[23];
						if (fields.Length > 10) rowDict["Brand"] = fields[10];
						if (fields.Length > 11) rowDict["Division"] = fields[11];
						if (fields.Length > 15) rowDict["System"] = fields[15];

						result.Add(rowDict);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"Error in BuildPriceCollection for file '{filePath}': {ex}");
			}

			return result;
		}


	}
}
