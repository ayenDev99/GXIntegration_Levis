using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundEmployee
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunEmployeeSyncAsync(string session, PrismRepository repository)
		{
			string inboundDir = GlobalInbound.InboundDir;
			string sentDir = GlobalInbound.SentDir;
			string unsentDir = GlobalInbound.UnsentDir;

			try
			{
				Logger.Log($"--------------------------------------------------------------------------");
				Logger.Log("[INBOUND - EMPLOYEE] STARTING EMPLOYEE Sync Process...");

				string fileNameFormat = "LSPI_WD_*.*";
				string sendingDir = Path.Combine(inboundDir, "SENDING");
				var files = globalInbound.GetInboundFiles(sendingDir, fileNameFormat);

				if (files.Count == 0)
				{
					Logger.Log($"[INBOUND - EMPLOYEE] No {fileNameFormat} file format found.");
					return;
				}

				foreach (string file in files)
				{
					bool isSuccess = true;
					string fileName = Path.GetFileName(file);
					Logger.Log($"[INBOUND - EMPLOYEE] Processing file: {fileName}");

					try
					{
						var result = BuildItemCollection(file);
						Logger.Log($"[INBOUND - EMPLOYEE] Records found: {result.Count}");

						int rowIndex = 1;
						foreach (var row in result)
						{
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
							long? empExtendRowVersion = null;
							string employeeSid = null;

							Logger.Log($"[INBOUND - EMPLOYEE] [{rowIndex}] StoreCode : {storeCode} | StoreSID : {baseStoreSid}");

							string workAddress = row["WorkAddress"]?.ToString();
							workAddress = workAddress?.Length > 40 ? workAddress.Substring(0, 40) : workAddress;

							// Get SBS Result
							var sbs_res = await GetSbsResult(repository);
							var sbs_sid = sbs_res[0].SID.ToString();
							Logger.Log($"[INBOUND - EMPLOYEE] SBS SID : {sbs_sid}");

							// Get User Group Result
							string jobTitle = row["JobTitle"]?.ToString();
							var user_group_res = await GetUserGroupResult(repository, jobTitle);
							var user_group_sid = user_group_res[0].SID.ToString();
							Logger.Log($"[INBOUND - EMPLOYEE] USER_GROUP SID : {user_group_sid}");

							//***************************************************************
							// Build employeeextend base structure
							//***************************************************************
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
							, ["jobtitle"]			= row["JobTitle"]?.ToString()
							, ["originapplication"] = "RProPrismWeb"
							, ["origsbssid"]		= sbs_sid
							, ["status"]			= 1
							, ["useractive"]		= true
							, ["username"]			= row["UserName"]?.ToString()
							, ["employeesubsidiary"] = new[]
								{
									new {
										accessallstores     = true
										, originapplication = "PrismWeb"
										, sbssid            = sbs_sid
									}
								}
							, ["empladdress"] = new[]
								{
									new {
										active = true
										, address1          = workAddress
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
							, ["usergroupuser"] = new[]
								{
									new {
										usergroupsid       = user_group_sid
									}
								}
							};

							string workerEmail = row["WorkerEmail"]?.ToString();
							if (!string.IsNullOrWhiteSpace(workerEmail))
								employeeData["emplemail"] = new[] { new { emailaddress = workerEmail } };

							string phoneNumber = row["PhoneNumber"]?.ToString();
							if (!string.IsNullOrWhiteSpace(phoneNumber))
								employeeData["emplphone"] = new[] { new { emailaddress = phoneNumber } };

							//***************************************************************
							// Determine CREATE or UPDATE
							//***************************************************************
							string employeeUsername = row["UserName"]?.ToString().ToUpper();
							var prism_employee = await repository.GetRpsEmployee("USER_NAME", employeeUsername);

							string responseJson = null;

							if (prism_employee == null || prism_employee.Count == 0)
							{
								Logger.Log($"[INBOUND - EMPLOYEE] [CREATE]");
								var payload = new { data = new[] { employeeData } };
								string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);

								responseJson = GlobalInbound.CallPrismAPI(
									session,
									"/api/common/employee",
									json,
									out bool isSuccessfulApi,
									"POST",
									rowIndex
								);

								Logger.Log($"API Response for row {rowIndex}:\n{FormatJson(responseJson)}");

								if (!isSuccessfulApi)
									isSuccess = false;
							}
							else
							{
								Logger.Log($"[INBOUND - EMPLOYEE] [UPDATE]");
								var emp = prism_employee[0];

								// Log employee data
								//Logger.Log($"[INBOUND - EMPLOYEE] Employee Data:\n{JsonConvert.SerializeObject(emp, Formatting.Indented)}");

								try
								{
									if (emp is IDictionary<string, object> empDict)
									{
										employeeRowVersion = empDict.ContainsKey("ROW_VERSION") ? Convert.ToInt64(empDict["ROW_VERSION"]) : (long?)null;
										employeeSid = empDict.ContainsKey("SID") ? empDict["SID"].ToString() : null;
									}

									if (employeeRowVersion.HasValue)
										employeeData["rowversion"] = employeeRowVersion.Value;

									// Get employee extend for proper rowversion
									var prism_employee_extend = await repository.GetRpsEmployeeExtend("EMPLOYEE_SID", employeeSid);

									if (prism_employee_extend != null && prism_employee_extend.Count > 0)
									{
										var empExtend = prism_employee_extend[0];
										if (empExtend is IDictionary<string, object> extendDict && extendDict.ContainsKey("ROW_VERSION"))
										{
											empExtendRowVersion = Convert.ToInt64(extendDict["ROW_VERSION"]);
											employeeExtend["rowversion"] = empExtendRowVersion.Value;
											Logger.Log($"[INBOUND - EMPLOYEE] EMPLOYEE_EXTEND RowVersion: {empExtendRowVersion}");
										}
									}

									employeeData["employeeextend"] = new[] { employeeExtend };

									// Build update payload
									var updatePayload = new { data = new[] { employeeData } };
									string updateJson = JsonConvert.SerializeObject(updatePayload, JsonFormatting.Indented);

									responseJson = GlobalInbound.CallPrismAPI(
										session,
										$"/api/common/employee/{employeeSid}?cols=*,emplphone.*,empladdress.*,emplemail.*,employeeextend.*,employeestore.*,employeesubsidiary.*,usergroupuser.*",
										updateJson,
										out bool isSuccessfulApi,
										"PUT",
										rowIndex
									);

									if (!isSuccessfulApi)
										isSuccess = false;

									Logger.Log($"[INBOUND - EMPLOYEE] API Response: {responseJson}");
								}
								catch (Exception ex)
								{
									Logger.Log($"❌ [INBOUND - EMPLOYEE] Error updating employee: {ex.Message}");
									isSuccess = false;
								}
							}

							rowIndex++;
						}
					}
					catch (Exception ex)
					{
						Logger.Log($"❌ [INBOUND - EMPLOYEE] Error processing file {fileName}: {ex}");
						isSuccess = false;
					}

					// MOVE FILE
					globalInbound.MoveFile(file, isSuccess);
				}

				Logger.Log("[INBOUND - EMPLOYEE] END Sync Process.");
			}
			catch (Exception ex)
			{
				Logger.Log($"❌ [INBOUND - EMPLOYEE] Critical Error in RunEmployeeSyncAsync: {ex}");
			}
		}

		private async Task<List<dynamic>> GetSbsResult(PrismRepository repository)
		{
			// Get default SBS No from config.xml
			XDocument config = XDocument.Load("config.xml");
			var sbs_no = config.Root.Element("EmpSubsidiaries").Element("Subsidiary").Value;
			Logger.Log($"[INBOUND - EMPLOYEE] Config SBS No. to process: {sbs_no}");

			// Fetch from prism subsidiary
			var sbs_result = await repository.GetRpsSubsidiary("SBS_NO", sbs_no);
			
			return sbs_result;
		}

		private async Task<List<dynamic>> GetUserGroupResult(PrismRepository repository, string jobTitle)
		{
			if (jobTitle == "Manager") { 
				jobTitle = "STORE MANAGER";
			} else if (jobTitle == "Cashier") {
				jobTitle = "STORE CASHIER";
			}

			// Fetch from prism user_group
			var user_group_result = await repository.GetRpsUserGroup("USER_GROUP_NAME", jobTitle);

			return user_group_result;
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

		public static string FormatJson(string json)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(json))
					return json;

				var parsedJson = JsonConvert.DeserializeObject(json);
				return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
			}
			catch
			{
				return json; // return raw if it is not valid JSON
			}
		}

	}
}
