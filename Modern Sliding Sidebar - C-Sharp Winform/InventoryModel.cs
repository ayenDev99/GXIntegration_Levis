using Dapper;
using Modern_Sliding_Sidebar___C_Sharp_Winform;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InventoryModel
{
	private readonly string _connectionString;

	public InventoryModel(string connectionString)
	{
		_connectionString = connectionString;
	}

	public async Task<List<Inventory>> GetMainData()
	{
		try
		{
			using (var connection = new OracleConnection(_connectionString))
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

				WHERE ISI.SID = 555545791000195684
					";

				var data = await connection.QueryAsync<Inventory>(sql);
				return data.AsList();
			}
		}
		catch (Exception ex)
		{
			// Debug log or rethrow
			Console.WriteLine("❌ SQL Error in GetMainData(): " + ex.Message);
			Console.WriteLine("🔍 Stack Trace: " + ex.StackTrace);

			throw; // OR return new List<Inventory>();
		}
	}
}
