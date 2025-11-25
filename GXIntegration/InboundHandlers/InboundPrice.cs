using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundPrice
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();
		bool isSuccess = true;
		private bool isAuto = false;

		public async Task RunPriceSyncAsync(string session, PrismRepository repository, bool is_auto)
		{
			isAuto = is_auto;
			string inboundDir = GlobalInbound.InboundDir;
			string sentDir = GlobalInbound.SentDir;
			string unsentDir = GlobalInbound.UnsentDir;

			try
			{
				string fileNameFormat = "LSPI_PRTARI_*.*";
				string sendingDir = Path.Combine(inboundDir, "SENDING");
				var files = globalInbound.GetInboundFiles(sendingDir, fileNameFormat);

				if (files.Count == 0)
				{
					Logger.LogInbound($"0 PRICE {fileNameFormat} file found.", isAuto);
					return;
				}

				Logger.LogInbound($"[PRICE] {files.Count} {fileNameFormat} file found.", isAuto);

				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file);

					try
					{
						var result = BuildPriceCollection(file);
						Logger.LogInbound($"-----------------------------------", isAuto);
						Logger.LogInbound($"[PRICE] Processing file: {fileName} | Row No found: {result.Count}", isAuto);

						await processPriceSyncAsync(result, repository, session, false);
						await reprocessPriceDbSyncAsync(repository, session);

					}
					catch (Exception ex)
					{
						Logger.LogError($"❌ [PRICE] Error processing file {fileName}: {ex.Message}", isAuto);
						isSuccess = false;
					}

					// MOVE FILE
					globalInbound.MoveFile(file, isSuccess);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"❌[PRICE] Error in RunPriceSyncAsync: {ex.Message}\nStackTrace: {ex.StackTrace}", isAuto);
				isSuccess = false;
			}
		}

		private async Task processPriceSyncAsync(List<Dictionary<string, string>> result, PrismRepository repository, string session, bool isReprocess)
		{
			XDocument config = XDocument.Load("config.xml");

			var sbsNos = config.Root.Element("PriceSubsidiaries").Element("Subsidiary").Value;

			foreach (var sbsNo in sbsNos)
			{
				Logger.LogInbound($"[PRICE] Processing for SBS_NO: {sbsNo}", isAuto);

				// Filter only rows that have valid effectivity date <= today
				var validRows = new List<Dictionary<string, string>>();
				foreach (var row in result)
				{
					if (DateTime.TryParseExact(row["EffectivityDate"], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime effDate)
						&& effDate <= DateTime.UtcNow.Date)
					{
						validRows.Add(row);
					}
				}

				if (!validRows.Any())
				{
					Logger.LogInbound("[PRICE] No valid rows for adjustment.", isAuto);
					continue;
				}

				int batchSize = 1000;
				for (int i = 0; i < validRows.Count; i += batchSize)
				{
					var batch = validRows.Skip(i).Take(batchSize).ToList();

					// Fetch repository data for all items in batch
					var productCodes = batch.Select(r => r["ProductCode"]).ToList();
					var filters = new Dictionary<string, object>
					{
						{ "SBS_NO", sbsNo },
						{ "DESCRIPTION1", productCodes }, // List
						{ "ACTIVE", 1 },
						{ "PRICE_LVL_NAME", "LSPC" }
					};

					var items = await repository.GetInboundItemsAsync(filters);
					var itemList = items as List<dynamic> ?? new List<dynamic>();

					if (!itemList.Any())
					{
						Logger.LogInbound("[PRICE] No items found in Prism DB for this batch.", isAuto);
						continue;
					}

					// Create single adjustment for this batch
					var adjustmentData = await createRpsAdjustment(session, itemList[0]);
					var adjustmentSid = JObject.Parse(adjustmentData)?["data"]?[0]?["sid"]?.ToString();
					Logger.LogInbound($"[PRICE] Adjustment SID: {adjustmentSid}", isAuto);
					Logger.LogInbound($"[PRICE] Item Count: {itemList.Count}", isAuto);

					// Add all items in the batch
					var itemCount = 0;
					foreach (var item in itemList)
					{
						itemCount++;
						// Find matching row for this item
						var row = batch.FirstOrDefault(r => r["ProductCode"] == item.DESCRIPTION1);
						if (row != null)
						{
							await createRpsAdjItem(session, item, row, adjustmentSid, itemCount);
						}
					}

					// Update adjustment
					var adjResult = await repository.GetRpsAdjustment("SID", adjustmentSid);
					var rowVersion = adjResult[0].ROW_VERSION.ToString();
					await updateRpsAdjustment(session, adjustmentSid, rowVersion);

					// Mark as processed if reprocessing
					if (isReprocess)
					{
						var repo = new InboundPriceRepository();
						foreach (var row in batch)
						{
							await repo.MarkTempPriceRowAsProcessedAsync(row);
						}
					}

					Logger.LogInbound($"[PRICE] Batch of {batch.Count} items processed for adjustment {adjustmentSid}", isAuto);
				}
			}

			// Handle rows with effectivity date in the future: insert to temp DB
			var futureRows = result
				.Where(r => DateTime.TryParseExact(r["EffectivityDate"], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime effDate)
							&& effDate > DateTime.UtcNow.Date)
				.ToList();

			foreach (var row in futureRows)
			{
				await insertDataToTempDb(row);
			}
		}

		private async Task reprocessPriceDbSyncAsync(dynamic repository, string session)
		{
			var repo = new InboundPriceRepository();
			var tempRecords = await repo.GetEligibleTempPriceRowsAsync(DateTime.UtcNow);
			var formattedRecords = new List<Dictionary<string, string>>();

			if (tempRecords.Count == 0)
			{
				Logger.LogInbound("[PRICE] No reprocess records found on TempInboundPriceData.db.", isAuto);

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

		// ***************************************************
		// API PROCESS METHODS
		// ***************************************************
		private async Task<string> createRpsAdjustment(string session, dynamic item)
		{
			Logger.LogInbound($"[PRICE] [CREATE] Creating ADJUSTMENT new record...", isAuto);

			string price_lvl_sid = item?.ACTIVE_PRICE_LVL_SID?.ToString();
			string sbs_sid = item?.SBS_SID?.ToString();
			var currentDate = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

			var adjustmentPayload = new Dictionary<string, object>
			{
				["adjtype"] = 1,
				["originapplication"] = "RProPrismWeb",
				["pricelvlsid"] = price_lvl_sid,
				["sbssid"] = sbs_sid,
				["status"] = 3,
				["modifieddatetime"] = currentDate,
				["adjreasonsid"] = "555357003000181402"
			};

			string endpointCreate = "/api/backoffice/adjustment";
			var payload = new { data = new[] { adjustmentPayload } };
			string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, endpointCreate
									, json
									, out bool isSuccessfulApi
									, "POST"
									, 1
									);

			var priceSid = JObject.Parse(responseJson)["data"]?[0]?["sid"]?.ToString();

			//Logger.LogInbound($"[EMPLOYEE] API Response: {responseJson}", isAuto);
			Logger.LogInbound($"[PRICE] SID: {priceSid}", isAuto);

			if (!isSuccessfulApi)
			{
				Logger.LogInbound($"❌ [PRICE] API failed.", isAuto);
				isSuccess = false;
			}
			else
			{
				//Logger.LogInbound($"[PRICE] Successfully processed.", isAuto);
			}

			return responseJson;
		}

		private async Task<string> updateRpsAdjustment(string session, string adjusmentSid, string rowversion)
		{
			Logger.LogInbound($"[ITEM] [UPDATE] Updating existing ADJUSTMENT record...", isAuto);

			var currentDate = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
			int rowVersion = Convert.ToInt32(rowversion);

			var adjustmentPayload = new Dictionary<string, object>
			{
				["modifieddatetime"] = currentDate,
				["adjreasonsid"] = "555357003000181402",
				["reasonname"] = "MANUALLY",
				["rowversion"] = rowVersion,
				["status"] = 4,
			};

			string endpointCreate = $"/api/backoffice/adjustment/{adjusmentSid}?";
			var payload = new { data = new[] { adjustmentPayload } };
			//Logger.LogInbound($"Payload: {JsonConvert.SerializeObject(payload, Formatting.Indented)}");
			string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, endpointCreate
									, json
									, out bool isSuccessfulApi
									, "PUT"
									, 1
									);

			var priceSid = JObject.Parse(responseJson)["data"]?[0]?["sid"]?.ToString();

			//Logger.LogInbound($"[EMPLOYEE] API Response: {responseJson}", isAuto);
			Logger.LogInbound($"[PRICE] SID: {priceSid}", isAuto);

			if (!isSuccessfulApi)
			{
				Logger.LogError($"❌ [PRICE] API failed.", isAuto);
				isSuccess = false;
			}
			else
			{
				//Logger.LogError($"[PRICE] Successfully processed.", isAuto);
			}

			return responseJson;
		}

		private async Task<string> createRpsAdjItem(string session, dynamic item, dynamic fileRowData, string adjustmentSid, int itemCount)
		{
			Logger.LogInbound($"[PRICE] [CREATE] ItemCount : {itemCount} | Creating ADJ_ITEM new record...", isAuto);

			string item_sid = item?.SID?.ToString();
			string sbs_sid = item?.SBS_SID?.ToString();
			decimal adjValue = 0m;
			if (!decimal.TryParse(fileRowData["Price"], out adjValue))
			{
				Logger.LogInbound($"[PRICE] Could not parse Price '{fileRowData["Price"]}' to decimal. Defaulting to 0.");
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
									, out bool isSuccessfulApi
									, "POST"
									, 1
									);

			var priceSid = JObject.Parse(responseJson)["data"]?[0]?["sid"]?.ToString();

			//Logger.LogInbound($"[EMPLOYEE] API Response: {responseJson}", isAuto);
			Logger.LogInbound($"[PRICE] SID: {priceSid}", isAuto);

			if (!isSuccessfulApi)
			{
				Logger.LogError($"❌ [PRICE] API failed.", isAuto);
				isSuccess = false;
			}
			else
			{
				//Logger.LogInbound($"[PRICE] Successfully processed.", isAuto);
			}

			return responseJson;
		}

		// ***************************************************
		// MISC METHODS
		// ***************************************************
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
				Logger.LogError($"[ERROR] Error inserting data ProductCode: {row["ProductCode"]} - {ex.Message}\nStackTrace: {ex.StackTrace}", isAuto);
				isSuccess = false;
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
						//if (fields.Length > 2) rowDict["ProductCode"] = fields[2].Trim() + "0";
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
				Logger.LogError($"[PRICE] Error in BuildPriceCollection: {ex.Message}\nStackTrace: {ex.StackTrace}", isAuto);
				isSuccess = false;
			}

			return result;
		}

	}
}
