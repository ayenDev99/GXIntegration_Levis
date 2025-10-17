using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreGoodsRepository
	{
		private readonly string _connectionString;
		public StoreGoodsRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreGoodsModel>> GetStoreGoodsAsync(DateTime fromDate, DateTime toDate, string storeCode, string processType)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string dateCondition;
					if (processType == "EOD")
					{
						dateCondition = "TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate";
					}
					else if (processType == "API")
					{
						dateCondition = "VOU.CREATED_DATETIME BETWEEN :FromDate AND :ToDate";
					}
					else
					{
						dateCondition = "TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate";
					}

					string sql = @"
						SELECT
							VOU.SID									AS VouSid							
							, '1'									AS TransOrganizationID
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.SHIPTO_STORE_SID)	AS TransRetailStoreID
							, VOU.WORKSTATION						AS TransWorkstationID
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.SHIPTO_STORE_SID) 
									|| VOU.WORKSTATION				AS TransTILLID
							, VOU.VOU_NO							AS TransSequenceNo
							, TRUNC(VOU.CREATED_DATETIME)			AS TransBusinessDayDate
							, VOU.CREATED_DATETIME					AS TransBeginDateTime
							, VOU.POST_DATE							AS TransEndDateTime
							, EMPLOYEE.EMPL_NAME			        AS TransOperatorID
							, 'PHP'									AS TransCurrencyCode
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.SHIPTO_STORE_SID)	AS AlternateStoreID

							, PO.PO_NO								AS DocumentID
							, VOU.MODIFIED_DATETIME			        AS CompletionTimestamp
							, VOU.POST_DATE							AS LastActivityTimestamp

							, '1'							        AS ShipmentSequence
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.SHIPTO_STORE_SID)	AS DestinationRetailLocationID
							, '1'				                    AS ShipmentStatusCode

							, '1'					                AS CartonID
							, '1'									AS CartonStatusCode

							, TO_NUMBER(VI.ITEM_POS) * 10			AS LineNumber
							, REPLACE(ISI.ALU, '-', '')				AS ItemID
							, PO_ITEM.RCVD_QTY						AS ActualCount
							, PO_ITEM.ORD_QTY						AS ExpectedCount
							, PO_ITEM.RCVD_QTY						AS POSTEDCOUNT
							, VOU.CREATED_DATETIME			        AS SaleLineBusinessDayDate
							, VOU.VOU_NO							AS TransactionSequence
							, VI.ITEM_POS							AS LineItemSequence
							, 'OTHER'							    AS RecordCreationType
							, '1'									AS LineItemStatusCode

							, ISI.ALU								AS ALU
							, TO_NUMBER(VI.ITEM_POS) * 10			AS ItemLineNumber
							, ISI.ITEM_SIZE							AS PTDIM1
							, ISI.ATTRIBUTE							AS PTDIM2
							, ISI.DESCRIPTION1						AS PTStyle
							, VOU_COMMENT.COMMENTS					AS PTControlNumber
							, ISI.UPC								AS PTEAN
							, PO_ITEM.ORD_QTY						AS QuantityOrdered
							, PO_ITEM.RCVD_QTY                      AS QuantityReceived
							, '1'                                   AS CartonNumber
							, ISI.DESCRIPTION2				        AS Description
						FROM
							RPS.VOUCHER VOU
						LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
						LEFT JOIN RPS.PO 						ON PO.PO_NO = VOU.PO_NO
						LEFT JOIN RPS.PO_ITEM 					ON PO_ITEM.PO_SID = PO.SID
						LEFT JOIN RPS.VOU_COMMENT 				ON VOU_COMMENT.VOU_SID = VOU.SID
						LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISI			ON ISI.SID = VI.ITEM_SID
						LEFT JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND PO.CLERK_SID = EMPLOYEE.SID
						WHERE
							{DATE_CONDITION}
							AND VOU.PO_NO IS NOT NULL
							AND VOU.VOU_TYPE = 0
							AND VOU.VOU_CLASS = 0
							AND VOU.STATUS = 4
							AND PO.SHIPTO_STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)
					";

					sql = sql.Replace("{DATE_CONDITION}", dateCondition);

					//Logger.Log($"Generated SQL: {sql}");

					var parameters = new
					{
						FromDate = fromDate,
						ToDate = toDate,
						StoreCode = storeCode
					};

					//var sales = await connection.QueryAsync<StoreGoodsModel>(sql, parameters);
					//return sales.ToList();
					var salesDictionary = new Dictionary<string, StoreGoodsModel>();

					var sales = await connection.QueryAsync<StoreGoodsModel, SGCarton, SGItems, StoreGoodsModel>(
						sql,
						(sale, carton, item) =>
						{
							// Group by transaction (document)
							if (!salesDictionary.TryGetValue(sale.TransSequenceNo, out var existingSale))
							{
								existingSale = sale;
								existingSale.SGCarton = new List<SGCarton>();
								existingSale.SGItems = new List<SGItems>();
								salesDictionary[sale.TransSequenceNo] = existingSale;
							}

							// --- Handle Carton ---
							SGCarton existinCarton = null;
							if (!string.IsNullOrEmpty(carton?.LineNumber))
							{
								existinCarton = existingSale.SGCarton
									.FirstOrDefault(i => i.LineNumber == carton.LineNumber);

								if (existinCarton == null)
								{
									existinCarton = carton;
									existingSale.SGCarton.Add(existinCarton);
								}
							}
							// --- Handle Item Description of Carton ---
							SGItems existingItem = null;
							if (!string.IsNullOrEmpty(item?.ALU))
							{
								existingItem = existingSale.SGItems
									.FirstOrDefault(i => i.ALU == item.ALU);

								if (existingItem == null)
								{
									existingItem = item;
									existingSale.SGItems.Add(existingItem);
								}
							}

							return existingSale;
						}
						, parameters
						, splitOn: "LineNumber,ALU"
					).ConfigureAwait(false);

					return salesDictionary.Values.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store_Goods data: {ex.Message}");
					Console.WriteLine($"Error fetching Store_Goods data: {ex.Message}");
					return new List<StoreGoodsModel>();
				}
			}
		}

	}
}
