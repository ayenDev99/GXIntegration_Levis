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
			//var fromDateLiteral = GlobalHelper.ToOracleTimestampTZLiteral(from_date);
			//var toDateLiteral = GlobalHelper.ToOracleTimestampTZLiteral(to_date);
						//Logger.Log("From Date: " + fromDateLiteral);
			//Logger.Log("To Date: " + toDateLiteral);

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
                                ,'true'                                 AS InventoryMovementSuccess
                                ,'AMA'	                                AS Region
                                , COUNTRY.COUNTRY_CODE                  AS Country
                                , (SELECT ADDRESS4 FROM RPS.STORE 
                                    WHERE SID = VOU.STORE_SID)			AS AlternateStoreID
                                , CASE WHEN VOU.STATUS = 4 
									THEN 'CLOSED' 
									ELSE 'PENDING' 
									END							        AS DocumentStatus
                                , VOU.VOU_NO					        AS DocumentID
                                , 'RECEIVING_ASN'                       AS DocumentTypeDescription
                                , 'RECEIVING'                           AS DocumentType
                                , 'ASN'                                 AS DocumentSubType
                                , VOU.MODIFIED_DATETIME			        AS CompletionTimestamp
								, VOU.MODIFIED_DATETIME			        AS LastActivityTimestamp
								, '1'							        AS ShipmentSequence
                                , (SELECT ADDRESS4 FROM RPS.STORE 
                                    WHERE SID = VOU.SLIP_STORE_SID)		AS DestinationAlternateStoreID
                                , ''				                    AS ShipmentStatusCode
                                , '1'					                AS CartonID
                                , VI.CARTON_STATUS				        AS CartonStatusCode
                                , VI.ITEM_POS					        AS LineNumber
                                , ISB.DESCRIPTION1				        AS ItemID
                                , VI.QTY						        AS ActualCount
                                , ''							        AS ExpectedCount
                                , ''							        AS PostedCount
                                , VOU.CREATED_DATETIME			        AS SaleLineBusinessDayDate
                                , ''							        AS TransactionSequence
                                , VI.ITEM_POS							AS LineItemSequence
                                , 'OTHER'							    AS RecordCreationType
								, '1'							        AS LineItemStatusCode
                                , ISB.ITEM_SIZE							AS PTDIM1
                                , ISB.ATTRIBUTE							AS PTDIM2
                                , ''									AS PTStyle
                                , VOU.PO_NO								AS PTControlNumber
                                , ISB.UPC						AS PTEAN
                                , ''						            AS QuantityOrdered
                                , ''                                    AS QuantityReceived
                                , '1'                                   AS CartonNumber
                                , ISB.DESCRIPTION2				        AS Description
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
								VOU.POST_DATE BETWEEN :FromDate AND :ToDate
								AND VOU.PO_SID IS NOT NULL
								AND VOU.VOU_TYPE = 0
								AND VOU.VOU_CLASS = 0
								AND VOU.STATUS = 4
                                AND VOU.STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)
					";

					// TO CONFIRM STATUS

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
