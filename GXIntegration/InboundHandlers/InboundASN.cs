using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Org.BouncyCastle.Math.EC.ECCurve;
using JsonFormatting = Newtonsoft.Json.Formatting;


namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundASN
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunASNSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			try
			{			
				Logger.Log("[INBOUND - ASN] Starting ASN Sync Process...");

				string fileNameFormat = "LSPI_PRTRDX_*.*";

				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) { Logger.Log("[INBOUND - ASN] No ASN files found.");	}

				foreach (string file in files)
				{
					var result = BuildASNCollection(file);
					Logger.Log($"[INBOUND - ASN] ASN file loaded. Rows found: {result.Count}");

					var groupedByDocument = result
								.Where(r => r.ContainsKey("DocumentNumber"))
								.GroupBy(r => r["DocumentNumber"]);

					foreach (var group in groupedByDocument)
					{
						var documentNumber = group.Key;

						// Check if PO_NO already exist on DB.
						var isPONumExist = await IsPONumExistAsync(repository, documentNumber);

						if(isPONumExist)
						{
							Logger.Log($"[INBOUND - ASN]		PO already exists.");

							continue;
						} 
						else
						{
							var productCodes = group
											.Where(row => row.ContainsKey("ProductCode")
													   && row.ContainsKey("ColorCode")
													   && row.ContainsKey("SizeCode"))
											.Select(row => new ProductCodeInfo
											{
												ProductCode = row["ProductCode"],
												ColorCode = row["ColorCode"],
												SizeCode = row["SizeCode"]
											})
											.Where(item => !string.IsNullOrWhiteSpace(item.ProductCode)
														&& !string.IsNullOrWhiteSpace(item.ColorCode)
														&& !string.IsNullOrWhiteSpace(item.SizeCode))
											.Distinct()
											.ToList();

							//Logger.Log($"[INBOUND - ASN] SKU Count: {productCodes.Count}\n" +
							//		   string.Join("\n", productCodes.Select(productCode =>
							//			   $"  ProductCode: {productCode.ProductCode} | ColorCode: {productCode.ColorCode} | SizeCode: {productCode.SizeCode}")));

							XDocument config = XDocument.Load("config.xml");
							bool acceptPartial = bool.Parse(config.Descendants("AcceptPartial").First().Value);

							Logger.Log($"[INBOUND - ASN]		AcceptPartial : {acceptPartial} ");

							// Check if PO ProductCode exist in DB
							bool isPOItemsExist = await IsPOItemsExistAsync(repository, documentNumber, productCodes, acceptPartial);

							if (isPOItemsExist)
							{
								// Create PO
								foreach (var row in group)
								{
									Logger.Log($"[INBOUND - ASN]		ROW DATA: {string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"))}");
									await createRpsPOAsync(repository, session, row);
								}
							}

						}

						continue;
					}

				}

			}
			catch (Exception ex)
			{
				Logger.Log($"Error in RunASNSyncAsync: {ex.Message}");
				return;
			}
		}

		private async Task<bool> IsPONumExistAsync(dynamic repository, string documentNumber)
		{
			if (string.IsNullOrWhiteSpace(documentNumber))
			{
				Logger.Log("[INBOUND - ASN]		Document number is null or empty.");
				return false;
			}

			var poResult = await repository.GetRpsPO("PO_NO", documentNumber);
			var resultList = poResult as List<dynamic> ?? new List<dynamic>();

			int count = resultList?.Count ?? 0;
			Logger.Log($"[INBOUND - ASN]	PO_NO : '{documentNumber}'");

			return count > 0;
		}
		
		private async Task<bool> IsPOItemsExistAsync(dynamic repository, string documentNumber, List<ProductCodeInfo> productCodes, bool isAcceptPartial)
		{
			if (string.IsNullOrWhiteSpace(documentNumber))
			{
				Logger.Log("[INBOUND - ASN]		Document number is null or empty.");
				return false;
			}

			bool anyItemExists = false;

			foreach (var productCode in productCodes)
			{
				var filters = new Dictionary<string, object>
				{
					{ "DESCRIPTION1", productCode.ProductCode },
					{ "ATTRIBUTE", productCode.ColorCode },
					{ "ITEM_SIZE",  productCode.SizeCode }
				};

				var results = await repository.GetInboundItemsAsync(filters);
				var resultList = results as List<dynamic> ?? new List<dynamic>();

				if (resultList.Count > 0)
				{
					Logger.Log($"[INBOUND - ASN]			ProductCode: {productCode.ProductCode} | ColorCode: {productCode.ColorCode} | SizeCode: {productCode.SizeCode} IS EXIST on Prism DB");
					anyItemExists = true;
				}
				else
				{
					Logger.Log($"[INBOUND - ASN]			Missing items detected. ProductCode: {productCode.ProductCode} IS EXIST on Prism DB");

					if (!isAcceptPartial)
					{
						// If partial not accepted, missing even 1 productCode → no insert
						Logger.Log($"[INBOUND - ASN]		Missing items detected. Skipping PO insertion. ProductCode : {productCode} does NOT EXIST on Prism DB");

						return false;
					}

					continue;
				}
			}

			// If partial accepted, insert only if at least one product exists.
			// If partial not accepted, at this point all productCodes exist.
			return isAcceptPartial ? anyItemExists : true;
		}

		// ***************************************************
		// API PROCESS METHODS
		// ***************************************************
		private async Task<string> createRpsPOAsync(dynamic repo, string session, IDictionary<string, string> item)
		{
			Logger.Log($"[INBOUND - PRICE]		[CREATE] PO");

			//Logger.Log("[INBOUND - PRICE] [CREATE] PO - Item Details:");
			//item?.ToList().ForEach(kv => Logger.Log($"   {kv.Key} = {kv.Value}"));

			var storeCode = item?.TryGetValue("StoreCode", out var storeCodeValue) == true ? storeCodeValue : null;
			var poNo = item?.TryGetValue("DocumentNumber", out var poNoValue) == true ? poNoValue : null;

			var prismStore = await repo.GetRpsStore("ADDRESS5", storeCode);

			if (prismStore == null || prismStore.Count == 0) { Logger.Log($"[INBOUND - ASN]		StoreCode : {storeCode} is not existing on Prism DB.");	}

			int billtostoreno = Convert.ToInt32(prismStore[0].STORE_NO);

			var sbs_sid = prismStore[0].SBS_SID.ToString();
			var shippingdate = item?.TryGetValue("ShipmentDate", out var rawDate) == true
								? GlobalHelper.FormatDateToIso8601(rawDate)
								: null;

			var PoPayload = new Dictionary<string, object>
			{
				["billtostoreno"] = billtostoreno,
				["originapplication"] = "RProPrismWeb",
				["sbssid"] = sbs_sid,
				["shippingdate"] = shippingdate,
				["status"] = 1,
				["potype"] = 0,
				["pono"] = poNo,
			};

			//string payloadJson = System.Text.Json.JsonSerializer.Serialize(PoPayload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
			//Logger.Log(payloadJson);

			// Call API to CREATE RPS.PO
			string endpointCreate = "/api/backoffice/purchaseorder";
			var payload = new { data = new[] { PoPayload } };
			string json = JsonConvert.SerializeObject(payload, JsonFormatting.Indented);
			string responseJson = GlobalInbound.CallPrismAPI(
									session
									, endpointCreate
									, json
									, out bool issuccessful
									, "POST"
									, 1
									);

			return responseJson;

			
		}

		// ***************************************************
		// MISC METHODS
		// ***************************************************
		private List<Dictionary<string, string>> BuildASNCollection(string filePath)
		{
			var result = new List<Dictionary<string, string>>();

			try
			{
				using (var parser = new TextFieldParser(filePath))
				{
					parser.TextFieldType = FieldType.Delimited;
					parser.SetDelimiters("{^^}");
					parser.HasFieldsEnclosedInQuotes = true;

					while (!parser.EndOfData)
					{
						string[] fields = parser.ReadFields();

						if (fields == null || fields.Length == 0 || fields.All(f => string.IsNullOrWhiteSpace(f)))
							continue;

						// Trim all fields
						fields = fields.Select(f => f.Trim()).ToArray();

						var rowDict = new Dictionary<string, string>();

						if (fields.Length > 0) rowDict["RecordType"] = fields[0];
						if (fields.Length > 1) rowDict["DocumentType"] = fields[1];
						if (fields.Length > 2) rowDict["DocumentLineType"] = fields[2];
						if (fields.Length > 3) rowDict["AutoReceiveFlag"] = fields[3];
						if (fields.Length > 4) rowDict["StoreCode"] = fields[4];
						if (fields.Length > 5) rowDict["DeliveryLocation"] = fields[5];
						if (fields.Length > 6) rowDict["ProductCode"] = fields[6];
						if (fields.Length > 7) rowDict["ProductReference"] = fields[7];
						if (fields.Length > 8) rowDict["ColorCode"] = fields[8];
						if (fields.Length > 9) rowDict["SizeCode"] = fields[9];
						if (fields.Length > 10) rowDict["Sku"] = fields[10];
						if (fields.Length > 11) rowDict["Quantity"] = fields[11];
						if (fields.Length > 12) rowDict["StoreOrderNumber"] = fields[12];
						if (fields.Length > 13) rowDict["VendorOrderNumber"] = fields[13];
						if (fields.Length > 14) rowDict["DocumentNumber"] = fields[14];		// PO Number
						if (fields.Length > 15) rowDict["VendorShipmentNumber"] = fields[15];
						if (fields.Length > 16) rowDict["Currency"] = fields[16];
						if (fields.Length > 17) rowDict["VendorCode"] = fields[17];
						if (fields.Length > 18) rowDict["PurchasePrice"] = fields[18];
						if (fields.Length > 19) rowDict["Discount"] = fields[19];
						if (fields.Length > 20) rowDict["LandedCost"] = fields[20];
						if (fields.Length > 21) rowDict["TaxCost"] = fields[21];
						if (fields.Length > 22) rowDict["AverageCost"] = fields[22];
						if (fields.Length > 23) rowDict["ShipmentDate"] = fields[23];
						if (fields.Length > 24) rowDict["DeliveryDate"] = fields[24];
						if (fields.Length > 25) rowDict["OrderDate"] = fields[25];
						if (fields.Length > 26) rowDict["RequestedDeliveryDate"] = fields[26];
						if (fields.Length > 27) rowDict["CancellationDate"] = fields[27];
						if (fields.Length > 28) rowDict["Brand"] = fields[28];
						if (fields.Length > 29) rowDict["SerialNumber"] = fields[29];
						if (fields.Length > 30) rowDict["ProductLine"] = fields[30];
						if (fields.Length > 31) rowDict["CustomerOrder"] = fields[31];
						if (fields.Length > 32) rowDict["AssignToBin"] = fields[32];
						if (fields.Length > 33) rowDict["BinCategory"] = fields[33];
						if (fields.Length > 34) rowDict["QtyDecimal"] = fields[34];
						if (fields.Length > 35) rowDict["UCC128"] = fields[35];
						if (fields.Length > 36) rowDict["ExtendedExternalSku"] = fields[36];
						if (fields.Length > 37) rowDict["QtySign"] = fields[37];

						result.Add(rowDict);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"Error in BuildPriceCollection for file '{filePath}': {ex}");
			}

			return result;
		}

		public class ProductCodeInfo
		{
			public string ProductCode { get; set; }
			public string ColorCode { get; set; }
			public string SizeCode { get; set; }
		}

	}
}
