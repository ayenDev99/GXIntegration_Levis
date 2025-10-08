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

namespace GXIntegration_Levis.Data.Access
{
	public class PriceRepository
	{
		private readonly string _connectionString;

		public PriceRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<PriceModel>> GetPriceAsync(DateTime procDate)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					//display latest price post_date
					await connection.OpenAsync();
					string sql = @"
						SELECT 
							'1'								AS SalesOrg
							, CASE 
								WHEN SUBSTR(ISI.DESCRIPTION1, -1) = '0' 
								THEN SUBSTR(ISI.DESCRIPTION1, 1, LENGTH(ISI.DESCRIPTION1) - 1)
								ELSE ISI.DESCRIPTION1
							END								AS PC9
							, PL.PRICE_LVL_NAME				AS PriceLevel
							, 'REG'							AS ConditionType
							, MAX(ISIP.CREATED_DATETIME)	AS PriceStartDate
							, MAX(ISIP.POST_DATE)			AS PriceEndDate
							, TO_CHAR(MAX(ISIP.PRICE))		AS Price
							, 'REG'							AS Flag
						FROM
							RPS.STORE
						LEFT JOIN RPS.INVN_SBS_PRICE ISIP	ON ISIP.PRICE_LVL_SID = STORE.ACTIVE_PRICE_LVL_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISI		ON ISI.SID = ISIP.INVN_SBS_ITEM_SID
						LEFT JOIN RPS.PRICE_LEVEL PL		ON PL.SID = ISIP.PRICE_LVL_SID
						WHERE 
							STORE.ACTIVE = 1
							AND ISI.ACTIVE = 1
							AND STORE.ADDRESS4 IS NOT NULL
							AND TRUNC(ISIP.POST_DATE) <= :ProcDate
						GROUP BY 
							CASE 
								WHEN SUBSTR(ISI.DESCRIPTION1, -1) = '0' 
								THEN SUBSTR(ISI.DESCRIPTION1, 1, LENGTH(ISI.DESCRIPTION1) - 1)
								ELSE ISI.DESCRIPTION1
							END
							, PL.PRICE_LVL_NAME
						ORDER BY 
							MAX(ISIP.CREATED_DATETIME) DESC
					";

					var parameters = new
					{
						ProcDate = procDate
					};

					var sales = await connection.QueryAsync<PriceModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching price data: {ex.Message}");
					Console.WriteLine($"Error fetching price data: {ex.Message}");
					return new List<PriceModel>();
				}
			}
		}
	}
}
