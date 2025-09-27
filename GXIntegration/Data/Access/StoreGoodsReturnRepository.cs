using Dapper;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreGoodsReturnRepository
	{
		private readonly string _connectionString;
		public StoreGoodsReturnRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreGoodsReturnModel>> GetStoreGoodsReturnAsync(DateTime from_date, DateTime to_date, string storeCode)
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
									WHERE SID = VOU.STORE_SID)			AS RetailStoreID
								, VOU.WORKSTATION						AS WorkstationID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID) 
									|| VOU.WORKSTATION					AS TillID
								, VOU.VOU_NO			                AS SequenceNo
								, TRUNC(VOU.CREATED_DATETIME)           AS BusinessDayDate
								, VOU.CREATED_DATETIME	                AS BeginDateTime
								, VOU.POST_DATE                         AS EndDateTime
								, EMPLOYEE.EMPL_NAME			        AS OperatorID
								, C.ALPHABETIC_CODE                     AS CurrencyCode
								, 'true'                                AS InventoryMovementSuccess
								, 'AMA'	                                AS Region
								, 'PHP'									AS Country
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS AlternateStoreID
								, VOU_REASON.NAME				        AS ReasonCode
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS OriginAlternateStoreID
								, CASE WHEN VOU.STATUS = 4 
									THEN 'CLOSED' 
									ELSE 'PENDING' 
									END							        AS DocumentStatus
								, VOU.VOU_NO					        AS DocumentID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS OriginatorName
								, 'SHIPPING_RTV_FROM_DAMAGED'           AS DocumentTypeDescription
								, 'SHIPPING'                            AS DocumentType
								, 'RTV_to_DC '                          AS DocumentSubType
								, VOU.MODIFIED_DATETIME			        AS CreationTimestamp
								, VOU.POST_DATE			                AS CompletionTimestamp
								, '1'							        AS ShipmentSequence
								, VOU.POST_DATE			                AS ActualDeliveryDate
								, VOU.POST_DATE			                AS ActualShipDate
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS DestinationPartyID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS DestinationRetailLocationID
								, 'SHIPPED'				                AS ShipmentStatusCode
								, ''				                    AS City
								, (SELECT ZIP FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS PostalCode
								, ISB.DESCRIPTION1				        AS ItemID
								, ISB.UPC				                AS ScannedBarcodeID
								, VI.QTY						        AS QuantityShipped
								, VI.ITEM_POS					        AS LineNumber
								, ISB.DESCRIPTION2				        AS Description
								, ISB.ITEM_SIZE							AS PTDIM1
								, ISB.ATTRIBUTE							AS PTDIM2
								, ISB.DESCRIPTION1						AS PTStyle
								, VOU.PO_NO								AS PTControlNumber
								, ISB.UPC								AS PTEAN
    
								, VOU.SID							    AS VouSid							
							FROM
								RPS.VOUCHER VOU
							LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
							LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
							LEFT JOIN RPS.COUNTRY					ON COUNTRY.SID = SBS.COUNTRY_SID
							LEFT JOIN RPS.REGION_SUBSIDIARY			ON SBS.SID = REGION_SUBSIDIARY.SBS_SID
							LEFT JOIN RPS.REGION					ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
							LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VI.ITEM_SID
							INNER JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND VOU.CLERK_SID = EMPLOYEE.SID
							LEFT JOIN RPS.CURRENCY C				ON SBS.BASE_CURRENCY_SID = C.SID
							LEFT JOIN RPS.PREF_REASON VOU_REASON	ON VOU.VOU_REASON_SID = VOU_REASON.SID
							WHERE
								TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate
								AND VOU.VOU_TYPE = 1
								AND VOU.VOU_CLASS = 0
								AND VOU.STATUS = 4
							AND VOU.STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)					
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND VOU.POST_DATE BETWEEN :FromDate AND :ToDate
					// TRUNC(VOU.POST_DATE) BETWEEN DATE '2020-08-20' AND DATE '2025-08-20'

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreGoodsReturnModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store_Goods_Return data: {ex.Message}");
					Console.WriteLine($"Error fetching Store_Goods_Return data: {ex.Message}");
					return new List<StoreGoodsReturnModel>();
				}
			}
		}

	}
}
