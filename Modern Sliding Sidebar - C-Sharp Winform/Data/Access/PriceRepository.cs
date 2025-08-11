using Dapper;
using Modern_Sliding_Sidebar___C_Sharp_Winform;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GXIntegration_Levis.Model;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class PriceRepository
	{
		private readonly string _connectionString;

		public PriceRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<PriceModel>> GetPriceAsync(DateTime date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT
						  '1' AS SalesOrg,
						  ISB.DESCRIPTION1 AS PC9,
						  PLVL.PRICE_LVL as PriceLevel,
						  'REG' AS ConditionType,
						  ADJ.CREATED_DATETIME AS PriceStartDate,
						  '01-JAN-99' AS PriceEndDate,
						  RPS.ADJ_ITEM.PRICE as Price,
						  'REG' AS Flag
						FROM
						  RPS.ADJUSTMENT ADJ
						LEFT JOIN RPS.ADJ_ITEM
						ON
						  ADJ.SID = RPS.ADJ_ITEM.ADJ_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISB
						ON
						  ISB.SID = RPS.ADJ_ITEM.ITEM_SID
						LEFT JOIN RPS.PRICE_LEVEL PLVL
						ON
						  PLVL.SID = ADJ.PRICE_LVL_SID
						WHERE
						TRUNC(ADJ.CREATED_DATETIME) BETWEEN DATE '2025-01-01' AND DATE '2025-08-07'
						";



					return (await connection.QueryAsync<PriceModel>(sql, new { CreatedDate = date })).ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching inventory data: {ex.Message}");
					Console.WriteLine($"Error fetching inventory data: {ex.Message}");
					return new List<PriceModel>();
				}
			}
		}
	}
}
