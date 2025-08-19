using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GXIntegration_Levis.Helpers.GlobalHelper;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.Views
{
	public partial class InboundPage : UserControl
	{
		static GXConfig config;
		private InventoryRepository _inventoryRepository;
		public InboundPage()
		{
			InitializeComponent();
			config = GXConfig.Load("config.xml");
			_inventoryRepository = new InventoryRepository(config.MainDbConnection);

		}

		private async void inventoryButton_Click(object sender, EventArgs e)
		{
			await RunInventorySyncAsync();
		}

		public async Task RunInventorySyncAsync()
		{
			try
			{
				// *** Call authentication first
				string prismAddress = "http://rpro-levis:8080";
				string prismUsername = "sysadmin";
				string prismPassword = "sysadmin";
				string workstationName = "rpro-levis_8080";

				Logger.Log("Starting inventory sync...");

				string session = await Authenticate(prismAddress, prismUsername, prismPassword, workstationName);

				if (string.IsNullOrEmpty(session))
				{
					Logger.Log("Authentication failed.");
					Console.WriteLine("Authentication failed.");
					return;
				}

				Logger.Log("Authenticated. Session: " + session);
				Console.WriteLine(session);

				// *** Continue with snapshot
				string filePath = @"C:\Users\GNX-RPRO.TEAM\Desktop\LSPI_ITEM_20250526100553_0.txt";
				Logger.Log("Loading snapshot file: " + filePath);

				var result = BuildInventorySnapshot(filePath);
				Logger.Log($"Snapshot loaded. Rows found: {result.Count}");

				foreach (var row in result)
				{
					foreach (var kv in row)
					{
						//Console.WriteLine($"{kv.Key}: {kv.Value}");
					}

					//Console.WriteLine("-----------------------------");

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
									description1 = row["PRODUCT_CD"],
									description2 = row["STYLE_CD"],
									attribute    = row["SIZE_DIM2"],	// INSEAM
									itemsize     = row["SIZE_DIM1"]		// Size
								},
								InventoryItems = new[]
								{
									new
									{
										sbssid          = "555356986000134257",
										dcssid          = "556255621000149144",

										description1		= row["PRODUCT_CD"],	// PRODUCT_CD	| description1
										long_description    = row["PRODUCT_NM"],	// PRODUCT_NM	| long_description
										description2		= row["STYLE_CD"],		// STYLE_CD		| description2
										// STYLE_NAME
										// BRAND_CD
										// CONSUMER_CD
										// PROD_CAT_CD
										// CLASS_CD
										// SUB_CLASS_CD
										// SEASON_CD
										alu					= row["PROD_SKU"],		// PROD_SKU		| alu
										itemsize            = row["SIZE_DIM1"],		//	SIZE_DIM1	| Size
										attribute           = row["SIZE_DIM2"],		//	SIZE_DIM2 	| INSEAM
										upc					= row["PROD_GTIN"],		// PROD_GTIN
										description4		= row["PROD_JAN"],		// PROD_JAN
										// AFFILIATE
										text1				= row["SAP_TAX_CD"],	// SAP_TAX_CD
										udf5_string		    = row["DEMAND_NM"],		// DEMAND_NM
										// BRAND_NM
										// SUBCLASS
										// PARTNER
										// CONSUMER_NM
										// PROD_CAT_NM
										// CLASS_NM
										// VENDOR_NM
										cost            = 0,
										spif            = 0,
										taxcodesid      = "555538434000189911",
										useqtydecimals  = 0,
										regional        = false,
										active          = true,
										maxdiscperc1    = 100,
										maxdiscperc2    = 100,
										serialtype      = 0,
										lottype         = 0,
										kittype         = 0,
										tradediscpercent= 0,
										activestoresid  = "555444605000106428",
										activepricelevelsid = "555357012000134500",
										activeseasonsid     = "555357012000192512",
										actstrprice         = 0,
										actstrpricewt       = 0,
										actstrohqty         = 0,
										dcscode             = "1  1  1"
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

					string responseJson = CallPrismAPI(session, prismAddress, "/api/backoffice/inventory?action=InventorySaveItems", json, out bool issuccessful, "POST");
					Console.WriteLine("Response: " + responseJson);

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in RunInventorySyncAsync: {ex.Message}");
				return;
			}
		}

		private List<Dictionary<string, string>> BuildInventorySnapshot(string filePath)
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
				Console.WriteLine($"Error in BuildInventorySnapshot: {ex.Message}");
			}

			return result;
		}

		public async Task<string> Authenticate(
		string prismAddress,
		string prismUsername,
		string prismPassword,
		string workstationName)
		{
			try
			{
				// Step 1: Get Auth-Nonce
				var nonceRequest = WebRequest.CreateHttp($"{prismAddress}/v1/rest/auth");
				nonceRequest.Method = "GET";
				nonceRequest.Accept = "application/json";
				nonceRequest.ContentType = "application/json; charset=UTF-8";

				long nonce;
				using (var response = nonceRequest.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						Logger.Log($"[PrismHelper] Failed to get Auth-Nonce. Status: {response.StatusCode}");
						return null;
					}

					nonce = long.Parse(response.Headers["Auth-Nonce"]);
				}

				// Step 2: Compute Nonce Response
				long nonceResponse = nonce / 13L % 99999L * 17L;

				// Step 3: Authenticate with credentials
				var authUrl = $"{prismAddress}/v1/rest/auth?usr={prismUsername}&pwd={prismPassword}";
				var loginRequest = WebRequest.CreateHttp(authUrl);
				loginRequest.Method = "GET";
				loginRequest.Accept = "application/json";
				loginRequest.ContentType = "application/json; charset=UTF-8";
				loginRequest.Headers.Add("Auth-Nonce", nonce.ToString());
				loginRequest.Headers.Add("Auth-Nonce-Response", nonceResponse.ToString());

				string authSession;
				using (var response = loginRequest.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						Logger.Log($"[PrismHelper] Login failed. Status: {response.StatusCode}");
						return null;
					}

					authSession = response.Headers["Auth-Session"];
				}

				// Step 4: Bind session to workstation
				var sitUrl = $"{prismAddress}/v1/rest/sit?ws={workstationName}";
				var sitRequest = WebRequest.CreateHttp(sitUrl);
				sitRequest.Method = "GET";
				sitRequest.Accept = "application/json";
				sitRequest.ContentType = "application/json; charset=UTF-8";
				sitRequest.Headers.Add("Auth-Session", authSession);

				using (var response = sitRequest.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						Logger.Log($"[PrismHelper] Workstation bind failed. Status: {response.StatusCode}");
						return null;
					}
				}

				Logger.Log("[PrismHelper] Authentication successful.");
				return authSession;
			}
			catch (WebException ex)
			{
				string errorMessage = "[PrismHelper] WebException occurred.";
				if (ex.Response != null)
				{
					using (var reader = new StreamReader(ex.Response.GetResponseStream()))
					{
						var errorResponse = reader.ReadToEnd();
						errorMessage += $" Response: {errorResponse}";
						Logger.Log(errorMessage + ex);
						return errorResponse;
					}
				}

				Logger.Log($"{errorMessage} Exception: {ex.Message}" + ex);
				return null;
			}
			catch (Exception ex)
			{
				Logger.Log($"[PrismHelper] Unexpected error: {ex.Message}" + ex);
				return null;
			}
		}

		public static string CallPrismAPI(
			string auth_session,
			string TargetHost,
			string endpoint,
			string obj,
			out bool issuccessful,
			string Method)
		{
			Uri requestUri = new Uri(TargetHost + endpoint);
			string responseContent = string.Empty;
			issuccessful = false;

			HttpWebRequest request = WebRequest.Create(requestUri) as HttpWebRequest;
			request.KeepAlive = false;
			request.Method = Method.ToUpper();
			request.Headers.Add("Auth-Session", auth_session);
			request.Accept = "application/json,text/plain,version=2";
			request.ContentType = "application/json";

			Logger.Log("-------------------------------------------------------------------------------");
			Logger.Log("Calling Prism API...");
			Logger.Log($"URL: {requestUri}");
			Logger.Log($"Method: {request.Method}");

			if (Method != "GET")
			{
				//Logger.Log("Payload:");
				//Logger.Log(string.IsNullOrWhiteSpace(obj) ? "[Empty Body]" : obj);
			}

			try
			{
				if (Method != "GET" && !string.IsNullOrWhiteSpace(obj))
				{
					byte[] bytes = Encoding.UTF8.GetBytes(obj);
					request.ContentLength = bytes.Length;

					using (Stream requestStream = request.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
				}

				using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				using (StreamReader reader = new StreamReader(response.GetResponseStream()))
				{
					responseContent = reader.ReadToEnd();
					issuccessful = true;
				}
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.Timeout)
				{
					responseContent = "Timeout Error";
				}
				else if (ex.Response != null)
				{
					using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
					{
						responseContent = reader.ReadToEnd();
					}
				}
				else
				{
					responseContent = "Unhandled WebException: " + ex.Message;
				}

				issuccessful = false;
				Logger.Log("API call failed");
				//Logger.Log("API call failed" + ex);
			}

			// 🔎 Try to extract only the errormsg
			try
			{
				var errorResponse = JsonConvert.DeserializeObject<PrismErrorResponse>(responseContent);
				if (errorResponse?.errors != null && errorResponse.errors.Count > 0)
				{
					string errorMsg = errorResponse.errors[0].errormsg;
					Logger.Log("Prism Error: " + errorMsg);
				}
				else
				{
					Logger.Log("Response: " + responseContent);
				}
			}
			catch
			{
				// fallback: raw log if not valid JSON
				Logger.Log("Response: " + responseContent);
			}

			return responseContent;
		}


	}

}
