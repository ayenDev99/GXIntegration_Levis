using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static GXIntegration_Levis.Helpers.GlobalHelper;
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
				Logger.Log("[INBOUND] Starting EMPLOYEE Sync Process...");

				string fileNameFormat = "LSPI_WD_*.*";

				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) return;

				foreach (string file in files)
				{
					var result = BuildItemCollection(file);
					string fileName = Path.GetFileName(file);

					Logger.Log($"[INBOUND] -> {fileName}");
					Logger.Log($"[INBOUND] Records found: {result.Count}");

					int rowIndex = 1;
					foreach (var row in result)
					{
						foreach (var kv in row)
						{
							// !Note:Uncomment for debugging file content.
							//Logger.Log($"{kv.Key}: {kv.Value}");
						}

						var storeCode = row["StoreCode"]?.ToString();
						var prism_store = await repository.GetRpsStore("ADDRESS5", storeCode);

						if (prism_store == null || prism_store.Count == 0)
						{
							Logger.Log($"[INBOUND] [{rowIndex}] StoreCode is not existing : {storeCode}");
							rowIndex++;
							continue;
						}

						var baseStoreSid = prism_store[0].SID.ToString();
						long? employeeRowVersion = null; // 🟡 Add this line
						string employeeSid = null;

						//Logger.Log($"[INBOUND] prism_store has {prism_store.Count} item(s). First item: {JsonConvert.SerializeObject(firstItem, Formatting.Indented)}");
						Logger.Log($"[INBOUND] [{rowIndex}] StoreCode : {storeCode} | SID : {baseStoreSid}");

						// 🛠 Build employeeextend with optional rowversion
						var employeeExtend = new Dictionary<string, object>
						{
							["udf6string"] = row["EffectiveStartDate"]?.ToString(),
							["udf7string"] = row["EmploymentStatus"]?.ToString(),
							["udf10string"] = row["EmployeeID"]?.ToString(),
							["udf11string"] = row["Gender"]?.ToString(),
							["udf12string"] = row["Language"]?.ToString()
						};

						var employeeData = new Dictionary<string, object>
						{
							["active"] = Convert.ToBoolean(row["Active"])
							,
							["basestoresid"] = baseStoreSid
							,
							["firstname"] = row["Firstname"]?.ToString()
							,
							["lastname"] = row["Lastname"]?.ToString()
							,
							["hiredate"] = row["HireDate"]?.ToString()
							,
							["jobsid"] = await repository.GetRpsJobSid(row["JobTitle"]?.ToString())
							,
							["jobtitle"] = "Manager"
							,
							["originapplication"] = "RProPrismWeb"
							,
							["origsbssid"] = "555356986000134257"
							,
							["status"] = 1
							,
							["useractive"] = true
							,
							["username"] = row["UserName"]?.ToString()
							,
							["employeesubsidiary"] = new[]
								{
									new {
										accessallstores     = true
										, originapplication = "PrismWeb"
										, sbssid            = "555356986000134257"
									}
								}
							,
							["empladdress"] = new[]
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
							//,
							//["employeeextend"] = new[]
							//	{
							//		new {
							//			udf6string          = row["EffectiveStartDate"]?.ToString()
							//			, udf7string        = row["EmploymentStatus"]?.ToString()
							//			, udf10string       = row["EmployeeID"]?.ToString()
							//			, udf11string       = row["Gender"]?.ToString()
							//			, udf12string       = row["Language"]?.ToString()
							//		}
							//	}
						};

						// Conditionally add 'emplemail'
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


						// 🔁 Call API to create employee
						string endpointCreate = "/api/common/employee";
						var payload = new { data = new[] { employeeData } };
						string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
						string responseJson = GlobalInbound.CallPrismAPI(
							session, endpointCreate, json,
							out bool issuccessful, "POST");

						var errorResponse = JsonConvert.DeserializeObject<PrismErrorResponse>(responseJson);

						// !Note:Uncomment for debugging file content.
						//Logger.Log("[INBOUND] Payload:\n" + json);

						if (GlobalInbound.IsDuplicateError(errorResponse))
						{
							string employeeUsername = row["UserName"]?.ToString().ToUpper();
							var prism_employee = await repository.GetRpsEmployee("USER_NAME", employeeUsername);

							string endpointGet = $"/api/common/employee?filters=USER_NAME eq '{employeeUsername}'";
							string getResponse = GlobalInbound.CallPrismAPI(
	session,
	endpointGet,
	string.Empty,
	out bool isSuccessfulGet,
	"GET");

							long? empExtendRowVersion = null;

							if (prism_employee != null && prism_employee.Count > 0)
							{
								var empFirstItem = prism_employee[0];
								employeeRowVersion = empFirstItem.ROW_VERSION;
								employeeSid = empFirstItem.SID.ToString();

								Logger.Log(JsonConvert.SerializeObject(empFirstItem, Formatting.Indented));

								var extendItem = empFirstItem.employeeextend[0];

								//string extendJson = JsonConvert.SerializeObject(extendItem, Formatting.Indented);
								//Logger.Log($"TEST\n{extendJson}");


								empExtendRowVersion = extendItem.rowversion;

						
								Logger.Log($"[INBOUND] Employee SID : {employeeSid}");
								Logger.Log($"[INBOUND] ROW VERSION : {employeeRowVersion}");

								// 🔁 ✅ Add rowversion to employeeData for update
								if (employeeRowVersion.HasValue)
								{
									employeeData["rowversion"] = employeeRowVersion.Value;
									employeeExtend["rowversion"] = empExtendRowVersion.Value;
								}

								employeeData["employeeextend"] = new[] { employeeExtend };

								// 🔄 Build update payload and send PUT
								var updatePayload = new { data = new[] { employeeData } };
								string updateJson = JsonConvert.SerializeObject(updatePayload, JsonFormatting.Indented);

								string endpointUpdate = $"/api/common/employee/{employeeSid}?cols=*,emplphone.*,empladdress.*,emplemail.*,employeeextend.*,employeestore.*,employeesubsidiary.*,usergroupuser.*";

								responseJson = GlobalInbound.CallPrismAPI(
									session, endpointUpdate, updateJson,
									out bool isSuccessful, "PUT");
							}

							Console.WriteLine($"[INBOUND] API Response: {responseJson}");
						}
					}

						rowIndex++;
						continue;
					}
				
				Logger.Log("[INBOUND] Employee sync process completed.");
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND] Error in RunEmployeeSyncAsync: {ex.Message}");
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
								Logger.Log("[INBOUND] WARNING : Extra values in line.");
							}

							result.Add(rowDict);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND] Error in BuildItemCollection: {ex.Message}");
			}

			return result;
		}

	}
}
