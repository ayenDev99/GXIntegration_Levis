using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundItem
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();
		private bool isAuto = false;

		public async Task RunItemSyncAsync(string session, PrismRepository repository, bool is_auto)
		{
			isAuto = is_auto;
			string inboundDir = GlobalInbound.InboundDir;
			string sentDir = GlobalInbound.SentDir;
			string unsentDir = GlobalInbound.UnsentDir;

			try
			{
				string fileNameFormat = "LSPI_ITEM_*.*";
				string sendingDir = Path.Combine(inboundDir, "SENDING");
				var files = globalInbound.GetInboundFiles(sendingDir, fileNameFormat);

				if (files.Count == 0)
				{
					Logger.LogInbound($"0 ITEM {fileNameFormat} file found.", isAuto);
					return;
				}

				Logger.LogInbound($"[ITEM] {files.Count} {fileNameFormat} file found.", isAuto);

				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file);
					bool isSuccess = true;

					try
					{						
						var result = BuildItemCollection(file);
						Logger.LogInbound($"-----------------------------------", isAuto);
						Logger.LogInbound($"[ITEM] Processing file: {fileName} | Row No found: {result.Count}", isAuto);

						foreach (var row in result)
						{
							var alu = row["PROD_SKU"]?.ToString();
							var upc = row["PROD_GTIN"]?.ToString();

							var rps_isi_collection = await repository.GetRpsInvnSbsItem("UPC", upc);

							if (rps_isi_collection != null && rps_isi_collection.Count > 0)
							{
								// UPDATE logic here
								await updateInventoryItem(row, session, isSuccess, rps_isi_collection);
							}
							else
							{
								// CREATE logic here
								await createInventoryItem(row, session, isSuccess);
							}
						}
					}
					catch (Exception ex)
					{
						Logger.LogError($"❌ [ITEM] Error processing file {fileName}: {ex.Message}", isAuto);
						isSuccess = false;
					}

					// MOVE FILE
					globalInbound.MoveFile(file, isSuccess);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"❌ [ITEM] Critical Error in RunItemSyncAsync: {ex}", isAuto);
			}
		}

		private async Task createInventoryItem(dynamic row, string session, bool isSuccess)
		{
			Logger.LogInbound($"[ITEM] [CREATE] Creating new record...", isAuto);

			// Trim fieled value based on Prism DB field limits
			var desc1 = StringExtensions.TrimMax(row["PRODUCT_CD"]?.ToString().Replace("-", ""), 30);
			var desc2 = StringExtensions.TrimMax(row["PRODUCT_NM"]?.ToString(), 30);
			var desc3 = StringExtensions.TrimMax(row["STYLE_CD"]?.ToString(), 30);
			var desc4 = StringExtensions.TrimMax(row["PROD_JAN"]?.ToString(), 30);

			var itemAlu = StringExtensions.TrimMax(row["PROD_SKU"]?.ToString(), 20);
			var itemUpc = StringExtensions.TrimMax(row["PROD_GTIN"]?.ToString(), 18);
			var itemSize = StringExtensions.TrimMax(row["SIZE_DIM1"]?.ToString(), 8);
			var itemAttribute = StringExtensions.TrimMax(row["SIZE_DIM2"]?.ToString(), 8);
			var txt1 = StringExtensions.TrimMax(row["SAP_TAX_CD"]?.ToString(), 255);

			var udf2 = StringExtensions.TrimMax(row["PROD_CAT_CD"]?.ToString(), 50);
			var udf5 = StringExtensions.TrimMax(row["DEMAND_NM"]?.ToString(), 50);
			var udf6 = StringExtensions.TrimMax(row["BRAND_CD"]?.ToString(), 50);
			var udf8 = StringExtensions.TrimMax(row["SEASON_CD"]?.ToString(), 50);
			var udf9 = StringExtensions.TrimMax(row["AFFILIATE"]?.ToString(), 50);
			var udf10 = StringExtensions.TrimMax(row["CONSUMER_CD"]?.ToString(), 50);
			var udf12 = StringExtensions.TrimMax(row["CLASS_CD"]?.ToString(), 50);
			var udf14 = StringExtensions.TrimMax(row["SUB_CLASS_CD"]?.ToString(), 50);

			// Start building payload
			var payload = new
			{
				data = new[]
				{
					new
					{
						OriginApplication       = "RProPrismWeb"
						, InventoryItems = new[]
						{
							new
							{
								sbssid                  = "555356986000134257"
								, dcssid                = "556255621000149144"
								, description1          = desc1
								, description2          = desc2
								, description3          = desc3
								, description4          = desc4
								, alu                   = itemAlu
								, itemsize              = itemSize
								, attribute             = itemAttribute
								, upc                   = itemUpc
								, text1                 = txt1
								, useqtydecimals        = 0
								, active                = true
								, invnextend = new[]
								{
									new
									{
										udf2string		= udf2 
										, udf5string	= udf5 
										, udf6string    = udf6 
										, udf8string    = udf8 
										, udf9string    = udf9 
										, udf10string   = udf10
										, udf12string   = udf12
										, udf14string   = udf14
									}
								}
							}
						}
					}
				}
			};

			var json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			//Logger.LogInbound("Payload:\n" + json);

			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, "/api/backoffice/inventory?action=InventorySaveItems"
									, json
									, out bool isSuccessfulApi
									, "POST"
									, 1);
			var itemSid = JObject.Parse(responseJson)["data"]?[0]?["sid"]?.ToString();

			//Logger.LogInbound($"[ITEM] API Response: {responseJson}", isAuto);
			//Logger.LogInbound($"[ITEM] SID: {itemSid}", isAuto);

			var alu = row["PROD_SKU"]?.ToString();
			var upc = row["PROD_GTIN"]?.ToString();

			if (!isSuccessfulApi)
			{
				Logger.LogInbound($"❌ [ITEM] CREATE : API failed for PROD_GTIN/UPC: {upc} | ALU: {alu}");
				isSuccess = false;
			}
			else
			{
				Logger.LogInbound($"[ITEM] CREATE : Successfully processed PROD_GTIN/UPC: {upc} | ALU: {alu}");
			}
		}

		private async Task updateInventoryItem(dynamic row, string session, bool isSuccess, dynamic rps_isi_collection)
		{
			Logger.LogInbound($"[ITEM] [UPDATE] Updating existing record...", isAuto);

			var list = rps_isi_collection as List<dynamic>;
			if (list == null || list.Count == 0)
			{
				//Logger.LogInbound("[ITEM] RPS.INVN_SBS_ITEM Collection is empty!", isAuto);
				return;
			}

			string SID = null;
			var firstItem = list[0];
			if (firstItem.SID != null)
			{
				SID = firstItem.SID.ToString();
			}

			Logger.LogInbound($"[ITEM] INVN_SBS_ITEM SID: {SID}", isAuto);

			firstItem.SID = SID;

			var isi_collection = System.Text.Json.JsonSerializer.Serialize(list);
			Logger.LogInbound($"[ITEM] RPS.INVN_SBS_ITEM Collection: {isi_collection}", isAuto);

			// Trim fieled value based on Prism DB field limits
			var desc3 = StringExtensions.TrimMax(row["STYLE_CD"]?.ToString(), 30);
			var desc4 = StringExtensions.TrimMax(row["PROD_JAN"]?.ToString(), 30);
			var txt1 = StringExtensions.TrimMax(row["SAP_TAX_CD"]?.ToString(), 255);

			var udf2 = StringExtensions.TrimMax(row["PROD_CAT_CD"]?.ToString(), 50);
			var udf5 = StringExtensions.TrimMax(row["DEMAND_NM"]?.ToString(), 50);
			var udf6 = StringExtensions.TrimMax(row["BRAND_CD"]?.ToString(), 50);
			var udf8 = StringExtensions.TrimMax(row["SEASON_CD"]?.ToString(), 50);
			var udf9 = StringExtensions.TrimMax(row["AFFILIATE"]?.ToString(), 50);
			var udf10 = StringExtensions.TrimMax(row["CONSUMER_CD"]?.ToString(), 50);
			var udf12 = StringExtensions.TrimMax(row["CLASS_CD"]?.ToString(), 50);
			var udf14 = StringExtensions.TrimMax(row["SUB_CLASS_CD"]?.ToString(), 50);

			var existingExtendJson = await getInventoryItemExtend(SID, session, isSuccess);
			//Logger.LogInbound($"[ITEM] Existing Inventory Extend JSON: {existingExtendJson}");

			var root = JsonConvert.DeserializeObject<JObject>(existingExtendJson);

			var item = root["data"]?.FirstOrDefault();  // get the first item in "data"
			if (item == null)
			{
				Logger.LogInbound("[ITEM] No inventory item found.", isAuto);
				return;
			}

			var invnextendArray = item["invnextend"] as JArray; // get "invnextend" array
			if (invnextendArray == null || !invnextendArray.Any())
			{
				Logger.LogInbound("[ITEM] No invnextend data found.", isAuto);
				return;
			}

			var extend = invnextendArray.FirstOrDefault();		// get first invnextend record
			string invnextendSid = extend["sid"]?.ToString();   // get the SID from the invnextend record
			//Logger.LogInbound($"[ITEM] invnextend SID: {invnextendSid}");

			var currentDate = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

			// Start building payload
			var payload = new
			{
				data = new[]
				{
					new
					{
						OriginApplication       = "RProPrismWeb"
						, InventoryItems = new[]
						{
							new
							{
								sbssid                  = "555356986000134257"
								, sid					= SID
								, description3          = desc3
								, description4          = desc4
								, text1                 = txt1
								, invnextend = new[]
								{
									new
									{
										invnsbsitemsid		= SID
										, sid				= invnextendSid
										, modifieddatetime	= currentDate
										, udf2string		= udf2
										, udf5string		= udf5
										, udf6string		= udf6
										, udf8string		= udf8
										, udf9string		= udf9
										, udf10string		= udf10
										, udf12string		= udf12
										, udf14string		= udf14
									}
								}
							}
						}
					}
				}
			};

			var json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			//Logger.LogInbound("[ITEM] Payload:\n" + json);

			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, "/api/backoffice/inventory?action=InventorySaveItems"
									, json
									, out bool isSuccessfulApi
									, "POST"
									, 1);

			var itemSid = JObject.Parse(responseJson)["data"]?[0]?["sid"]?.ToString();

			//Logger.LogInbound($"[ITEM] API Response: {responseJson}", isAuto);
			//Logger.LogInbound($"[ITEM] SID: {itemSid}", isAuto);

			var alu = row["PROD_SKU"]?.ToString();
			var upc = row["PROD_GTIN"]?.ToString();

			if (!isSuccessfulApi)
			{
				Logger.LogInbound($"❌ [ITEM] API failed for PROD_GTIN/UPC: {upc} | ALU: {alu}");
				isSuccess = false;
			}
			else
			{
				Logger.LogInbound($"[ITEM] Successfully processed PROD_GTIN/UPC: {upc} | ALU: {alu}");
			}
		}

		private async Task<string> getInventoryItemExtend(string invnsbsitemsid, string session, bool isSuccess)
		{
			var payload = new { data = new[] { new { } } };
			var json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);

			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, $"/api/backoffice/inventory?action=InventoryGetItems&cols=*,invnextend,invnextend.*,invnprice.*,invnquantity.*&filter=(sid,eq,{invnsbsitemsid})"
									, json
									, out bool isSuccessfulApi
									, "POST"
									, 1);

			if (!isSuccessfulApi)
			{
				Logger.LogInbound($"❌ [ITEM] API failed on getting Inventory Extend data");
				isSuccess = false;
				return null;
			}
			else
			{
				//Logger.LogInbound($"[ITEM] Successfully processed on getting Inventory Extend data. INVN_SBS_SID: {invnsbsitemsid}");
				return responseJson;
			}
		}

		private List<Dictionary<string, string>> BuildItemCollection(string filePath)
		{
			var result = new List<Dictionary<string, string>>();

			try
			{
				var lines = File.ReadAllLines(filePath);
				if (lines.Length == 0) return result;

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

					rowDict["UNITCOUNT_SIGN"] = "UNITCOUNT:";
					result.Add(rowDict);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"[ITEM] Error in BuildItemCollection: {ex.Message}", isAuto);
			}

			return result;
		}
	}
}
