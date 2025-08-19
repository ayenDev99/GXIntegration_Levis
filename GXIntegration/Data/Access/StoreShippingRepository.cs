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
		public async Task<List<StoreShippingModel>> GetStoreShippingAsync(DateTime from_date, DateTime to_date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT
								VOU.SID							AS VouSid
								, TO_CHAR(S.ADDRESS4)			AS StoreCode
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
								, ''							AS DestinationAlternateStoreId
								, ''							AS OriginAlternateStoreId
								, CASE WHEN VOU.STATUS = 4 
									THEN 'CLOSED' 
									ELSE 'PENDING' 
									END							AS DocumentStatus
								, VOU.VOU_NO					AS DocumentId
								, VOU.MODIFIED_DATETIME			AS CreationTimestamp
								, VOU.MODIFIED_DATETIME			AS CompletionTimestamp
								, VOU.MODIFIED_DATETIME			AS LastActivityTimestamp
								, ''							AS ShipmentSequence
								, ''							AS ActualShipDate
								, ''							AS DestinationRetailLocationId
								, ''							AS ShippingCarrier
								, ''							AS TrackingNumber
								, ''							AS ShipmentStatusCode
								, ''							AS PostalCode
								, ISB.DESCRIPTION1				AS ItemId
								, ''							AS PTDIM1
								, ''							AS PTDIM2
								, ''							AS PTStyle
								, ''							AS PTControlNumber
								, ''							AS PTEAN
								, VI.QTY 						AS QuantityShipped
								, VI.ITEM_POS					AS LineNumber
								, ISB.DESCRIPTION2				AS Description
	
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
								TRUNC(VOU.POST_DATE) BETWEEN DATE '2021-01-01' AND DATE '2024-02-17'
								AND VOU.VOU_CLASS = 2
								AND VOU.SLIP_FLAG = 1
							--  AND VOU.STATUS = 4
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND D.POST_DATE BETWEEN :FromDate AND :ToDate

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date
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
