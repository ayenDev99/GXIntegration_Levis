using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GXIntegration_Levis.Helpers.GlobalHelper;

namespace GXIntegration_Levis.InboundHandlers
{
	
	public class GlobalInbound
	{
		public async Task<string> Authenticate(string prismAddress, string prismUsername, string prismPassword, string workstationName)
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
						Logger.Log($"Failed to get Auth-Nonce. Status: {response.StatusCode}");
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
						Logger.Log($"Login failed. Status: {response.StatusCode}");
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
						Logger.Log($"Workstation bind failed. Status: {response.StatusCode}");
						return null;
					}
				}

				return authSession;
			}
			catch (WebException ex)
			{
				string errorMessage = "WebException occurred.";
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
				Logger.Log($"Unexpected error: {ex.Message}" + ex);
				return null;
			}
		}

		public async Task<string> AuthenticateFromConfigAsync()
		{
			try
			{
				var config = XDocument.Load("config.xml");

				string prismAddress = config.Root.Element("PrismConfig").Element("Address").Value;
				string prismUsername = config.Root.Element("PrismConfig").Element("Username").Value;
				string prismPassword = config.Root.Element("PrismConfig").Element("Password").Value;
				string workstationName = config.Root.Element("PrismConfig").Element("WorkstationName").Value;

				Logger.Log("Address : " + prismAddress);
				Logger.Log("Username : " + prismUsername);
				Logger.Log("Password : [REDACTED]");
				Logger.Log("Workstation Name : " + workstationName);
				Logger.Log("--------------------------------------------------------------------------");
				Logger.Log("Starting Prism authentication...");

				string session = await Authenticate(prismAddress, prismUsername, prismPassword, workstationName);

				if (string.IsNullOrEmpty(session))
				{
					Logger.Log("❌ Authentication failed.");
					Console.WriteLine("❌ Authentication failed.");
					return null;
				}

				Logger.Log("Authentication successful. Session: " + session);
				return session;
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ Error during AuthenticateFromConfigAsync: {ex}");
				return null;
			}
		}

		public static string CallPrismAPI(string auth_session, string endpoint, string obj, out bool issuccessful, string Method)
		{
			var config = XDocument.Load("config.xml");
			string prismAddress = config.Root.Element("PrismConfig").Element("Address").Value;

			Uri requestUri = new Uri(prismAddress + endpoint);
			string responseContent = string.Empty;
			issuccessful = false;

			HttpWebRequest request = WebRequest.Create(requestUri) as HttpWebRequest;
			request.KeepAlive = false;
			request.Method = Method.ToUpper();
			request.Headers.Add("Auth-Session", auth_session);
			request.Accept = "application/json,text/plain,version=2";
			request.ContentType = "application/json";

			Logger.Log("--------------------------------------------------------------------------");
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
					Logger.Log("DATA SAVED SUCCESSFULLY!");
				}
			}
			catch
			{
				// fallback: raw log if not valid JSON
				Logger.Log("Response: " + responseContent);
			}

			return responseContent;
		}

		public string EnsureInboundDirectory()
		{
			try
			{
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string today = DateTime.Now.ToString("yyyyMMdd");
				string inboundDir = Path.Combine(baseDir, "INBOUND", today);

				if (!Directory.Exists(inboundDir))
				{
					Directory.CreateDirectory(inboundDir);
					Logger.Log("INBOUND folder created: " + inboundDir);
					Console.WriteLine("Created INBOUND directory: " + inboundDir);
				}
				else
				{
					Logger.Log("INBOUND folder already exists: " + inboundDir);
				}

				return inboundDir;
			}
			catch (Exception ex)
			{
				Logger.Log("❌ Failed to ensure INBOUND directory: " + ex);
				Console.WriteLine("❌ Failed to create/check INBOUND directory: " + ex.Message);
				throw;
			}
		}

		public List<string> GetInboundFiles(string inboundDir, string filePattern)
		{
			try
			{
				string[] files = Directory.GetFiles(inboundDir, filePattern);

				if (files.Length == 0)
				{
					Logger.Log($"No '{filePattern}' files found in: {inboundDir}");
					Console.WriteLine($"No '{filePattern}' files found.");
					return new List<string>();
				}

				Logger.Log("Files to be processed --------------------------------------------------------");
				var fileList = new List<string>();
				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file);
					Console.WriteLine(" - " + fileName);
					Logger.Log(fileName);
					fileList.Add(file);
				}
				Logger.Log("-------------------------------------------------------------------------------");

				return fileList;
			}
			catch (Exception ex)
			{
				Logger.Log("❌ Error retrieving inbound files: " + ex);
				Console.WriteLine("❌ Error retrieving inbound files: " + ex.Message);
				return new List<string>();
			}
		}


	}
}
