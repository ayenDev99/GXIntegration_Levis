using Dapper;
using GXIntegration;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace GXIntegration_Levis.Data.Access
{
	public class PriceRepository
	{
		private readonly string _connectionString;

		public PriceRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<PriceModel>> GetPriceAsync(DateTime from_date, DateTime to_date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
						SELECT
						  '1'						AS SalesOrg
						  , ISB.DESCRIPTION1		AS PC9
						  , PLVL.PRICE_LVL_NAME		AS PriceLevel
						  , 'REG'					AS ConditionType
						  , ADJ.CREATED_DATETIME	AS PriceStartDate
						  , ''						AS PriceEndDate
						  , ADJ_ITEM.ADJ_VALUE		AS Price
						  , 'REG'					AS Flag
						FROM
						  RPS.ADJUSTMENT ADJ
						LEFT JOIN RPS.ADJ_ITEM				ON ADJ.SID = RPS.ADJ_ITEM.ADJ_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISB		ON ISB.SID = RPS.ADJ_ITEM.ITEM_SID
						LEFT JOIN RPS.PRICE_LEVEL PLVL		ON PLVL.SID = ADJ.PRICE_LVL_SID
						WHERE
							ADJ.POST_DATE BETWEEN :FromDate AND :ToDate
							AND ADJ.ADJ_TYPE = 1
						";

					//ADJ.POST_DATE BETWEEN :FromDate AND :ToDate

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date
					};

					var sales = await connection.QueryAsync<PriceModel>(sql, parameters);
					return sales.ToList();
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
