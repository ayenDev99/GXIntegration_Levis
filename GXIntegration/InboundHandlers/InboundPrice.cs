using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
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
			try
			{
				Logger.Log("[INBOUND - PRICE] Starting PRICE Sync Process...");

				string fileNameFormat = "LSPI_PRTARI_*.*";
				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0)
				{
					Logger.Log("[INBOUND - PRICE] No price files found.");
				}

				foreach (string data in files)
				{
					var result = BuildPriceCollection(data);
					await processPriceSyncAsync(result, repository, session, false);
				}

				await reprocessPriceDbSyncAsync(repository, session);

			}
			catch (Exception ex)
			{
				Logger.Log($"Error in RunItemSyncAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
				return;
			}
		}

		private async Task processPriceSyncAsync(dynamic result, dynamic repository, string session, dynamic isReprocess)
		{
			XDocument config = XDocument.Load("config.xml");

			try
			{
				// Load SBS_NO from config
				var sbsNos = config
					.Descendants("Subsidiary")
					.Select(x => int.Parse(x.Value))
					.ToList();

				// Process each subsidiary
				foreach (var sbsNo in sbsNos)
				{
					Logger.Log($"[INBOUND - PRICE] Processing for SBS_NO: {sbsNo}");

					// Process each row from the data
					foreach (var row in result)
					{
						try
						{
							//Log each key/value in the row (optional for debugging)
							//foreach (var kv in row) { Logger.Log($"{kv.Key}: {kv.Value}"); }

							string effectivityDateStr = row["EffectivityDate"];
							DateTime effectivityDate = DateTime.ParseExact(effectivityDateStr, "yyyyMMdd", null);
							DateTime currentDate = DateTime.UtcNow.Date;

							// Get PriceLevel value from config.xml


							// Prepare filters
							var baseFilters = new Dictionary<string, object>
									{
										{ "DESCRIPTION1", row["ProductCode"] },
										{ "ACTIVE", 1 },
										{ "PRICE_LVL_NAME", "LSPC" }
									};

							var filters = new Dictionary<string, object>(baseFilters)
									{
										{ "SBS_NO", sbsNo }
									};
							var results = await repository.GetInboundItemsAsync(filters);
							var resultList = results as List<dynamic> ?? new List<dynamic>();

							if (effectivityDate <= currentDate)
							{
								Logger.Log("[INBOUND - PRICE] Effectivity Date is valid (≤ current system date).");
								Logger.Log($"[INBOUND - PRICE] Fetching item data for SBS_NO {sbsNo} | Item Code : {row["ProductCode"]}");

								if (resultList.Count == 0)
								{
									Logger.Log("No results returned from GetInboundItemsAsync.");
									continue;
								}

								Logger.Log($"[INBOUND - PRICE] Item count: {resultList.Count}");

								string jsonResult = JsonConvert.SerializeObject(resultList, Formatting.Indented);
								//Logger.Log("Inbound items result:\n" + jsonResult);

								foreach (var item in resultList)
								{
									var price_lvl_sid = item.ACTIVE_PRICE_LVL_SID;
									var sbs_sid = item.SBS_SID;
									Logger.Log($"[INBOUND - PRICE] PRICE_LVL_SID : {sbs_sid}");

									var newAjustmentData = await createRpsAdjustment(session, item);
									string adjusment_sid = JObject.Parse(newAjustmentData)?["data"]?[0]?["sid"]?.ToString();

									await createRpsAdjItem(session, item, row, adjusment_sid);

									Logger.Log($"isReprocess: {isReprocess}");

									if (isReprocess)
									{
										Logger.Log("Reprocessing item...");

										var repo = new InboundPriceRepository();
										await repo.MarkTempPriceRowAsProcessedAsync(row);
									}
								}
							}
							else
							{
								if (resultList.Count == 0)
								{
									Logger.Log("[SKIP] Skip inserting data to temporary table. ProductCode is not existing on Prism DB.");
									continue;
								}

								await insertDataToTempDb(row);
							}

						}
						catch (Exception ex)
						{
							Logger.Log($"[ERROR] Failed to process row for ProductCode: {row["ProductCode"]} | {ex.Message}\nStackTrace: {ex.StackTrace}");
						}
					}
				}
				
			}
			catch (Exception ex)
			{
				Logger.Log($"[ERROR] Error inserting data - {ex.Message}\nStackTrace: {ex.StackTrace}");

				throw;
			}

		}

		private async Task reprocessPriceDbSyncAsync(dynamic repository, string session)
		{
			var repo = new InboundPriceRepository();
			var tempRecords = await repo.GetEligibleTempPriceRowsAsync(DateTime.UtcNow);
			var formattedRecords = new List<Dictionary<string, string>>();

			if (tempRecords.Count == 0)
			{
				Logger.Log("No reprocess records found on TempInboundPriceData.db.");

				return;
			}
			foreach (var tempRecord in tempRecords)
			{
				var rowDict = new Dictionary<string, string>();

				if (tempRecord.TryGetValue("CountryCode", out var countryCode)) rowDict["CountryCode"] = countryCode;
				if (tempRecord.TryGetValue("StoreCode", out var storeCode)) rowDict["StoreCode"] = storeCode;
				if (tempRecord.TryGetValue("ProductCode", out var productCode)) rowDict["ProductCode"] = productCode;
				if (tempRecord.TryGetValue("ColorCode", out var colorCode)) rowDict["ColorCode"] = colorCode;
				if (tempRecord.TryGetValue("SizeCode", out var sizeCode)) rowDict["SizeCode"] = sizeCode;
				if (tempRecord.TryGetValue("SKU", out var sku)) rowDict["SKU"] = sku;
				if (tempRecord.TryGetValue("PriceType", out var priceType)) rowDict["PriceType"] = priceType;
				if (tempRecord.TryGetValue("Currency", out var currency)) rowDict["Currency"] = currency;
				if (tempRecord.TryGetValue("Price", out var price)) rowDict["Price"] = price;
				if (tempRecord.TryGetValue("EffectivityDate", out var effectivityDate)) rowDict["EffectivityDate"] = effectivityDate;
				if (tempRecord.TryGetValue("ProductReference", out var productReference)) rowDict["ProductReference"] = productReference;
				if (tempRecord.TryGetValue("Brand", out var brand)) rowDict["Brand"] = brand;
				if (tempRecord.TryGetValue("PriceListCode", out var priceListCode)) rowDict["PriceListCode"] = priceListCode;
				if (tempRecord.TryGetValue("SerialNumber", out var serialNumber)) rowDict["SerialNumber"] = serialNumber;
				if (tempRecord.TryGetValue("PriceSource", out var priceSource)) rowDict["PriceSource"] = priceSource;
				if (tempRecord.TryGetValue("Price2", out var price2)) rowDict["Price2"] = price2;
				if (tempRecord.TryGetValue("EffectivePriceEndDate", out var effectivePriceEndDate)) rowDict["EffectivePriceEndDate"] = effectivePriceEndDate;
				if (tempRecord.TryGetValue("DiscountCode", out var discountCode)) rowDict["DiscountCode"] = discountCode;
				if (tempRecord.TryGetValue("DiscountDesc", out var discountDesc)) rowDict["DiscountDesc"] = discountDesc;
				if (tempRecord.TryGetValue("ReasonCode", out var reasonCode)) rowDict["ReasonCode"] = reasonCode;
				if (tempRecord.TryGetValue("ReasonDesc", out var reasonDesc)) rowDict["ReasonDesc"] = reasonDesc;
				if (tempRecord.TryGetValue("Level1Code", out var level1Code)) rowDict["Level1Code"] = level1Code;

				formattedRecords.Add(rowDict);
			}

			await processPriceSyncAsync(formattedRecords, repository, session, true);
		}

		private async Task<string> createRpsAdjustment(string session, dynamic item)
		{
			Logger.Log($"[INBOUND - PRICE]		[CREATE] ADJUSTMENT");

			string price_lvl_sid = item?.ACTIVE_PRICE_LVL_SID?.ToString();
			string sbs_sid = item?.SBS_SID?.ToString();

			var adjustmentPayload = new Dictionary<string, object>
			{
				["adjtype"] = 1,
				["originapplication"] = "RProPrismWeb",
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

		private async Task insertDataToTempDb(dynamic row)
		{
			try
			{
				var repo = new InboundPriceRepository();
				double p;
				double p2;

				await repo.InsertTempInboundPriceData(
					createdDate				: DateTime.Now.ToString("yyyyMMdd")
					, countryCode			: row["CountryCode"]
					, storeCode				: row["StoreCode"]
					, productCode			: row["ProductCode"]
					, colorCode				: row["ColorCode"]
					, sizeCode				: row["SizeCode"]
					, sku					: row["SKU"]
					, priceType				: row["PriceType"]
					, currency				: row["Currency"]
					, price					: double.TryParse(row["Price"], out p) ? p : 0
					, effectivityDate		: row["EffectivityDate"]
					, productReference		: row["ProductReference"]
					, brand					: row["Brand"]
					, priceListCode			: row["PriceListCode"]
					, serialNumber			: row["SerialNumber"]
					, priceSource			: row["PriceSource"]
					, price2				: double.TryParse(row["Price2"], out p2) ? p2 : 0
					, effectivePriceEndDate	: row["EffectivePriceEndDate"]
					, discountCode			: row["DiscountCode"]
					, discountDesc			: row["DiscountDesc"]
					, reasonCode			: row["ReasonCode"]
					, reasonDesc			: row["ReasonDesc"]
					, level1Code			: row["Level1Code"]
				);
				
			}
			catch (Exception ex)
			{
				Logger.Log($"[ERROR] Error inserting data ProductCode: {row["ProductCode"]} - {ex.Message}\nStackTrace: {ex.StackTrace}");

				throw;
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
						if (fields.Length > 1) rowDict["StoreCode"] = fields[1].Trim();
						if (fields.Length > 2) rowDict["ProductCode"] = fields[2].Trim();
						if (fields.Length > 3) rowDict["ColorCode"] = fields[3].Trim();
						if (fields.Length > 4) rowDict["SizeCode"] = fields[4].Trim();
						if (fields.Length > 5) rowDict["SKU"] = fields[5].Trim();
						if (fields.Length > 6) rowDict["PriceType"] = fields[6].Trim();
						if (fields.Length > 7) rowDict["Currency"] = fields[7].Trim();
						if (fields.Length > 8) rowDict["Price"] = fields[8].Trim();
						if (fields.Length > 9) rowDict["EffectivityDate"] = fields[9].Trim();
						if (fields.Length > 10) rowDict["ProductReference"] = fields[10].Trim();
						if (fields.Length > 11) rowDict["Brand"] = fields[11].Trim();
						if (fields.Length > 12) rowDict["PriceListCode"] = fields[12].Trim();
						if (fields.Length > 13) rowDict["SerialNumber"] = fields[13].Trim();
						if (fields.Length > 14) rowDict["PriceSource"] = fields[14].Trim();
						if (fields.Length > 15) rowDict["Price2"] = fields[15].Trim();
						if (fields.Length > 16) rowDict["EffectivePriceEndDate"] = fields[16].Trim();
						if (fields.Length > 17) rowDict["DiscountCode"] = fields[17].Trim();
						if (fields.Length > 18) rowDict["DiscountDesc"] = fields[18].Trim();
						if (fields.Length > 19) rowDict["ReasonCode"] = fields[19].Trim();
						if (fields.Length > 20) rowDict["ReasonDesc"] = fields[20].Trim();
						if (fields.Length > 21) rowDict["Level1Code"] = fields[21].Trim();

						result.Add(rowDict);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"Error in BuildPriceCollection: {ex.Message}\nStackTrace: {ex.StackTrace}");
			}

			return result;
		}

	}
}
