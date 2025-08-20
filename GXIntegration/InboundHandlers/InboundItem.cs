using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundItem
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunItemSyncAsync()
		{
			try
			{
				// *** Call authentication first
				var config = XDocument.Load("config.xml");

				// Read Prism config values
				string prismAddress = config.Root.Element("PrismConfig").Element("Address").Value;
				string prismUsername = config.Root.Element("PrismConfig").Element("Username").Value;
				string prismPassword = config.Root.Element("PrismConfig").Element("Password").Value;
				string workstationName = config.Root.Element("PrismConfig").Element("WorkstationName").Value;

				Logger.Log("Credentials : ...");
				Logger.Log("Address : " + prismAddress);
				Logger.Log("Username : " + prismUsername);
				Logger.Log("Password : " + prismPassword);
				Logger.Log("Workstation Name : " + workstationName);
				Logger.Log("--------------------------------------------------------------------------");

				Logger.Log("Starting ITEM sync...");

				string session = await globalInbound.Authenticate(
					prismAddress, prismUsername, prismPassword, workstationName);

				if (string.IsNullOrEmpty(session))
				{
					Logger.Log("Authentication failed.");
					Console.WriteLine("Authentication failed.");
					return;
				}

				Logger.Log("Authenticated. Session: " + session);
				Console.WriteLine(session);

				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string today = DateTime.Now.ToString("yyyyMMdd");
				string inboundDir = Path.Combine(baseDir, "INBOUND", today);

				// Create directory if it does not exist
				if (!Directory.Exists(inboundDir))
				{
					Directory.CreateDirectory(inboundDir);
					Logger.Log("INBOUND folder created : " + inboundDir);
				}

				// Find all files that start with LSPI_ITEM_
				string[] files = Directory.GetFiles(inboundDir, "LSPI_ITEM_*.*");

				if (files.Length == 0)
				{
					Logger.Log("No LSPI_ITEM_ files found in: " + inboundDir);
					return;
				}

				Logger.Log("Files to Processed--------------------------------------------------------");
				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file); // extracts just the file name
					Console.WriteLine(fileName);
					Logger.Log(fileName);
				}
				Logger.Log("--------------------------------------------------------------------------");


				foreach (string file in files)
				{
					var result = BuildItemCollection(file);
					Logger.Log($"Snapshot loaded. Rows found: {result.Count}");

					foreach (var row in result)
					{
						foreach (var kv in row)
						{
							//Console.WriteLine($"{kv.Key}: {kv.Value}");
						}

						// Build payload for this specific row
						var payload = new
						{
							data = new[]
							{
							new
							{
								OriginApplication = "RProPrismWeb",
								PrimaryItemDefinition = new
								{
									dcssid       = "556255621000149144",
									vendsid      = (string)null,
									description1 = row["PRODUCT_CD"]?.ToString().Replace("-", ""),
									attribute    = row["SIZE_DIM2"],
									itemsize     = row["SIZE_DIM1"]
								},
								InventoryItems = new[]
								{
									new
									{
										sbssid              = "555356986000134257",
										dcssid              = "556255621000149144",
										description1        = row["PRODUCT_CD"]?.ToString().Replace("-", ""),
										description2        = row["PRODUCT_NM"]?.ToString(),
										description3        = row["STYLE_CD"]?.ToString(),
										alu                 = row["PROD_SKU"]?.ToString(),
										itemsize            = row["SIZE_DIM1"]?.ToString(),
										attribute           = row["SIZE_DIM2"]?.ToString(),
										upc                 = row["PROD_GTIN"]?.ToString(),
										description4        = row["PROD_JAN"]?.ToString(),
										text1               = row["SAP_TAX_CD"]?.ToString(),

										cost                = 0,
										spif                = 0,
										taxcodesid          = "555538434000189911",
										useqtydecimals      = 0,
										regional            = false,
										active              = true,
										maxdiscperc1        = 100,
										maxdiscperc2        = 100,
										serialtype          = 0,
										lottype             = 0,
										kittype             = 0,
										tradediscpercent    = 0,
										activestoresid      = "555444605000106428",
										activepricelevelsid = "555357012000134500",
										activeseasonsid     = "555357012000192512",
										actstrprice         = 0,
										actstrpricewt       = 0,
										actstrohqty         = 0,
										dcscode             = "1  1  1",

										invnextend = new[]
										{
											new
											{
												udf6string   = row["BRAND_CD"]?.ToString(),
												udf10string  = row["CONSUMER_CD"]?.ToString(),
												udf2string   = row["PROD_CAT_CD"]?.ToString(),
												udf12string  = row["CLASS_CD"]?.ToString(),
												udf14string  = row["SUB_CLASS_CD"]?.ToString(),
												udf8string   = row["SEASON_CD"]?.ToString(),
												udf9string   = row["AFFILIATE"]?.ToString(),
												udf5_string  = row["DEMAND_NM"]?.ToString(),
											}
										}
									}
								},
								UpdateStyleDefinition = false,
								UpdateStyleCost       = false,
								UpdateStylePrice      = false
							}
						}
						};

						var json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);

						Console.WriteLine("Payload:");
						Console.WriteLine(json);
						//Logger.Log("Payload built:\n" + json);

						string responseJson = GlobalInbound.CallPrismAPI(
												session,
												prismAddress,
												"/api/backoffice/inventory?action=InventorySaveItems",
												json,
												out bool issuccessful,
												"POST");

						//string responseJson = globalInbound.CallPrismAPI(session, prismAddress, "/api/backoffice/inventory?action=InventorySaveItems", json, out bool issuccessful, "POST");
						Console.WriteLine("Response: " + responseJson);
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in RunItemSyncAsync: {ex.Message}");
				return;
			}
		}

		private List<Dictionary<string, string>> BuildItemCollection(string filePath)
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

					// add custom fields if you want
					rowDict["UNITCOUNT_SIGN"] = "UNITCOUNT:";

					result.Add(rowDict);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in BuildItemCollection: {ex.Message}");
			}

			return result;
		}



	}
}
