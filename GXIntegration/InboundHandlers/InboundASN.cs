using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace GXIntegration_Levis.InboundHandlers
{
	public class InboundASN
	{
		private readonly GlobalInbound globalInbound = new GlobalInbound();

		public async Task RunASNSyncAsync(string session, string inboundDir, PrismRepository repository)
		{
			try
			{
				Logger.Log($"--------------------------------------------------------------------------");
				Logger.Log("[INBOUND - ASN] STARTING ASN Sync Process...");

				string fileNameFormat = "LSPI_PRTRDX_*.*";
				var files = globalInbound.GetInboundFiles(inboundDir, fileNameFormat);
				if (files.Count == 0) { Logger.Log($"[INBOUND - ASN] No {fileNameFormat} file format found."); }

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
						if (isPONumExist)
						{
							Logger.Log($"[INBOUND - ASN]		PO already exists.");

							continue;
						} 
						else
						{
							var productCodes = group
											.Where(row => row.ContainsKey("ProductCode")
													   && row.ContainsKey("ColorCode")
													   && row.ContainsKey("SizeCode")
													   && row.ContainsKey("StoreCode"))
											.Select(row => new ProductCodeInfo
											{
												ProductCode = row["ProductCode"],
												ColorCode = row["ColorCode"],
												SizeCode = row["SizeCode"],
												StoreCode = row["StoreCode"]
											})
											.Where(item => !string.IsNullOrWhiteSpace(item.ProductCode)
														&& !string.IsNullOrWhiteSpace(item.ColorCode)
														&& !string.IsNullOrWhiteSpace(item.StoreCode)
														&& !string.IsNullOrWhiteSpace(item.ColorCode))
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

				Logger.Log("[INBOUND - ASN] END Sync Process");
			}
			catch (Exception ex)
			{
				Logger.Log($"[INBOUND - ASN] Error in RunASNSyncAsync: {ex.Message}");
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
				var storeCode = productCode.StoreCode;
				var prismStore = await repository.GetRpsStore("ADDRESS4", storeCode);
				var sbs_sid = prismStore?.Count > 0 ? prismStore[0].SBS_SID.ToString() : null;

				if (prismStore == null || prismStore.Count == 0)
				{
					Logger.Log($"[INBOUND - ASN]		StoreCode : {storeCode} is not existing on Prism DB.");
					return false;
				}

				var ALU = productCode.ProductCode + productCode.SizeCode + productCode.ColorCode;
				var filters = new Dictionary<string, object>
				{
					{ "DESCRIPTION1", productCode.ProductCode },
					{ "ATTRIBUTE", productCode.ColorCode },
					{ "ITEM_SIZE",  productCode.SizeCode },
					{ "SBS_SID",  sbs_sid }
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
					//Logger.Log($"[INBOUND - ASN]			Missing items detected. ProductCode: {productCode.ProductCode} IS EXIST on Prism DB");

					if (isAcceptPartial)
					{
						// If partial not accepted, missing even 1 productCode → no insert
						Logger.Log($"[INBOUND - ASN]		Skipping PO insertion. Missing items detected. ALU : {ALU} and SBS : {sbs_sid} does NOT EXIST on Prism DB");

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
			//Logger.Log("[INBOUND - ASN] [CREATE] PO - Item Details:");
			//item?.ToList().ForEach(kv => Logger.Log($"   {kv.Key} = {kv.Value}"));

			var storeCode		= GlobalHelper.GetStringValue(item, "StoreCode");
			var prismStore		= await repo.GetRpsStore("ADDRESS4", storeCode);

			if (prismStore == null || prismStore.Count == 0) 
			{ 
				Logger.Log($"[INBOUND - ASN]		StoreCode : {storeCode} is not existing on Prism DB.");
				return null;
			}

			int? billtostoreno		= prismStore?.Count > 0 ? Convert.ToInt32(prismStore[0].STORE_NO) : (int?) null;
			int? orderQty			= GlobalHelper.GetIntValue(item, "Quantity");
			var poNo				= GlobalHelper.GetStringValue(item, "DocumentNumber");
			var sbs_sid				= prismStore?.Count > 0 ? prismStore[0].SBS_SID.ToString() : null;
			var instruction1		= GlobalHelper.GetStringValue(item, "StoreOrderNumber");
			string shippingDate		= GlobalHelper.FormatDateToIso8601(item?["ShipmentDate"]);
			string orderDate		= GlobalHelper.FormatDateToIso8601(item?["OrderDate"]);
			decimal? purchasePrice	= GlobalHelper.GetDecimalValue(item, "PurchasePrice", 4);
			decimal? landedCost		= GlobalHelper.GetDecimalValue(item, "LandedCost", 4);
			decimal? taxCost		= GlobalHelper.GetDecimalValue(item, "TaxtCost", 4);
			var productCode			= GlobalHelper.GetStringValue(item, "ProductCode");
			var sizeCode			= GlobalHelper.GetStringValue(item, "SizeCode");
			var colorCode			= GlobalHelper.GetStringValue(item, "ColorCode");
			var itemAlu				= productCode + sizeCode + colorCode;

			// CREATE RPS.PO
			var poPayload = new Dictionary<string, object>
			{
				["billtostoreno"]		= billtostoreno ?? 0
				, ["createddatetime"]	= orderDate ?? ""
				, ["originapplication"] = "RProPrismWeb"
				, ["sbssid"]			= sbs_sid ?? string.Empty
				, ["shippingDate"]		= shippingDate ?? ""
				, ["status"]			= 1
				, ["potype"]			= 0
				, ["pono"]				= poNo ?? string.Empty
				, ["ordqty"]			= orderQty ?? 0
				, ["instruction1"]		= instruction1 ?? string.Empty
			};

			// CREATE RPS.PO_ITEM
			var prismInvnSbsItem = await repo.GetRpsInvnSbsItem("ALU", itemAlu);
			var activeItems = new List<dynamic>();
			foreach (var x in prismInvnSbsItem)
				if (((IDictionary<string, object>)x)["SBS_SID"]?.ToString() == sbs_sid)
					activeItems.Add(x);

			var itemSid = ((IDictionary<string, object>)activeItems.First())["SID"]?.ToString();
			if (!string.IsNullOrWhiteSpace(itemSid))
			{
				poPayload["poitem"] = new[]
				{
				new {
					itemsid		= itemSid
					, price     = purchasePrice ?? 0
					, cost      = landedCost ?? 0
					, taxamount = taxCost ?? 0
					, ordqty    = orderQty ?? 0
				}
			};
			}

			var payload = new { data = new[] { poPayload } };
			string json = JsonConvert.SerializeObject(payload, Formatting.Indented);

			Logger.Log($"[INBOUND - ASN]		[CREATE] PO");
			// Call API to CREATE RPS.PO
			string endpointCreate = "/api/backoffice/purchaseorder";
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
			public string StoreCode { get; set; }
		}

	}
}
