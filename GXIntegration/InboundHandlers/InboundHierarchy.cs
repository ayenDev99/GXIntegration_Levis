using GXIntegration_Levis.Data.Access;
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

		public async Task RunHierarchySyncAsync(InboundHierarchyRepository repository)
		{
			try
			{
				Console.WriteLine(">>> Starting Hierarchy Sync Process...");

				var config = XDocument.Load("config.xml");

				string prismAddress = config.Root.Element("PrismConfig").Element("Address").Value;
				string prismUsername = config.Root.Element("PrismConfig").Element("Username").Value;
				string prismPassword = config.Root.Element("PrismConfig").Element("Password").Value;
				string workstationName = config.Root.Element("PrismConfig").Element("WorkstationName").Value;

				Logger.Log("Credentials : ...");
				Logger.Log("Address : " + prismAddress);
				Logger.Log("Username : " + prismUsername);
				Logger.Log("Password : [REDACTED]");
				Logger.Log("Workstation Name : " + workstationName);
				Logger.Log("--------------------------------------------------------------------------");

				Console.WriteLine("Authenticating with Prism...");
				string session = await globalInbound.Authenticate(
					prismAddress, prismUsername, prismPassword, workstationName);

				if (string.IsNullOrEmpty(session))
				{
					Logger.Log("Authentication failed.");
					Console.WriteLine("❌ Authentication failed.");
					return;
				}

				Logger.Log("Authenticated. Session: " + session);
				Console.WriteLine("✅ Authentication successful. Session: " + session);

				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string today = DateTime.Now.ToString("yyyyMMdd");
				string inboundDir = Path.Combine(baseDir, "INBOUND", today);

				if (!Directory.Exists(inboundDir))
				{
					Directory.CreateDirectory(inboundDir);
					Logger.Log("INBOUND folder created : " + inboundDir);
					Console.WriteLine("📁 Created INBOUND directory: " + inboundDir);
				}

				Console.WriteLine("Looking for files in directory: " + inboundDir);
				string[] files = Directory.GetFiles(inboundDir, "LSPI_HIERARCHY_*.*");

				if (files.Length == 0)
				{
					Logger.Log("No LSPI_HIERARCHY_ files found in: " + inboundDir);
					Console.WriteLine("⚠️ No LSPI_HIERARCHY_ files found.");
					return;
				}

				Console.WriteLine($"📄 {files.Length} file(s) found:");
				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file);
					Console.WriteLine(" - " + fileName);
					Logger.Log(fileName);
				}

				Console.WriteLine("--------------------------------------------------------------------------");

				foreach (string file in files)
				{
					Console.WriteLine($"\n📁 Starting processing for file: {Path.GetFileName(file)}");

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

					Console.WriteLine("📜 Retrieved SBS List");

					foreach (var sbsItem in SBS_result)
					{
						Console.WriteLine($"\n🏬 Checking SBS: {sbsItem.SBS_NAME} (SID: {sbsItem.SID})");

						foreach (var udfType in filteredUdfValues)
						{
							Console.WriteLine($"\n🔎 Checking UDF Type: UDF{udfType.Key} with {udfType.Value.Count} values");

							foreach (var udfValue in udfType.Value)
							{
								Console.WriteLine($"   ➤ Checking if UDF value '{udfValue}' exists for UDF{udfType.Key}");

								var udf_result = await repository.GetUdfDetailsAsync(udfType.Key, udfValue, sbsItem.SID.ToString());
								Console.WriteLine(udf_result.Count);
								if (udf_result == null || udf_result.Count == 0)
								{
										var invn_udf_res = await repository.GetInvnUdfSidAsync(udfType.Key, sbsItem.SID.ToString());

									//	if (invn_udf_res == null)
									//	{
									//		Console.WriteLine($"   ⚠️ invn_udf_res is empty for UDF{udfType.Key} / SBS SID {sbsItem.SID}");
									//		continue; // or handle as needed
									//	}

									//	// Safe access
									//	var udfSid = invn_udf_res.SID.ToString();




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
									Console.WriteLine("   📦 Payload:\n" + json);

									string responseJson = GlobalInbound.CallPrismAPI(
										session,
										prismAddress,
										"/api/backoffice/invnudfoption",
										json,
										out bool isSuccessful,
										"POST"
									);

									Console.WriteLine($"   📬 API Response: {responseJson}");

									continue;
								}

								foreach (var udf_res in udf_result)
								{
									if (udf_res.UDF_OPTION == null)
									{
										var udfSid = udf_res.UDF_SID;

										Console.WriteLine($"   ➕ Missing value detected: '{udfValue}' (UDF_SID: {udfSid})");
										Console.WriteLine("   ⏳ Preparing payload to insert...");

									}
									else
									{
										Console.WriteLine($"   ✅ UDF value '{udfValue}' already exists.");
									}
								}
							}
						}
					}
				}

				Console.WriteLine("\n✅ Hierarchy sync process completed.");




			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error in RunHierarchySyncAsync: {ex}");
				Logger.Log($"Error in RunHierarchySyncAsync: {ex}");
			}
		}

		/// <summary>
		/// Reads file and maps columns to UDF codes with corresponding values.
		/// </summary>
		private Dictionary<string, List<string>> BuildHierarchyByUdf(string filePath)
		{
			var result = new Dictionary<string, List<string>>();

			try
			{
				var lines = File.ReadAllLines(filePath);
				if (lines.Length == 0)
				{
					Console.WriteLine("⚠️ File is empty: " + filePath);
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

				Console.WriteLine($"✅ Parsed {result.Count} UDF-mapped columns from file: {Path.GetFileName(filePath)}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error in BuildHierarchyByUdf: {ex.Message}");
				Logger.Log($"Error in BuildHierarchyByUdf: {ex}");
			}

			return result;
		}
	}
}
