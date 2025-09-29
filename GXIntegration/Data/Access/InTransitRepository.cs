using Dapper;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace GXIntegration_Levis.Data.Access
{
	public class InTransitRepository
	{
		private readonly string _connectionString;
		public InTransitRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<InTransitModel>> GetInventoryAsync(DateTime from_date, DateTime to_date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT
							SUBSTR(ISI.DESCRIPTION1, 1, LENGTH(ISI.DESCRIPTION1) - 1)	AS ProductCode
							, ISI.DESCRIPTION1 
								|| ISI.ITEM_SIZE
								|| ISI.ATTRIBUTE        AS Sku	
							, ISI.ITEM_SIZE				AS Waist
							, ISI.ATTRIBUTE				AS Inseam
							, TO_CHAR(STORE.ADDRESS4)	AS StoreCode
							, VOU_ITEM.QTY				AS Quantity
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
							TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate
							AND VOU.STATUS IN (1, 3)
							AND ISI.ACTIVE = 1
							AND STORE.ACTIVE = 1
					";

					//VOU.POST_DATE BETWEEN :FromDate AND :ToDate
					//TRUNC(VOU.POST_DATE) BETWEEN DATE '2025-07-20' AND DATE '2025-09-16'

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date
					};

					var sales = await connection.QueryAsync<InTransitModel>(sql, parameters);
					return sales.ToList();
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
