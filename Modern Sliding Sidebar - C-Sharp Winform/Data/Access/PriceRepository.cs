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
							isi.description1 AS PC9,
							prclvl.price_lvl AS PriceLevel,
							'REG' AS ConditionType,
							TO_CHAR(MIN(TRUNC(adj.created_datetime) + INTERVAL '12' HOUR), 'DD-MON-YY HH24.MI.SS') || '.000000' AS PriceStartDate,
							'01-JAN-99 12.00.00.00000' AS PriceEndDate,
							MIN(TO_NUMBER(price.price)) 
								KEEP (DENSE_RANK FIRST ORDER BY adj.created_datetime) AS Price,
							'REG' AS Flag
						FROM rps.adjustment adj
						LEFT JOIN rps.adj_item adjitem ON adj.SID = adjitem.adj_sid
						LEFT JOIN rps.invn_sbs_item isi ON adjitem.item_sid = isi.SID
						LEFT JOIN rps.price_level prclvl ON adj.price_lvl_sid = prclvl.SID
						LEFT JOIN rps.invn_sbs_price price ON isi.SID = price.invn_sbs_item_sid
						WHERE TRUNC(adj.post_date) BETWEEN TO_DATE('01-MAR-20', 'DD-MON-YY') AND TO_DATE('31-MAR-20', 'DD-MON-YY')
						GROUP BY isi.description1, prclvl.price_lvl
						ORDER BY PC9
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
