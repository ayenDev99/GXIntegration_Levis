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
		public async Task<List<StoreGoodsModel>> GetStoreGoodsAsync(DateTime from_date, DateTime to_date, string storeCode)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
						SELECT
							'1'										AS OrganizationID
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.BILLTO_STORE_SID)	AS RetailStoreID
							, VOU.WORKSTATION						AS WorkstationID
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.BILLTO_STORE_SID) 
									|| VOU.WORKSTATION				AS TILLID
							, PO.PO_NO								AS SequenceNo
							, TRUNC(VOU.CREATED_DATETIME)			AS BusinessDayDate
							, VOU.CREATED_DATETIME					AS BeginDateTime
							, VOU.POST_DATE							AS EndDateTime
							, EMPLOYEE.EMPL_NAME			        AS OPERATORID
							, 'PHP'									AS CurrencyCode
							,'true'                                 AS InventoryMovementSuccess
							,'AMA'	                                AS Region
							, 'PHP'									AS Country
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.BILLTO_STORE_SID)	AS AlternateStoreID
							, CASE WHEN VOU.STATUS = 4 
								THEN 'CLOSED' 
								ELSE 'PENDING' 
								END									AS DocumentStatus
							, PO.PO_NO								AS DocumentID
							, 'RECEIVING_ASN'                       AS DocumentTypeDescription
							, 'RECEIVING'                           AS DocumentType
							, 'ASN'                                 AS DocumentSubType
							, VOU.MODIFIED_DATETIME			        AS CompletionTimestamp
							, VOU.POST_DATE							AS LastActivityTimestamp
							, '1'							        AS ShipmentSequence
							, (SELECT ADDRESS4 FROM RPS.STORE 
								WHERE SID = PO.SHIPTO_STORE_SID)	AS DestinationAlternateStoreID
							, '1'				                    AS ShipmentStatusCode
							, '1'					                AS CartonID
							, '1'									AS CartonStatusCode
							, VI.ITEM_POS					        AS LineNumber
							, ISB.DESCRIPTION1				        AS ItemID
							, PO.RCVD_QTY						    AS ActualCount
							, PO.ORD_QTY							AS ExpectedCount
							, PO.RCVD_QTY							AS POSTEDCOUNT
							, VOU.CREATED_DATETIME			        AS SaleLineBusinessDayDate
							, VOU.VOU_NO							AS TransactionSequence
							, VI.ITEM_POS							AS LineItemSequence
							, 'OTHER'							    AS RecordCreationType
							, '1'									AS LineItemStatusCode
							, ISB.ITEM_SIZE							AS PTDIM1
							, ISB.ATTRIBUTE							AS PTDIM2
							, ISB.DESCRIPTION1						AS PTStyle
							, PO.PO_NO								AS PTControlNumber
							, ISB.UPC								AS PTEAN
							, PO.ORD_QTY						    AS QUANTITYORDERED
							, PO.RCVD_QTY                           AS QuantityReceived
							, '1'                                   AS CartonNumber
							, ISB.DESCRIPTION2				        AS Description
							, VOU.SID							    AS VouSid							
						FROM
										RPS.VOUCHER VOU
						LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
						LEFT JOIN RPS.PO 						ON PO.PO_NO = VOU.PO_NO
						LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
						LEFT JOIN RPS.COUNTRY					ON COUNTRY.SID = SBS.COUNTRY_SID
						LEFT JOIN RPS.REGION_SUBSIDIARY			ON SBS.SID = REGION_SUBSIDIARY.SBS_SID
						LEFT JOIN RPS.REGION					ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VI.ITEM_SID
						INNER JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND PO.CLERK_SID = EMPLOYEE.SID
						LEFT JOIN RPS.CURRENCY C				ON SBS.BASE_CURRENCY_SID = C.SID
						LEFT JOIN RPS.PREF_REASON VOU_REASON	ON VOU.VOU_REASON_SID = VOU_REASON.SID
						WHERE
							TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate
							AND VOU.PO_NO IS NOT NULL
							AND VOU.VOU_TYPE = 0
							AND VOU.VOU_CLASS = 0
							AND VOU.STATUS = 4
							AND PO.BILLTO_STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)
					";

					//FETCH FIRST 1 ROWS ONLY
					// TRUNC(VOU.POST_DATE) BETWEEN DATE '2025-08-20' AND DATE '2025-08-20'

					//Logger.Log(sql);

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreGoodsModel>(sql, parameters);
					return sales.ToList();
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
