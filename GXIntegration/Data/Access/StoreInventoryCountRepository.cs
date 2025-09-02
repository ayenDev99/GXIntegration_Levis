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
								'1'							AS OrganizationID
								, S.ADDRESS4				AS StoreID
								, '1'						AS WorkstationID
								, S.ADDRESS4 
									|| '001' 
									|| '000100'				AS TillID
								, ''						AS SequenceNo
								, TO_CHAR(TRUNC
									(ISIQ.CREATED_DATETIME) 
									, 'YYYY-MM-DD')			AS BusinessDayDate
								, ISIQ.CREATED_DATETIME		AS BeginDateTime
								, ISIQ.CREATED_DATETIME		AS EndDateTime
								, '1'						AS OperatorID
								, 'PHP'						AS CurrencyCode
								, 'AMA'						AS Region
								, 'PHL'						AS Country
								, S.ADDRESS5				AS AlternateStoreID
								, ''						AS CountID
								,  TO_CHAR(TRUNC
									(ISIQ.CREATED_DATETIME) 
									, 'YYYY-MM-DD')			AS DueDate
								, ''						AS CountType
								, ''						AS CountStatus
								, ''						AS VarianceAdj
								, ISI.ALU					AS ItemID
								, ISI.UPC					AS ScannedBarcodeID
								, ISI.ITEM_SIZE				AS DIM1
								, ISI.ATTRIBUTE				AS DIM2
								, ISIQ.QTY					AS Quantity
								, ISIQ.QTY					AS SnapshotQty
								, ''						AS UnitVariance
								, 'ON_HAND'					AS InventoryBucketID
								FROM 
									RPS.INVN_SBS_ITEM_QTY ISIQ
								LEFT JOIN RPS.INVN_SBS_ITEM ISI ON ISI.SID = ISIQ.INVN_SBS_ITEM_SID
								LEFT JOIN RPS.STORE S ON S.SID = ISIQ.STORE_SID
								WHERE 
									TRUNC(ISIQ.POST_DATE) BETWEEN DATE '2025-08-20' AND DATE '2025-08-20'
									AND ISI.ACTIVE = 1
									AND S.ADDRESS4 = :StoreCode
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND D.POST_DATE BETWEEN :FromDate AND :ToDate

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
