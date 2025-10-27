using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace GXIntegration_Levis.InboundHandlers
{
		public class InboundHierarchy
		{
			private readonly GlobalInbound globalInbound = new GlobalInbound();

			private readonly Dictionary<string, string> columnToUdfMap = new Dictionary<string, string>
			{
				{ "BRAND_CD", "UDF6" },
				{ "BRAND_NM", "UDF7" },
				{ "CONSUMER_CD", "UDF10" },
				{ "CONSUMER_NM", "UDF11" },
				{ "PROD_CAT_CD", "UDF2" },
				{ "PROD_CAT_NM", "UDF4" },
				{ "CLASS_CD", "UDF12" },
				{ "CLASS_NM", "UDF13" },
				{ "SUB_CLASS_CD", "UDF14" },
				{ "SUB_CLASS_NM", "UDF3" }
			};

			public async Task RunHierarchySyncAsync(string session, PrismRepository repository)
			{
				string inboundDir = GlobalInbound.InboundDir;
				string sentDir = GlobalInbound.SentDir;
				string unsentDir = GlobalInbound.UnsentDir;

				try
				{
					Logger.Log("--------------------------------------------------------------------------");
					Logger.Log("[INBOUND - HIERARCHY] STARTING HIERARCHY Sync Process...");

					string fileNameFormat = "LSPI_HIERARCHY_*.*";
					string sendingDir = Path.Combine(inboundDir, "SENDING");
					var files = globalInbound.GetInboundFiles(sendingDir, fileNameFormat);
					if (files.Count == 0)
					{
						Logger.Log($"[INBOUND - HIERARCHY] No {fileNameFormat} file format found.");
						return;
					}

					foreach (string file in files)
					{
						string fileName = Path.GetFileName(file);
						bool isSuccess = true;

						Logger.Log($"[INBOUND - HIERARCHY] Processing file: {fileName}");

						try
						{
							var udfData = BuildHierarchyByUdf(file);
							Logger.Log($"[INBOUND - HIERARCHY] UDF_NO Records found: {udfData.Count}");

							// Extract and prepare data
							var filteredUdfValues = new Dictionary<string, List<string>>
						{
							{ "6", udfData.TryGetValue("UDF6", out var bc) ? bc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "7", udfData.TryGetValue("UDF7", out var bn) ? bn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "10", udfData.TryGetValue("UDF10", out var cc) ? cc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "11", udfData.TryGetValue("UDF11", out var cn) ? cn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "2", udfData.TryGetValue("UDF2", out var pc) ? pc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "4", udfData.TryGetValue("UDF4", out var pn) ? pn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "12", udfData.TryGetValue("UDF12", out var clsC) ? clsC.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "13", udfData.TryGetValue("UDF13", out var clsN) ? clsN.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "14", udfData.TryGetValue("UDF14", out var sc) ? sc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() },
							{ "3", udfData.TryGetValue("UDF3", out var sn) ? sn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>() }
						};

							// Fetch subsidiaries
							var SBS_result = await repository.GetRpsSubsidiary("ACTIVE", "1");
							Logger.Log($"[INBOUND - HIERARCHY] SBS Count : {SBS_result.Count}");

							int rowIndex = 1;
							foreach (var sbsItem in SBS_result)
							{
								Logger.Log($"[INBOUND - HIERARCHY] [{rowIndex}] SBS Name : {sbsItem.SBS_NAME} | SBS_NO : {sbsItem.SBS_NO} | SID: {sbsItem.SID}");

								foreach (var udfType in filteredUdfValues)
								{
									foreach (var udfValue in udfType.Value)
									{
										try
										{
											var udf_result = await repository.GetUdfDetailsAsync(udfType.Key, udfValue, sbsItem.SID.ToString());

											if (udf_result == null || udf_result.Count == 0)
											{
												var invn_udf_res = await repository.GetInvnUdfSidAsync(udfType.Key, sbsItem.SID.ToString());

												var payload = new
												{
													data = new[]
													{
													new
													{
														OriginApplication = "RProPrismWeb",
														udfoption = udfValue,
														udfsid = invn_udf_res[0].SID.ToString()
													}
												}
												};

												var json = JsonConvert.SerializeObject(payload, Formatting.Indented);

												Logger.Log($"[INBOUND - HIERARCHY] [CREATE] UDF_NO : {udfType.Key} | Value: '{udfValue}'");

												string responseJson = GlobalInbound.CallPrismAPI(
													session,
													"/api/backoffice/invnudfoption",
													json,
													out bool isSuccessfulApi,
													"POST",
													1
												);

												if (!isSuccessfulApi)
												{
													Logger.Log($"❌ [INBOUND - HIERARCHY] API failed for UDF_NO : {udfType.Key} | Value: '{udfValue}'");
													isSuccess = false;
												}
											}
											else
											{
												Logger.Log($"[INBOUND - HIERARCHY] UDF_NO : {udfType.Key} | Value: '{udfValue}' already exists.");
											}
										}
										catch (Exception innerEx)
										{
											Logger.Log($"❌ [INBOUND - HIERARCHY] Error processing UDF Value '{udfValue}': {innerEx.Message}");
											isSuccess = false;
										}
									}
								}

								rowIndex++;
							}
						}
						catch (Exception ex)
						{
							Logger.Log($"❌ [INBOUND - HIERARCHY] Error processing file {fileName}: {ex}");
							isSuccess = false;
						}

					// MOVE FILE
					globalInbound.MoveFile(file, isSuccess);
				}

				Logger.Log("[INBOUND - HIERARCHY] END Sync Process.");
				}
				catch (Exception ex)
				{
					Logger.Log($"❌ [INBOUND - HIERARCHY] Critical Error in RunHierarchySyncAsync: {ex}");
				}
			}

			private Dictionary<string, List<string>> BuildHierarchyByUdf(string filePath)
			{
				var result = new Dictionary<string, List<string>>();

				try
				{
					var lines = File.ReadAllLines(filePath);
					if (lines.Length == 0)
					{
						Logger.Log($"⚠️ [INBOUND - HIERARCHY] File is empty: {filePath}");
						return result;
					}

					var headers = lines[0].Split('^');
					var mappedHeaders = headers.Select(h => columnToUdfMap.ContainsKey(h) ? columnToUdfMap[h] : null).ToArray();

					for (int i = 0; i < headers.Length; i++)
					{
						string udf = mappedHeaders[i];
						if (string.IsNullOrEmpty(udf)) continue;
						result[udf] = new List<string>();
					}

					foreach (var line in lines.Skip(1))
					{
						var parts = line.Split('^');

						for (int i = 0; i < headers.Length; i++)
						{
							string udf = mappedHeaders[i];
							if (string.IsNullOrEmpty(udf)) continue;

							string value = (i < parts.Length) ? parts[i] : string.Empty;
							result[udf].Add(value);
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Log($"❌ [INBOUND - HIERARCHY] Error in BuildHierarchyByUdf: {ex.Message}");
				}

				return result;
			}
		}
	}

