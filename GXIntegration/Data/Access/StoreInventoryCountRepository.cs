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
	public class StoreInventoryCountRepository
	{
		private readonly string _connectionString;
		public StoreInventoryCountRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreInventoryCountModel>> GetStoreInventoryCountAsync(DateTime from_date, DateTime to_date, string storeCode)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT  
								'1'								AS OrganizationID
								, STORE.ADDRESS4				AS RetailStoreID
								, ''							AS WorkstationID
								,  STORE.ADDRESS4				AS TillID
								, PI_SHEET.NAME					AS SequenceNo
								, PI_SHEET.CREATED_DATETIME		AS BusinessDayDate
								, PI_SHEET.CREATED_DATETIME		AS BeginDateTime
								, PI_SHEET.POST_DATE			AS EndDateTime
								, PI_SHEET.CREATED_BY			AS OperatorID
								, CURRENCY.ALPHABETIC_CODE		AS CurrencyCode
								, 'AMA'							AS Region
								, COUNTRY.COUNTRY_CODE			AS Country
								, STORE.ADDRESS4				AS AlternateStoreID
								, ''							AS CountID
								, PI_SHEET.POST_DATE			AS DueDate
								, 'LEV_COUNT'					AS CountType
								, 'COMPLETE'					AS CountStatus
								, 'true'						AS VariancesAdjusted
								, INVN_SBS_ITEM.DESCRIPTION1	AS ItemCountItemID
								, INVN_SBS_ITEM.UPC				AS ItemCountScannedBarcodeID
								, INVN_SBS_ITEM.ITEM_SIZE		AS ItemCountDIM1
								, INVN_SBS_ITEM.ATTRIBUTE		AS ItemCountDIM2
								, PI_START.QTY					AS ItemCountQuantity
								, PI_START.QTY					AS ItemCountSnapshotQuantity
								, ''							AS ItemCountUnitVariance
								, 'ONHAND'						AS ItemCountInventoryBucketID
							FROM 
								RPS.PI_SHEET
							LEFT JOIN RPS.PI_START			ON PI_START.SHEET_SID = PI_SHEET.SID
							LEFT JOIN RPS.STORE				ON STORE.SID = PI_SHEET.STORE_SID
							LEFT JOIN RPS.PI_ZONE			ON PI_ZONE.SHEET_SID = PI_SHEET.SID
							LEFT JOIN RPS.INVN_SBS_ITEM		ON INVN_SBS_ITEM.SID = PI_START.INVN_ITEM_UID
							LEFT JOIN RPS.SUBSIDIARY		ON SUBSIDIARY.SID = PI_SHEET.SBS_SID
							LEFT JOIN RPS.CURRENCY			ON CURRENCY.SID = SUBSIDIARY.BASE_CURRENCY_SID
							LEFT JOIN RPS.COUNTRY			ON COUNTRY.SID = SUBSIDIARY.COUNTRY_SID
							WHERE 
								PI_SHEET.POST_DATE BETWEEN :FromDate AND :ToDate
								AND PI_SHEET.ACTIVE = 1
								AND STORE.ADDRESS4 = :StoreCode
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND PI_SHEET.POST_DATE BETWEEN :FromDate AND :ToDate

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreInventoryCountModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store Inventory Adjustment data: {ex.Message}");
					Console.WriteLine($"Error fetching Store Inventory Adjustment data: {ex.Message}");
					return new List<StoreInventoryCountModel>();
				}
			}
		}

	}
}
