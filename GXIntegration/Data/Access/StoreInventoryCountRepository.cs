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

		public async Task<List<StoreInventoryCountModel>> GetPagedStoreInventoryCountAsync(
			DateTime fromDate,
			DateTime toDate,
			string storeCode,
			int startRow,
			int endRow)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				await connection.OpenAsync();

				string sql = @"
					SELECT * FROM (
						SELECT 
							t.*, 
							ROWNUM AS RN 
						FROM (
							SELECT  
								'1'									AS OrganizationID
								, STORE.ADDRESS4					AS RetailStoreID
								, '1'								AS WorkstationID
								, STORE.ADDRESS4 || '1'				AS TillID
								, PI_SHEET.NAME						AS SequenceNo
								, PI_SHEET.CREATED_DATETIME			AS BusinessDayDate
								, PI_SHEET.CREATED_DATETIME			AS BeginDateTime
								, PI_SHEET.POST_DATE				AS EndDateTime
								, PI_SHEET.CREATED_BY				AS OperatorID
								, 'PHP'								AS CurrencyCode
								, 'AMA'								AS Region
								, 'PHP'								AS Country
								, STORE.ADDRESS4					AS AlternateStoreID
								, PI_SHEET.NAME						AS CountID
								, PI_SHEET.POST_DATE				AS DueDate
								, 'LEV_COUNT'						AS CountType
								, 'COMPLETE'						AS CountStatus
								, 'true'							AS VariancesAdjusted
								, INVN_SBS_ITEM.DESCRIPTION1		AS ItemCountItemID
								, INVN_SBS_ITEM.UPC					AS ItemCountScannedBarcodeID
								, INVN_SBS_ITEM.ITEM_SIZE			AS ItemCountDIM1
								, INVN_SBS_ITEM.ATTRIBUTE			AS ItemCountDIM2
								, PI_ZONE_ITEM_V.START_QTY			AS ItemCountQuantity
								, PI_ZONE_ITEM_V.SCAN_QTY			AS ItemCountSnapshotQuantity
								, PI_ZONE_ITEM_V.DISCREPANCY_QTY	AS ItemCountUnitVariance
								, 'ON_HAND'							AS ItemCountInventoryBucketID
							FROM 
								RPS.PI_SHEET
								LEFT JOIN RPS.STORE ON STORE.SID = PI_SHEET.STORE_SID
								LEFT JOIN RPS.PI_ZONE ON PI_ZONE.SHEET_SID = PI_SHEET.SID
								LEFT JOIN RPS.PI_ZONE_ITEM_V ON PI_ZONE_ITEM_V.ZONE_SID = PI_ZONE.SID
								LEFT JOIN RPS.INVN_SBS_ITEM ON INVN_SBS_ITEM.SID = PI_ZONE_ITEM_V.INVN_SBS_ITEM_SID
							WHERE 
								TRUNC(PI_SHEET.POST_DATE) BETWEEN :FromDate AND :ToDate
								AND STORE.ADDRESS4 = :StoreCode
								AND PI_SHEET.ACTIVE = 1
						) t
					)
					WHERE RN BETWEEN :StartRow AND :EndRow
				";

				var parameters = new
				{
					FromDate = fromDate,
					ToDate = toDate,
					StoreCode = storeCode,
					StartRow = startRow,
					EndRow = endRow
				};

				//Logger.Log($"[TEST {sql}");

				var result = await connection.QueryAsync<StoreInventoryCountModel>(
					sql,
					param: parameters,
					transaction: null,
					commandTimeout: 300,
					commandType: System.Data.CommandType.Text);

				return result.ToList();
			}
		}
	}
}
