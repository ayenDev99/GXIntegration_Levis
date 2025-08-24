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

		// Column to UDF Mapping
		private readonly Dictionary<string, string> columnToUdfMap = new Dictionary<string, string>
		{
			{ "BRAND_CD", "UDF6" },
			{ "BRAND_NM", "UDF1" },
			{ "CONSUMER_CD", "UDF10" },
			{ "CONSUMER_NM", "UDF11" },
			{ "PROD_CAT_CD", "UDF2" },
			{ "PROD_CAT_NM", "UDF4" },
			{ "CLASS_CD", "UDF12" },
			{ "CLASS_NM", "UDF13" },
			{ "SUB_CLASS_CD", "UDF14" },
			{ "SUB_CLASS_NM", "UDF3" }
		};

		public async Task RunHierarchySyncAsync(string session, string inboundDir, InboundHierarchyRepository repository)
		{
			try
			{
				Logger.Log("");
				Logger.Log("**************************************************************************");
				Logger.Log(">>> Starting INBOUND HIERARCHY Sync Process...");
				Logger.Log("**************************************************************************");

				string fileNameFormat = "LSPI_HIERARCHY_*.*";

				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) return;

				foreach (string file in files)
				{
					Logger.Log($"\n📁 Starting processing for file: {Path.GetFileName(file)}");

					var udfData = BuildHierarchyByUdf(file);

					// Extract distinct, non-empty values
					var brandCodes = udfData.TryGetValue("UDF6", out var bc) ? bc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var brandNames = udfData.TryGetValue("UDF1", out var bn) ? bn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var consumerCodes = udfData.TryGetValue("UDF10", out var cc) ? cc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var consumerNames = udfData.TryGetValue("UDF11", out var cn) ? cn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var productCategoryCodes = udfData.TryGetValue("UDF2", out var pc) ? pc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var productCategoryNames = udfData.TryGetValue("UDF4", out var pn) ? pn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var classCodes = udfData.TryGetValue("UDF12", out var clsC) ? clsC.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var classNames = udfData.TryGetValue("UDF13", out var clsN) ? clsN.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var subClassCodes = udfData.TryGetValue("UDF14", out var sc) ? sc.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();
					var subClassNames = udfData.TryGetValue("UDF3", out var sn) ? sn.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : new List<string>();

					var filteredUdfValues = new Dictionary<string, List<string>>
					{
						{ "6", brandCodes },
						{ "1", brandNames },
						{ "10", consumerCodes },
						{ "11", consumerNames },
						{ "2", productCategoryCodes },
						{ "4", productCategoryNames },
						{ "12", classCodes },
						{ "13", classNames },
						{ "14", subClassCodes },
						{ "3", subClassNames }
					};

					var SBS_result = await repository.GetSbsListAsync();

					Logger.Log("Retrieved SBS List");

					foreach (var sbsItem in SBS_result)
					{
						Logger.Log($"\nChecking SBS: {sbsItem.SBS_NAME} (SID: {sbsItem.SID})");

						foreach (var udfType in filteredUdfValues)
						{
							Logger.Log($"\nChecking UDF Type: UDF{udfType.Key} with {udfType.Value.Count} values");

							foreach (var udfValue in udfType.Value)
							{
								Logger.Log($"Checking if UDF value '{udfValue}' exists for UDF{udfType.Key}");

								var udf_result = await repository.GetUdfDetailsAsync(udfType.Key, udfValue, sbsItem.SID.ToString());
								Logger.Log(udf_result.Count);
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
									Logger.Log("Payload:\n" + json);

									string responseJson = GlobalInbound.CallPrismAPI(
															session
															, "/api/backoffice/invnudfoption"
															, json
															, out bool isSuccessful
															, "POST"
														);

									Logger.Log($"API Response: {responseJson}");

									continue;
								}

								foreach (var udf_res in udf_result)
								{
									if (udf_res.UDF_OPTION == null)
									{
										var udfSid = udf_res.UDF_SID;

										Logger.Log($"Missing value detected: '{udfValue}' (UDF_SID: {udfSid})");
										Logger.Log("Preparing payload to insert...");

									}
									else
									{
										Logger.Log("UDF value '{udfValue}' already exists.");
									}
								}
							}
						}
					}
				}

				Logger.Log("Hierarchy sync process completed.");

			}
			catch (Exception ex)
			{
				Logger.Log($"Error in RunHierarchySyncAsync: {ex}");
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
					Console.WriteLine("File is empty: " + filePath);
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

				Logger.Log($"Parsed {result.Count} UDF-mapped columns from file: {Path.GetFileName(filePath)}");
			}
			catch (Exception ex)
			{
				Logger.Log($"Error in BuildHierarchyByUdf: {ex}");
			}

			return result;
		}
	
	}
}
