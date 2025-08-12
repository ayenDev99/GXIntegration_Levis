using Dapper;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreGoodsReturnRepository
	{
		private readonly string _connectionString;
		public StoreGoodsReturnRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreGoodsReturnModel>> GetStoreGoodsReturnAsync(DateTime date, List<int> vouType, List<int> vouClass)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT
								S.STORE_CODE                    AS StoreCode
								, VOU.WORKSTATION               AS WorkstationNo
								, VOU.VOU_NO			        AS SequenceNo
								, TRUNC(VOU.CREATED_DATETIME)   AS BusinessDayDate
								, VOU.CREATED_DATETIME	        AS BeginDateTime
								, VOU.POST_DATE                 AS EndDateTime
								, VOU.CLERK_SID			        AS OperatorId
								, C.ALPHABETIC_CODE             AS CurrencyCode
								, REGION.REGION_NAME	        AS Region
								, COUNTRY.COUNTRY_CODE          AS Country
								, S.ADDRESS5			        AS AlternateStoreId
								, CASE WHEN VOU.STATUS = 4 
									THEN 'CLOSED' 
									ELSE 'PENDING' 
									END							AS DocumentStatus
								, VOU.VOU_NO					AS DocumentId
								, VOU.MODIFIED_DATETIME			AS CompletionTimestamp
								, VOU.MODIFIED_DATETIME			AS LastActivityTimestamp
								, ''							AS ShipmentSequence
								, ''							AS DestinationRetailLocationId
								, ''							AS ShipmentStatusCode
								, VI.CARTON_NO					AS CartonId
								, VI.CARTON_STATUS				AS CartonStatusCode
								, VI.ITEM_POS					AS LineNumber
								, ISB.DESCRIPTION1				AS ItemId
								, VI.QTY						AS ActualCount
								, ''							AS ExpectedCount
								, ''							AS PostedCount
								, VOU.CREATED_DATETIME			AS SaleLineBusinessDayDate
								, ''							AS TransactionSequence
								, ''							AS LineItemSequence
								, ''							AS RecordCreationType
								, ''							AS LineItemStatusCode
								, ''							AS PTDIM1
								, ''							AS PTDIM2
								, ''							AS PTStyle
								, ''							AS PTControlNumber
								, ''							AS PTEAN
								, VI.QTY						AS QuantityOrdered
								, ''							AS QuantityReceived
								, ISB.DESCRIPTION2				AS Description

								, VOU_REASON.NAME				AS ReasonCode
								, ''							AS OriginatorName
								, ''							AS ActualDeliveryDate
								, ''							AS ActualShipDate
								, S.ADDRESS5					AS DestinationPartyID
								, S.ZIP							AS ShipmentPostalCode
								, COUNTRY.COUNTRY_CODE			AS ShipmentCountry
								, VI.QTY 						AS QuantityShipped
	
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
								TRUNC(VOU.CREATED_DATETIME) BETWEEN DATE '2020-08-01' AND DATE '2025-08-07'
								AND VOU.VOU_TYPE IN :VoucherTypes
								AND VOU.VOU_CLASS IN :VoucherClass
							--  AND VOU.STATUS = 4
							FETCH FIRST 1 ROWS ONLY
					";

					var parameters = new
					{
						SaleDate = date.Date,
						VoucherTypes = vouType,
						VoucherClass = vouClass
					};

					var sales = await connection.QueryAsync<StoreGoodsReturnModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching ASN - Receiving data: {ex.Message}");
					Console.WriteLine($"Error fetching ASN - Receiving data: {ex.Message}");
					return new List<StoreGoodsReturnModel>();
				}
			}
		}

	}
}
