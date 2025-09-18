using Dapper;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreShippingRepository
	{
		private readonly string _connectionString;
		public StoreShippingRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreShippingModel>> GetStoreShippingAsync(DateTime from_date, DateTime to_date, string storeCode)
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
                                , VOU.VOU_NO							AS SequenceNo
                                , TRUNC(VOU.CREATED_DATETIME)			AS BusinessDayDate
                                , VOU.CREATED_DATETIME					AS BeginDateTime
                                , VOU.POST_DATE							AS EndDateTime
                                , EMPLOYEE.EMPL_NAME					AS OperatorID
                                , C.ALPHABETIC_CODE						AS CurrencyCode
                                , 'true'								AS InventoryMovementSuccess
                                , 'AMA'									AS Region
                                , (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS AlternateStoreID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.SLIP_STORE_SID)		AS DestinationAlternateStoreID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS OriginAlternateStoreID
                                , CASE WHEN VOU.STATUS = 4 
									THEN 'CLOSED' 
									ELSE 'PENDING' 
									END									AS DocumentStatus
								, VOU.VOU_NO							AS DocumentID
								, ''									AS OriginatorID
								, ''									AS OriginatorName
								, 'SHIPPING_STORE_TRANSFER'				AS DocumentTypeDescription
								, 'SHIPPING'							AS DocumentType
								, 'STORE_TRANSFER'						AS DocumentSubType
								, 'STORE'								AS RecordCreationType
								, VOU.CREATED_DATETIME					AS CreationTimestamp
								, VOU.MODIFIED_DATETIME					AS CompletionTimestamp
								, VOU.MODIFIED_DATETIME					AS LastActivityTimestamp
								, '1'									AS ShipmentSequence
                                , VOU.CREATED_DATETIME					AS ActualDeliveryDate                                
                                , VOU.CREATED_DATETIME					AS ActualShipDate                                
								, (SELECT UDF4_STRING FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS DestinationRetailLocationID
								, ''									AS ShippingCarrier
                                , VOU.TRACKING_NO						AS TrackingNumber
								, 'SHIPPED'								AS StatusCode
                                , ''								    AS PostalCode
                                , COUNTRY.COUNTRY_CODE                  AS Country
                                , ISB.DESCRIPTION1						AS ItemID
                                , ISB.ITEM_SIZE							AS PTDIM1
								, ISB.ATTRIBUTE							AS PTDIM2
								, ''									AS PTStyle
								, VOU.PO_NO								AS PTControlNumber
								, ISB.DESCRIPTION1						AS PTEAN
                                , VI.QTY 						        AS QuantityShipped
								, VI.ITEM_POS							AS LineNumber
								, ISB.DESCRIPTION2						AS Description
							FROM
								RPS.VOUCHER VOU
							LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
							LEFT JOIN RPS.STORE	S					ON S.SID = VOU.STORE_SID
							LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
							LEFT JOIN RPS.COUNTRY					ON COUNTRY.SID = SBS.COUNTRY_SID
							LEFT JOIN RPS.REGION_SUBSIDIARY			ON SBS.SID = REGION_SUBSIDIARY.SBS_SID
							LEFT JOIN RPS.REGION					ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
							LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VI.ITEM_SID
							INNER JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND VOU.CLERK_SID = EMPLOYEE.SID
							LEFT JOIN RPS.CURRENCY C				ON SBS.BASE_CURRENCY_SID = C.SID
							LEFT JOIN RPS.PREF_REASON VOU_REASON	ON VOU.VOU_REASON_SID = VOU_REASON.SID
							WHERE
								TRUNC(VOU.POST_DATE) BETWEEN DATE '2021-01-20' AND DATE '2025-09-20'
								
								AND VOU.VOU_CLASS = 2
								AND VOU.SLIP_FLAG = 1
								AND VOU.STATUS = 3
                                AND VOU.STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND D.POST_DATE BETWEEN :FromDate AND :ToDate

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreShippingModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store Shipping data: {ex.Message}");
					Console.WriteLine($"Error fetching Store Shipping data: {ex.Message}");
					return new List<StoreShippingModel>();
				}
			}
		}

	}
}
