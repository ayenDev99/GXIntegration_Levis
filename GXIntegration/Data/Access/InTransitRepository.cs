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
							ISI.DESCRIPTION1 AS ProductCode
							, ISI.ALU AS Sku
							, ISI.ITEM_SIZE AS Waist
							, ISI.ATTRIBUTE AS Inseam
							, TO_CHAR(STORE.ADDRESS5) AS StoreCode
							, VOU_ITEM.QTY AS Quantity
						FROM
						  RPS.VOUCHER VOU
						LEFT JOIN RPS.VOU_ITEM VOU_ITEM ON VOU.SID = RPS.VOU_ITEM.VOU_SID 
						LEFT JOIN RPS.STORE ON RPS.STORE.SID = VOU.STORE_SID
						LEFT JOIN RPS.SUBSIDIARY SUBS ON SUBS.SID = VOU.SBS_SID
						LEFT JOIN RPS.COUNTRY ON RPS.COUNTRY.SID = SUBS.COUNTRY_SID
						LEFT JOIN RPS.CURRENCY ON RPS.CURRENCY.SID = RPS.VOU_ITEM.CURRENCY_SID
						LEFT JOIN RPS.REGION_SUBSIDIARY ON SUBS.SID = RPS.REGION_SUBSIDIARY.SBS_SID
						LEFT JOIN RPS.REGION ON RPS.REGION.SID = RPS.REGION_SUBSIDIARY.REGION_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISI ON ISI.SID = RPS.VOU_ITEM.ITEM_SID
						WHERE 
							VOU.STATUS IN (1, 3)
							AND ISI.active = 1
							AND TRUNC(VOU.POST_DATE) BETWEEN 
								TO_DATE('01-JAN-25', 'DD-MON-YY') AND 
								TO_DATE('31-DEC-25', 'DD-MON-YY')
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
