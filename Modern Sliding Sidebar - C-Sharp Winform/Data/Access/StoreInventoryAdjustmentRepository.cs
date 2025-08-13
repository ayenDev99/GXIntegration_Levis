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
	public class StoreInventoryAdjustmentRepository
	{
		private readonly string _connectionString;
		public StoreInventoryAdjustmentRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreInventoryAdjustmentModel>> GetStoreInventoryAdjustmentAsync(DateTime date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							--  AND VOU.STATUS = 4
							FETCH FIRST 1 ROWS ONLY
					";

					return (await connection.QueryAsync<StoreInventoryAdjustmentModel>(sql, new { CurrentDate = date })).ToList();
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
