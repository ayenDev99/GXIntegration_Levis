using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundPrice
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunPriceSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			try
			{
				Logger.Log("");
				Logger.Log("**************************************************************************");
				Logger.Log(">>> Starting INBOUND PRICE Sync Process...");
				Logger.Log("**************************************************************************");

				string fileNameFormat = "LSPI_PRTARI_*.*";

				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) return;

				foreach (string file in files)
				{
					var result = BuildPriceCollection(file);
					Logger.Log($"Price file loaded. Rows found: {result.Count}");

					foreach (var row in result)
					{
						foreach (var kv in row)
						{
							Console.WriteLine($"{kv.Key}: {kv.Value}");
						}

					
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
				Console.WriteLine($"Error in RunItemSyncAsync: {ex.Message}");
				return;
			}
		}


		private List<Dictionary<string, string>> BuildPriceCollection(string filePath)
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

						if (fields == null || fields.Length == 0)
							continue;

						var rowDict = new Dictionary<string, string>();

						// Map only relevant indices
						if (fields.Length > 0) rowDict["CountryCode"] = fields[0].Trim();
						if (fields.Length > 2) rowDict["ItemCode"] = fields[2].Trim();
						if (fields.Length > 6) rowDict["UOM"] = fields[6].Trim();
						if (fields.Length > 7) rowDict["Currency"] = fields[7].Trim();
						if (fields.Length > 8) rowDict["Price"] = fields[8].Trim();
						if (fields.Length > 9) rowDict["StartDate"] = fields[9].Trim();
						if (fields.Length > 10) rowDict["Brand"] = fields[10].Trim();
						if (fields.Length > 11) rowDict["Division"] = fields[11].Trim();
						if (fields.Length > 15) rowDict["System"] = fields[15].Trim();

						result.Add(rowDict);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in BuildPriceCollection: {ex.Message}");
			}

			return result;
		}

	}
}
