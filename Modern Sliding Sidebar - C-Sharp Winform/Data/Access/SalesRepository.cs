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
		public async Task<List<SalesModel>> GetSalesAsync(DateTime date, List<int> receiptTypes)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT 
								D.SID
								, S.STORE_NO AS StoreNo
								, S.ADDRESS5 AS AlternateStoreId
								, D.WORKSTATION_NO AS WorkstationNo
								, D.DOC_NO AS DocNo
								, D.CREATED_DATETIME as CreatedDateTime
								, D.INVC_POST_DATE as InvcPostDate
								, D.CASHIER_LOGIN_NAME as CashierLoginName
								, C.ALPHABETIC_CODE as CurrencyCode
								, CTRY.COUNTRY_CODE as CountryCode
								, DI.ITEM_POS as ItemSequenceNumber
								, DI.ALU AS Alu
								, D.DOC_NO AS TransactionCode
								, DI.SCAN_UPC AS Barcode
								, DI.ITEM_POS AS SequenceNumber
								, DI.CREATED_DATETIME AS BeginDateTime
								, DI.POST_DATE AS EndDateTime

								, DI.ALU AS ItemId
								, DI.DESCRIPTION2 AS Description
								, DI.PRICE AS RegularPrice
								, DI.ORIG_PRICE AS ActualPrice
								, DI.PRICE * DI.QTY AS ExtendedAmount
								, DI.QTY AS Quantity

							FROM 
								RPS.DOCUMENT D
							LEFT JOIN RPS.STORE S ON S.SID = D.STORE_SID
							LEFT JOIN RPS.DOCUMENT_ITEM DI ON DI.DOC_SID = D.SID
							LEFT JOIN RPS.TENDER T ON T.DOC_SID = D.SID
							LEFT JOIN RPS.CURRENCY C ON C.SID = T.CURRENCY_SID
							LEFT JOIN RPS.SUBSIDIARY SBS ON SBS.SID = D.SUBSIDIARY_SID
							LEFT JOIN RPS.COUNTRY CTRY ON CTRY.SID = SBS.COUNTRY_SID
							WHERE 
								S.STORE_NO = 1
								AND D.STATUS = 4
								AND D.RECEIPT_TYPE IN :ReceiptTypes
								AND D.DOC_NO IS NOT NULL
								AND TRUNC(D.CREATED_DATETIME) BETWEEN TO_DATE('2020-03-01', 'YYYY-MM-DD')
								AND TO_DATE('2025-03-31', 'YYYY-MM-DD')
							ORDER BY  
								S.STORE_NO ASC
								, D.WORKSTATION_NO ASC
								, D.DOC_NO ASC
							FETCH FIRST 1 ROWS ONLY
					";

					var parameters = new
					{
						SaleDate = date.Date,
						ReceiptTypes = receiptTypes
					};


					var sales = await connection.QueryAsync<SalesModel>(sql, parameters);
					return sales.ToList();
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
