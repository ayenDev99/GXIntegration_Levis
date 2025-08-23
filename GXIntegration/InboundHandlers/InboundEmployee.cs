using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;
using Microsoft.VisualBasic.FileIO;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundEmployee
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunEmployeeSyncAsync(PrismRepository repository)
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

				Logger.Log("Starting E sync...");

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

				// Find all files that start with LSPI_WD_
				string[] files = Directory.GetFiles(inboundDir, "LSPI_WD_*.*");

				if (files.Length == 0)
				{
					Logger.Log("No LSPI_WD_ files found in: " + inboundDir);
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
							Console.WriteLine($"{kv.Key}: {kv.Value}");
						}

						var prism_store = await repository.GetRpsStore(row["StoreCode"]?.ToString());
						Console.WriteLine("SID :" + (prism_store ?? ""));

						var employeeData = new Dictionary<string, object>
						{
							["active"]						= Convert.ToBoolean(row["Active"])
							, ["basestoresid"]				= prism_store?.SID.ToString() ?? null
							, ["firstname"]					= row["Firstname"]?.ToString()
							, ["lastname"]					= row["Lastname"]?.ToString()
							, ["hiredate"]					= row["HireDate"]?.ToString()
							, ["jobsid"]					= await repository.GetRpsJobSid(row["JobTitle"]?.ToString())
							, ["jobtitle"]					= "Manager"
							, ["originapplication"]			= "RProPrismWeb"
							, ["origsbssid"]				= "555356986000134257"
							, ["status"]					= 1
							, ["useractive"]				= true
							, ["username"]					= row["UserName"]?.ToString()
							, ["employeesubsidiary"]		= new[]
								{
									new {
										accessallstores		= true
										, originapplication = "PrismWeb"
										, sbssid			= "555356986000134257"
									}
								}
							, ["empladdress"] = new[]
								{
									new {
										active = true
										, address1			= row["WorkAddress"]?.ToString()
										, address2			= row["WorkCity"]?.ToString()
										, address3			= row["WorkState"]?.ToString()
										, address4			= row["WorkCountry"]?.ToString()
										, postalcode		= row["WorkZipCode"]?.ToString()
									}
								}
							, ["employeeextend"] = new[]
								{
									new {
										udf6string			= row["EffectiveStartDate"]?.ToString()
										, udf7string		= row["EmploymentStatus"]?.ToString()
										, udf10string		= row["EmployeeID"]?.ToString()
										, udf11string		= row["Gender"]?.ToString()
										, udf12string		= row["Language"]?.ToString()
									}
								}
						};

						// Conditionally add 'emplemail'
						string workerEmail = row["WorkerEmail"]?.ToString();
						if (!string.IsNullOrWhiteSpace(workerEmail))
						{
							employeeData["emplemail"] = new[]
							{
								new {
									emailaddress = workerEmail
								}
							};
						}

						// Conditionally add 'emplphone'
						string phoneNumber = row["PhoneNumber"]?.ToString();
						if (!string.IsNullOrWhiteSpace(phoneNumber))
						{
							employeeData["emplphone"] = new[]
							{
								new {
									emailaddress = phoneNumber
								}
							};
						}

						var payload = new { data = new[] { employeeData } };
						var json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);

						Console.WriteLine("Payload:");
						Console.WriteLine(json);

						string responseJson = GlobalInbound.CallPrismAPI(
												session
												, prismAddress
												, "/api/common/employee"
												, json
												, out bool issuccessful
												, "POST");

						Console.WriteLine("Response: " + responseJson);
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in RunEmployeeSyncAsync: {ex.Message}");
				return;
			}
		}

		private List<Dictionary<string, string>> BuildItemCollection(string filePath)
		{
			var result = new List<Dictionary<string, string>>();

			try
			{
				using (var parser = new TextFieldParser(filePath))
				{
					parser.TextFieldType = FieldType.Delimited;
					parser.SetDelimiters(",");
					parser.HasFieldsEnclosedInQuotes = true;

					// Read header
					if (!parser.EndOfData)
					{
						string[] headers = parser.ReadFields();

						while (!parser.EndOfData)
						{
							string[] fields = parser.ReadFields();

							var rowDict = new Dictionary<string, string>();

							for (int i = 0; i < headers.Length; i++)
							{
								string header = headers[i];
								string value = (i < fields.Length ? fields[i].Trim() : string.Empty);
								rowDict[header] = value;
							}

							if (fields.Length > headers.Length)
							{
								Console.WriteLine("Warning: Extra values in line.");
							}

							result.Add(rowDict);
						}
					}
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
