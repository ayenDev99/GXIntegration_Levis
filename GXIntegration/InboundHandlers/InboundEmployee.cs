using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JsonFormatting = Newtonsoft.Json.Formatting;
using Microsoft.VisualBasic.FileIO;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundEmployee
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunEmployeeSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			try
			{
				Logger.Log("");
				Logger.Log("**************************************************************************");
				Logger.Log(">>> Starting INBOUND EMPLOYEE Sync Process...");
				Logger.Log("**************************************************************************");

				string fileNameFormat = "LSPI_WD_*.*";

				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) return;

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
						Logger.Log("Payload:\n" + json);

						string responseJson = GlobalInbound.CallPrismAPI(
												session
												, "/api/common/employee"
												, json
												, out bool issuccessful
												, "POST");

						Logger.Log($"API Response: {responseJson}");

						continue;
					}
				}
				Logger.Log("Employee sync process completed.");
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
								Logger.Log("Warning: Extra values in line.");
							}

							result.Add(rowDict);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"Error in BuildItemCollection: {ex.Message}");
			}

			return result;
		}

	}
}
