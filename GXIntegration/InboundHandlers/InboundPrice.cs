using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;


namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundPrice
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunPriceSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			XDocument config = XDocument.Load("config.xml");


			try
			{
				Logger.Log("[INBOUND] Starting PRICE Sync Process...");

				string fileNameFormat = "LSPI_PRTARI_*.*";
				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0)
				{
					Logger.Log("[INBOUND] No price files found.");
					return;
				}

				foreach (string file in files)
				{
					var result = BuildPriceCollection(file);
					Logger.Log($"Price file loaded. Rows found: {result.Count}");

					// Load SBS_NO from config
					var sbsNos = config
						.Descendants("Subsidiary")
						.Select(x => int.Parse(x.Value))
						.ToList();

					// Process each subsidiary
					foreach (var sbsNo in sbsNos)
					{
						Logger.Log($"[INBOUND - PRICE] Processing for SBS_NO: {sbsNo}");

						// Process each row from the file
						foreach (var row in result)
						{
							// Log each key/value in the row (optional for debugging)
							foreach (var kv in row)
							{
								//Logger.Log($"{kv.Key}: {kv.Value}");
							}

							// Prepare filters
							var baseFilters = new Dictionary<string, object>
								{
									{ "DESCRIPTION1", row["ItemCode"] },
									{ "ACTIVE", 1 },
									{ "PRICE_LVL_NAME", "LSPC" }
								};

							var filters = new Dictionary<string, object>(baseFilters)
								{
									{ "SBS_NO", sbsNo }
								};

							Logger.Log($"[INBOUND - PRICE] Fetching item data for SBS_NO {sbsNo} | Item Code : {row["ItemCode"]}");

							var results = await repository.GetInboundItemsAsync(filters);
							var resultList = results?.ToList() ?? new List<dynamic>();

							// Log count
							Logger.Log($"[INBOUND - PRICE] Item count: {resultList.Count}");

							//string jsonResult = JsonConvert.SerializeObject(results, Formatting.Indented); // Pretty print
							//Logger.Log("Inbound items result:\n" + jsonResult);

							foreach (var item in resultList)
							{
								var price_lvl_sid = item.ACTIVE_PRICE_LVL_SID;
								var sbs_sid = item.SBS_SID;
								Logger.Log($"PRICE_LVL_SID : {sbs_sid}");

								if (results != null)
								{
									var newAjustmentData = await createRpsAdjustment(session, item);
									string adjusment_sid = JObject.Parse(newAjustmentData)?["data"]?[0]?["sid"]?.ToString();

									await createRpsAdjItem(session, item, row, adjusment_sid);
								}
								else
								{
									Logger.Log("No inbound items found or an error occurred.");
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"Error in RunItemSyncAsync: {ex.Message}");
				return;
			}
		}

		private async Task<string> createRpsAdjustment(string session, dynamic item)
		{
			Logger.Log($"[INBOUND - PRICE]		[CREATE] ADJUSTMENT");

			string price_lvl_sid = item?.ACTIVE_PRICE_LVL_SID?.ToString();
			string sbs_sid = item?.SBS_SID?.ToString();

			var adjustmentPayload = new Dictionary<string, object>
			{
				["adjtype"] = 1,
				//["clerksid"] = "RProPrismWeb",
				//["creatingdoctype"] = "RProPrismWeb",
				["originapplication"] = "RProPrismWeb",
				//["origstoresid"] = "RProPrismWeb",
				["pricelvlsid"] = price_lvl_sid,
				["sbssid"] = sbs_sid,
				["status"] = 3,
			};

			// NOTE: ADD REASON SID FROM RPS.PREF_REASON

			// Call API to CREATE RPS.ADJUSTMENT
			string endpointCreate = "/api/backoffice/adjustment";
			var payload = new { data = new[] { adjustmentPayload } };
			string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, endpointCreate
									, json
									, out bool issuccessful
									, "POST"
									, 1
									);

			return responseJson;

		}

		private async Task<string> createRpsAdjItem(string session, dynamic item, dynamic fileRowData, string adjustmentSid)
		{
			Logger.Log($"[INBOUND - PRICE]		[CREATE] ADJ_ITEM");

			string item_sid = item?.SID?.ToString();
			string sbs_sid = item?.SBS_SID?.ToString();
			decimal adjValue = 0m;
			if (!decimal.TryParse(fileRowData["Price"], out adjValue))
			{
				Logger.Log($"[WARNING] Could not parse Price '{fileRowData["Price"]}' to decimal. Defaulting to 0.");
			}
			var adjustmentPayload = new Dictionary<string, object>
			{
				["adjsid"] = adjustmentSid,
				["itemsid"] = item_sid,
				["originapplication"] = "RProPrismWeb",
				["rowversion"] = 1,
				["adjvalue"] = adjValue
			};

			// Call API to CREATE RPS.ADJUSTMENT
			string endpointCreate = $"/api/backoffice/adjustment/{adjustmentSid}/adjitem";
			var payload = new { data = new[] { adjustmentPayload } };
			string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, endpointCreate
									, json
									, out bool issuccessful
									, "POST"
									, 1
									);

			return responseJson;
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
				Logger.Log($"Error in BuildPriceCollection: {ex.Message}");
			}

			return result;
		}

	}
}
