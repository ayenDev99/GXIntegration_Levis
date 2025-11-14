using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using Renci.SshNet;
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

		public async Task RunItemSyncAsync(string session, PrismRepository repository)
		{
			string inboundDir = GlobalInbound.InboundDir;
			string sentDir = GlobalInbound.SentDir;
			string unsentDir = GlobalInbound.UnsentDir;

			try
			{
				Logger.Log("--------------------------------------------------------------------------");
				Logger.Log("[INBOUND - ITEM] STARTING ITEM Sync Process...");

				string fileNameFormat = "LSPI_ITEM_*.*";
				string sendingDir = Path.Combine(inboundDir, "SENDING");
				var files = globalInbound.GetInboundFiles(sendingDir, fileNameFormat);

				if (files.Count == 0)
				{
					Logger.Log($"[INBOUND - ITEM] No {fileNameFormat} file format found.");
					return;
				}

				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file);
					bool isSuccess = true;

					try
					{
						Logger.Log($"[INBOUND - ITEM] Processing file: {fileName}");

						var result = BuildItemCollection(file);
						Logger.Log($"[INBOUND - ITEM] ITEM loaded. Rows found: {result.Count}");

						foreach (var row in result)
						{
							var alu = row["PROD_SKU"]?.ToString();
							var upc = row["PROD_GTIN"]?.ToString();

							var rps_isi_collection = await repository.GetRpsInvnSbsItem("UPC", upc);
							var isi_collection = System.Text.Json.JsonSerializer.Serialize(rps_isi_collection);

							if (rps_isi_collection != null && rps_isi_collection.Count > 0)
							{
								// UPDATE logic here
								Logger.Log($"RPS.INVN_SBS_ITEM Collection: {isi_collection}");
								Logger.Log($"[INBOUND - ITEM] Start UPDATE Process.");

							}
							else
							{
								// CREATE logic here
								Logger.Log($"[INBOUND - ITEM] Start CREATE Process.");
								await createInventoryItem(row, session, isSuccess);
							}
						}
					}
					catch (Exception ex)
					{
						Logger.Log($"❌ [INBOUND - ITEM] Error processing file {fileName}: {ex.Message}");
						isSuccess = false;
					}

					// MOVE FILE
					globalInbound.MoveFile(file, isSuccess);
				}

				Logger.Log("[INBOUND - ITEM] END Sync Process.");
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND - ITEM] Critical Error in RunItemSyncAsync: {ex}");
			}
		}

		private async Task createInventoryItem(dynamic row, string session, bool isSuccess)
		{
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
								, alu                   = itemAlu	// remove for update
								, itemsize              = itemSize
								, attribute             = itemAttribute
								, upc                   = itemUpc	// remove for update
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
			//Logger.Log("Payload:\n" + json);

			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, "/api/backoffice/inventory?action=InventorySaveItems"
									, json
									, out bool isSuccessfulApi
									, "POST"
									, 1);

			var alu = row["PROD_SKU"]?.ToString();
			var upc = row["PROD_GTIN"]?.ToString();
			if (!isSuccessfulApi)
			{
				Logger.Log($"❌ [INBOUND - ITEM] API failed for PROD_GTIN/UPC: {upc} | ALU: {alu}");
				isSuccess = false;
			}
			else
			{
				Logger.Log($"[INBOUND - ITEM] Successfully processed PROD_GTIN/UPC: {upc} | ALU: {alu}");
			}
		}

		//private Task async updateInventoryItem()
		//{

		//}


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
				Logger.Log($"[INBOUND - ITEM] Error in BuildItemCollection: {ex.Message}");
			}

			return result;
		}
	}
}
