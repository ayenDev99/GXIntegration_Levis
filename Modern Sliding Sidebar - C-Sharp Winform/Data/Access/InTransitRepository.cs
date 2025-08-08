using Dapper;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class InTransitRepository
	{
		private readonly string _connectionString;
		public InTransitRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<InTransitModel>> GetInventoryAsync(DateTime date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT
							CT.COUNTRY_CODE AS CurrencyId
							, ISI.DESCRIPTION1 AS ProductCode
							, ISI.ALU AS Sku
							, ISI.ITEM_SIZE AS Waist
							, ISI.ATTRIBUTE AS Inseam
							, TO_CHAR(S.ADDRESS5) AS StoreCode
							, ISIQ.QTY AS Quantity
						FROM rps.INVN_SBS_ITEM ISI
						LEFT JOIN rps.INVN_SBS_ITEM_QTY ISIQ ON ISIQ.INVN_SBS_ITEM_SID = ISI.SID
						LEFT JOIN rps.STORE S ON S.SID = ISIQ.STORE_SID
						LEFT JOIN rps.CURRENCY C ON C.SID = ISI.CURRENCY_SID
						LEFT JOIN rps.SUBSIDIARY SBS ON SBS.SID = ISI.SBS_SID
						LEFT JOIN rps.COUNTRY CT ON CT.SID = SBS.COUNTRY_SID
						WHERE 
							ISI.active = 1
							AND TRUNC(ISI.post_date) BETWEEN 
								TO_DATE('01-MAR-25', 'DD-MON-YY') AND 
								TO_DATE('31-MAR-25', 'DD-MON-YY')
						GROUP BY 
							CT.COUNTRY_CODE
							, ISI.DESCRIPTION1
							, ISI.ALU
							, ISI.ITEM_SIZE
							, ISI.ATTRIBUTE
							, TO_CHAR(S.ADDRESS5)
							, ISIQ.QTY
						ORDER BY 
							ISI.ITEM_SIZE
					";

					return (await connection.QueryAsync<InTransitModel>(sql, new { CreatedDate = date })).ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching INTRANSIT data: {ex.Message}");
					Console.WriteLine($"Error fetching INTRANSIT data: {ex.Message}");
					return new List<InTransitModel>();
				}
			}
		}

	}
}
