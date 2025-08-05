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
					C.ALPHABETIC_CODE AS CurrencyId,
					TO_CHAR(S.ADDRESS5) AS StoreId,

					ISI.DESCRIPTION1 AS ProductCode,
					ISI.ALU AS Sku,
					ISI.ITEM_SIZE AS Waist,
					ISI.ATTRIBUTE AS Inseam,

					ISI.LAST_RCVD_DATE AS LastMovementDate,

					ISIQ.QTY AS Quantity,
					ISI.COST AS RetailPrice,

					SUBSTR(C.ALPHABETIC_CODE, 1, 2) AS CountryCode,
					ISI.UPC AS ManufactureUpc,
					ISI.UDF5_STRING AS Division,

					S.store_code AS StoreCode,
					ISI.upc AS Upc
				FROM rps.invn_sbs_item ISI
				LEFT JOIN rps.INVN_SBS_ITEM_QTY ISIQ ON ISIQ.INVN_SBS_ITEM_SID = ISI.SID
				LEFT JOIN rps.STORE S ON S.SID = ISIQ.store_sid
				LEFT JOIN rps.CURRENCY C ON C.SID = ISI.CURRENCY_SID
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

			// Optionally rethrow or return empty list
			throw; // OR return new List<Inventory>();
		}
	}
}
