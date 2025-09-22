using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundEmployee
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunEmployeeSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			try
			{
				Logger.Log($"--------------------------------------------------------------------------");
				Logger.Log("[INBOUND - EMPLOYEE] STARTING EMPLOYEE Sync Process...");

				string fileNameFormat = "LSPI_WD_*.*";
				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) { Logger.Log($"[INBOUND - EMPLOYEE] No {fileNameFormat} file format found."); }

				foreach (string file in files)
				{
					var result = BuildItemCollection(file);
					string fileName = Path.GetFileName(file);

					Logger.Log($"[INBOUND - EMPLOYEE] -> {fileName}");
					Logger.Log($"[INBOUND - EMPLOYEE] Records found: {result.Count}");

					int rowIndex = 1;
					foreach (var row in result)
					{
						foreach (var kv in row)
						{
							// !Note:Uncomment for debugging file content.
							//Logger.Log($"{kv.Key}: {kv.Value}");
						}

						var storeCode = row["StoreCode"]?.ToString();
						var prism_store = await repository.GetRpsStore("ADDRESS4", storeCode);

						if (prism_store == null || prism_store.Count == 0)
						{
							Logger.Log($"[INBOUND - EMPLOYEE] [{rowIndex}] StoreCode : {storeCode} is not existing.");
							rowIndex++;
							continue;
						}

						var baseStoreSid = prism_store[0].SID.ToString();
						long? employeeRowVersion = null;
						string employeeSid = null;

						//Logger.Log($"[INBOUND - EMPLOYEE] prism_store has {prism_store.Count} item(s). First item: {JsonConvert.SerializeObject(firstItem, Formatting.Indented)}");
						Logger.Log($"[INBOUND - EMPLOYEE] [{rowIndex}] StoreCode : {storeCode} | SID : {baseStoreSid}");

						//***********************************************************************
						//  Build employeeextend with optional rowversion
						//***********************************************************************
						var employeeExtend = new Dictionary<string, object>
						{
							["udf6string"]		= row["EffectiveStartDate"]?.ToString()
							, ["udf7string"]	= row["EmploymentStatus"]?.ToString()
							, ["udf10string"]	= row["EmployeeID"]?.ToString()
							, ["udf11string"]	= row["Gender"]?.ToString()
							, ["udf12string"]	= row["Language"]?.ToString()
						};

						var employeeData = new Dictionary<string, object>
						{
							["active"]				= Convert.ToBoolean(row["Active"])
							, ["basestoresid"]		= baseStoreSid
							, ["firstname"]			= row["Firstname"]?.ToString()
							, ["lastname"]			= row["Lastname"]?.ToString()
							, ["hiredate"]			= row["HireDate"]?.ToString()
							, ["jobsid"]			= await repository.GetRpsJobSid(row["JobTitle"]?.ToString())
							, ["jobtitle"]			= "Manager"
							, ["originapplication"] = "RProPrismWeb"
							, ["origsbssid"]		= "555356986000134257"
							, ["status"]			= 1
							, ["useractive"]		= true
							, ["username"]			= row["UserName"]?.ToString()
							, ["employeesubsidiary"] = new[]
								{
									new {
										accessallstores     = true
										, originapplication = "PrismWeb"
										, sbssid            = "555356986000134257"
									}
								}
							, ["empladdress"] = new[]
								{
									new {
										active = true
										, address1          = row["WorkAddress"]?.ToString()
										, address2          = row["WorkCity"]?.ToString()
										, address3          = row["WorkState"]?.ToString()
										, address4          = row["WorkCountry"]?.ToString()
										, postalcode        = row["WorkZipCode"]?.ToString()
									}
								}
							, ["employeeextend"] = new[]
								{
									new {
										udf6string          = row["EffectiveStartDate"]?.ToString()
										, udf7string        = row["EmploymentStatus"]?.ToString()
										, udf10string       = row["EmployeeID"]?.ToString()
										, udf11string       = row["Gender"]?.ToString()
										, udf12string       = row["Language"]?.ToString()
									}
								}
						};

						string workerEmail = row["WorkerEmail"]?.ToString();
						if (!string.IsNullOrWhiteSpace(workerEmail))
						{
							employeeData["emplemail"] = new[] { new { emailaddress = workerEmail } };
						}

						string phoneNumber = row["PhoneNumber"]?.ToString();
						if (!string.IsNullOrWhiteSpace(phoneNumber))
						{
							employeeData["emplphone"] = new[] { new { emailaddress = phoneNumber } };
						}
						
						//***********************************************************************
						// Get RPS.EMPLOYEE
						string employeeUsername = row["UserName"]?.ToString().ToUpper();
						var prism_employee = await repository.GetRpsEmployee("USER_NAME", employeeUsername);

						long? empExtendRowVersion = null;
						string responseJson = null;

						// Check for duplicate error and proceed to UPDATE | PUT
						if (prism_employee == null || prism_employee.Count == 0)
						{
							Logger.Log($"[INBOUND - EMPLOYEE]		[CREATE]");
							// Call API to CREATE | POST employee
							string endpointCreate = "/api/common/employee";
							var payload = new { data = new[] { employeeData } };
							string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
							responseJson = GlobalInbound.CallPrismAPI(
													session
													, endpointCreate
													, json
													, out bool issuccessful
													, "POST"
													, rowIndex);

							// !Note:Uncomment for debugging file content.
							//Logger.Log("[INBOUND - EMPLOYEE] Payload:\n" + json);
						}
						else
						{
							var empFirstItem = prism_employee[0];

							Logger.Log($"[INBOUND - EMPLOYEE]		[UPDATE]");

							employeeRowVersion = empFirstItem.ROW_VERSION;
							employeeSid = empFirstItem.SID.ToString();
							//Logger.Log(JsonConvert.SerializeObject(empFirstItem, Formatting.Indented));

							// Get RPS.EMPLOYEE_EXTEND
							var prism_employee_extend = await repository.GetRpsEmployeeExtend("EMPLOYEE_SID", employeeSid);
							//Logger.Log(JsonConvert.SerializeObject(empExtendFirstItem, Formatting.Indented));

							if (prism_employee_extend == null || prism_employee_extend.Count == 0)
							{
								Logger.Log($"[INBOUND - EMPLOYEE]		EMPLOYEE_EXTEND not found — adding new EMPLOYEE_EXTEND");

								employeeData["employeeextend"] = new[] { employeeExtend };
							}
							else
							{
								var empExtendFirstItem = prism_employee_extend[0];
								empExtendRowVersion = empExtendFirstItem.ROW_VERSION;

								Logger.Log($"[INBOUND - EMPLOYEE]		EMPLOYEE_EXTEND found — updating existing EMPLOYEE_EXTEND");

								employeeExtend["rowversion"] = empExtendRowVersion;
								employeeData["employeeextend"] = new[] { employeeExtend };
							}

							// Required fields for update
							if (employeeRowVersion.HasValue)
							{
								employeeData["rowversion"] = employeeRowVersion.Value;
							}

							// Build update payload and send PUT
							var updatePayload = new { data = new[] { employeeData } };
							string updateJson = JsonConvert.SerializeObject(updatePayload, JsonFormatting.Indented);
							string endpointUpdate = $"/api/common/employee/{employeeSid}?cols=*,emplphone.*,empladdress.*,emplemail.*,employeeextend.*,employeestore.*,employeesubsidiary.*,usergroupuser.*";

							responseJson = GlobalInbound.CallPrismAPI(
											session
											, endpointUpdate
											, updateJson
											, out bool isSuccessful
											, "PUT"
											, rowIndex);

						}

						Console.WriteLine($"[INBOUND - EMPLOYEE]		API Response: {responseJson}");
						rowIndex++;
						continue;
					}

					rowIndex++;
					continue;
				}
				
				Logger.Log("[INBOUND - EMPLOYEE] END Sync Process.");
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND - EMPLOYEE] Error in RunEmployeeSyncAsync: {ex.Message}");
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
								Logger.Log("[INBOUND - EMPLOYEE] WARNING : Extra values in line.");
							}

							result.Add(rowDict);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND - EMPLOYEE] Error in BuildItemCollection: {ex.Message}");
			}

			return result;
		}

	}
}
