using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
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
						Logger.Log($"[INBOUND - ITEM] Snapshot loaded. Rows found: {result.Count}");

						foreach (var row in result)
						{
							var alu = row["PROD_SKU"]?.ToString();
							var upc = row["PROD_GTIN"]?.ToString();

							var rps_isi_collection = await repository.GetRpsInvnSbsItem("UPC", upc);
							Logger.Log($"RPS.INVN_SBS_ITEM Collection: {rps_isi_collection}");

							if (rps_isi_collection != null)
							{
								// UPDATE logic here
							}
							else
							{
								// CREATE logic here
							}

							var payload = new
							{
								data = new[]
								{
									new
									{
										OriginApplication		= "RProPrismWeb"
										, PrimaryItemDefinition = new
										{
											dcssid			= "556255621000149144"
											, vendsid		= (string)null
											, description1	= row["PRODUCT_CD"]?.ToString().Replace("-", "")
											, attribute		= row["SIZE_DIM2"]
											, itemsize		= row["SIZE_DIM1"]
										}
										, InventoryItems = new[]
										{
											new
											{
												sbssid					= "555356986000134257"
												, dcssid				= "556255621000149144"
												, description1			= row["PRODUCT_CD"]?.ToString().Replace("-", "")
												, description2			= row["PRODUCT_NM"]?.ToString()
												, description3			= row["STYLE_CD"]?.ToString()
												, alu					= row["PROD_SKU"]?.ToString()	// remove for update
												, itemsize				= row["SIZE_DIM1"]?.ToString()
												, attribute				= row["SIZE_DIM2"]?.ToString()
												, upc					= row["PROD_GTIN"]?.ToString()	// remove for update
												, description4			= row["PROD_JAN"]?.ToString()
												, text1					= row["SAP_TAX_CD"]?.ToString()
												, cost					= 0
												, spif					= 0
												, taxcodesid			= "555538434000189911"
												, useqtydecimals		= 0
												, regional				= false
												, active				= true
												, maxdiscperc1			= 100
												, maxdiscperc2			= 100
												, serialtype			= 0
												, lottype				= 0
												, kittype				= 0
												, tradediscpercent		= 0
												, activestoresid		= "555444605000106428"
												, activepricelevelsid	= "555357012000134500"
												, activeseasonsid		= "555357012000192512"
												, actstrprice			= 0
												, actstrpricewt			= 0
												, actstrohqty			= 0
												, dcscode				= "1  1  1"
												, invnextend = new[]
												{
													new
													{
														udf6string		= row["BRAND_CD"]?.ToString()
														, udf10string	= row["CONSUMER_CD"]?.ToString()
														, udf2string	= row["PROD_CAT_CD"]?.ToString()
														, udf12string	= row["CLASS_CD"]?.ToString()
														, udf14string	= row["SUB_CLASS_CD"]?.ToString()
														, udf8string	= row["SEASON_CD"]?.ToString()
														, udf9string	= row["AFFILIATE"]?.ToString()
														, udf5_string	= row["DEMAND_NM"]?.ToString()
													}
												}
											}
										}
								, UpdateStyleDefinition	= false
								, UpdateStyleCost       = false
								, UpdateStylePrice      = false
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

							if (!isSuccessfulApi)
							{
								Logger.Log($"❌ [INBOUND - ITEM] API failed for UPC: {upc} | ALU: {alu}");
								isSuccess = false;
							}
							else
							{
								Logger.Log($"[INBOUND - ITEM] Successfully processed UPC: {upc} | ALU: {alu}");
							}
						}
					}
					catch (Exception ex)
					{
						Logger.Log($"❌ [INBOUND - ITEM] Error processing file {fileName}: {ex.Message}");
						isSuccess = false;
					}

					// MOVE FILE
					try
					{
						string destinationDir = isSuccess ? sentDir : unsentDir;
						string destinationPath = Path.Combine(destinationDir, fileName);

						if (File.Exists(destinationPath))
							File.Delete(destinationPath);

						File.Move(file, destinationPath);
						Logger.Log($"[INBOUND - ITEM] File moved to {(isSuccess ? "SENT" : "UNSENT")} folder: {destinationPath}");
					}
					catch (Exception ex)
					{
						Logger.Log($"❌ [INBOUND - ITEM] Failed to move file {fileName}: {ex.Message}");
					}
				}

				Logger.Log("[INBOUND - ITEM] END Sync Process.");
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND - ITEM] Critical Error in RunItemSyncAsync: {ex}");
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
				Logger.Log($"[INBOUND - ITEM] Error in BuildItemCollection: {ex.Message}");
			}

			return result;
		}
	}
}
