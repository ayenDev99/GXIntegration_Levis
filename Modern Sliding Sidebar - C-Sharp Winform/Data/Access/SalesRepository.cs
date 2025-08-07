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
	public class SalesRepository
	{
		private readonly string _connectionString;
		public SalesRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<SalesModel>> GetSalesAsync(DateTime date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT 
							d.sid,
							s.store_no AS StoreNo,
							d.workstation_no AS WorkstationNo,
							d.doc_no AS DocNo,
							d.created_datetime as CreatedDateTime,
							d.invc_post_date as InvcPostDate,
							d.cashier_login_name as CashierLoginName,



							c.alphabetic_code as CurrencyCode,
							ctry.country_code as CountryCode,
	
							di.item_pos as ItemSequenceNumber,

							di.alu AS Alu
							
							
						FROM 
							rps.document d
						LEFT JOIN 
							rps.store s ON s.sid = d.store_sid
						LEFT JOIN   
							rps.document_item di ON di.doc_sid = d.sid
						LEFT JOIN   
							rps.tender t ON t.doc_sid = d.sid
						LEFT JOIN   
							rps.currency c ON c.sid = t.currency_sid
						LEFT JOIN   
								rps.subsidiary sbs ON sbs.sid = d.subsidiary_sid
						LEFT JOIN   
								rps.country ctry ON ctry.sid = sbs.country_sid

						WHERE 
							s.store_no = 1
							AND d.status IN (3, 4)
							AND d.doc_no IS NOT NULL
							AND TRUNC(d.created_datetime) BETWEEN TO_DATE('2020-03-01', 'YYYY-MM-DD')
															   AND TO_DATE('2020-03-31', 'YYYY-MM-DD')
						ORDER BY  
							s.store_no ASC,
							d.workstation_no ASC,
							d.doc_no ASC


					";
					return (await connection.QueryAsync<SalesModel>(sql, new { SaleDate = date })).ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching sales data: {ex.Message}");
					Console.WriteLine($"Error fetching sakes data: {ex.Message}");
					return new List<SalesModel>();
				}
			}
		}
	}
}
