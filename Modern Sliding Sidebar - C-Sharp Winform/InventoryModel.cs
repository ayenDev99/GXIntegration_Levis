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
						ISI.SID,
						ISI.CURRENCY_SID,

						C.ALPHABETIC_CODE,


						ISI.DESCRIPTION1,



						qty.qty,
						s.store_code,
						ISI.alu,
						ISI.upc
						
					FROM rps.invn_sbs_item ISI
					LEFT JOIN rps.invn_sbs_item_qty qty ON qty.invn_sbs_item_sid = ISI.SID
					LEFT JOIN rps.store s ON s.SID = qty.store_sid
					LEFT JOIN rps.CURRENCY C ON C.SID = ISI.CURRENCY_SID
					FETCH FIRST 1 ROWS ONLY";

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
