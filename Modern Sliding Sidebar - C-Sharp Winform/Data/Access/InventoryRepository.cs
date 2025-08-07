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
	public class InventoryRepository
	{
		private readonly string _connectionString;

		public InventoryRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<InventoryModel>> GetInventoryAsync(DateTime date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{

					await connection.OpenAsync();

					string sql = @"
							SELECT 
							CT.COUNTRY_CODE AS CurrencyId,
							TO_CHAR(S.ADDRESS5) AS StoreId,

							ISI.DESCRIPTION1 AS ProductCode,
							ISI.ALU AS Sku,
							ISI.ITEM_SIZE AS Waist,
							ISI.ATTRIBUTE AS Inseam,

							ISI.LAST_RCVD_DATE AS LastMovementDate,

							ISIQ.QTY AS Quantity,
							ISI.COST AS RetailPrice,
							SUBSTR(CT.COUNTRY_CODE, 1, 2) AS CountryCode,
							ISI.UPC AS ManufactureUpc,
							ISI.UDF5_STRING AS Division


						FROM rps.INVN_SBS_ITEM ISI
						LEFT JOIN rps.INVN_SBS_ITEM_QTY ISIQ ON ISIQ.INVN_SBS_ITEM_SID = ISI.SID
						LEFT JOIN rps.STORE S ON S.SID = ISIQ.STORE_SID
						LEFT JOIN rps.CURRENCY C ON C.SID = ISI.CURRENCY_SID
						LEFT JOIN rps.SUBSIDIARY SBS ON SBS.SID = ISI.SBS_SID
						LEFT JOIN rps.COUNTRY CT ON CT.SID = SBS.COUNTRY_SID
						WHERE TRUNC(ISI.CREATED_DATETIME) = TO_DATE('2020-03-18', 'YYYY-MM-DD')
					";

					return (await connection.QueryAsync<InventoryModel>(sql, new { CreatedDate = date })).ToList();
				}
				catch (Exception ex)
				{
					// Log the exception or handle it as needed
					Console.WriteLine($"Error fetching inventory data: {ex.Message}");
					return new List<InventoryModel>();
				}
			}
		}
	}
}
