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
	public class StoreInventoryAdjustmentRepository
	{
		private readonly string _connectionString;
		public StoreInventoryAdjustmentRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreInventoryAdjustmentModel>> GetStoreInventoryAdjustmentAsync(DateTime from_date, DateTime to_date, string storeCode)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT
                                '1'                                 AS OrganizationID
                                , S.ADDRESS4			            AS RetailStoreID
                                , WS.WORKSTATION_NO				    AS WorkstationID
                                , S.ADDRESS4 || WS.WORKSTATION_NO   AS TillID
                                , ADJ.ADJ_NO			            AS SequenceNo
                                , ADJ.CREATED_DATETIME			    AS BusinessDayDate
                                , ADJ.CREATED_DATETIME	            AS BeginDateTime
								, ADJ.POST_DATE                     AS EndDateTime   
                                , EMP.EMPL_NAME			            AS OperatorID
                                , CURRENCY.ALPHABETIC_CODE          AS CurrencyCode
                                , 'true'                            AS InventoryMovementSuccess
                                , 'AMA'	                            AS Region
                                , COUNTRY.COUNTRY_CODE              AS Country
                                , S.ADDRESS4			            AS AlternateStoreID
                                , ''							    AS CountID
                                , 'ADJUSTMENT'					    AS CountType
                                , CASE WHEN ADJ.STATUS = 4 
									THEN 'CLOSED' 
										WHEN ADJ.STATUS = 2 
										THEN 'CANCELLED' 
										ELSE 'PENDING' 
									END							    AS CountStatus
                                , PREF_REASON.NAME				    AS ReasonCode
                                , ADJ_COMMENT.COMMENTS				AS Comments
                                , ISI.DESCRIPTION1					AS ItemID
                                , ADJ_ITEM.ADJ_VALUE				AS QuantityShipped
                                , 'ON_HAND'							AS InventoryBucketID
                                , ISI.ITEM_SIZE						AS PTDIM1
								, ISI.ATTRIBUTE						AS PTDIM2
								, ''								AS PTStyle
								, ISI.ALU							AS PTEAN                               
								, ADJ.SID							AS AdjSid
							FROM
								RPS.ADJUSTMENT ADJ
							LEFT JOIN RPS.ADJ_ITEM ADJ_ITEM		ON ADJ.SID = ADJ_ITEM.ADJ_SID
							LEFT JOIN RPS.PREF_REASON			ON PREF_REASON.SID = ADJ.ADJ_REASON_SID
							LEFT JOIN RPS.STORE	S				ON S.SID = ADJ.STORE_SID
							LEFT JOIN RPS.SUBSIDIARY			ON ADJ.SBS_SID = SUBSIDIARY.SID
							LEFT JOIN RPS.REGION_SUBSIDIARY		ON SUBSIDIARY.SID = REGION_SUBSIDIARY.SBS_SID
							LEFT JOIN RPS.REGION				ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
							LEFT JOIN RPS.INVN_SBS_ITEM ISI		ON ISI.SID = ADJ_ITEM.ITEM_SID
							LEFT JOIN RPS.COUNTRY				ON COUNTRY.SID = SUBSIDIARY.COUNTRY_SID
							LEFT JOIN RPS.CURRENCY				ON CURRENCY.SID = SUBSIDIARY.BASE_CURRENCY_SID
							LEFT JOIN RPS.ADJ_COMMENT			ON ADJ.SID = ADJ_COMMENT.ADJ_SID
							LEFT JOIN RPS.EMPLOYEE EMP			ON EMP.SID = ADJ.CLERK_SID
							LEFT JOIN RPS.WORKSTATION WS		ON WS.SID = ADJ.WORKSTATION_SID
							WHERE
								ADJ.POST_DATE BETWEEN :FromDate AND :ToDate
								AND ADJ.ADJ_TYPE = 0
								AND S.ADDRESS4 = :StoreCode
							ORDER BY 
								ADJ.POST_DATE DESC
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND ADJ.POST_DATE BETWEEN :FromDate AND :ToDate
					// TRUNC(ADJ.POST_DATE) BETWEEN DATE '2025-01-20' AND DATE '2025-08-20'

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreInventoryAdjustmentModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store Inventory Adjustment data: {ex.Message}");
					Console.WriteLine($"Error fetching Store Inventory Adjustment data: {ex.Message}");
					return new List<StoreInventoryAdjustmentModel>();
				}
			}
		}

	}
}
