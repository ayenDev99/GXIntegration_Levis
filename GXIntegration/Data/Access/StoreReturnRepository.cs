using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreReturnRepository
	{
		private readonly string _connectionString;
		public StoreReturnRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreReturnModel>> GetStoreReturnAsync(DateTime from_date, DateTime to_date, List<int> receiptTypes)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT 
								D.SID					AS DocSid
								, TO_CHAR(S.ADDRESS4)	AS StoreCode
								, S.ADDRESS5			AS AlternateStoreId
								, D.WORKSTATION_NO		AS WorkstationNo
								, D.DOC_NO				AS DocNo
								, D.CREATED_DATETIME	AS CreatedDateTime
								, D.INVC_POST_DATE		AS InvcPostDate
								, D.CASHIER_LOGIN_NAME	AS CashierLoginName
								, C.ALPHABETIC_CODE		AS CurrencyCode
								, CTRY.COUNTRY_CODE		AS CountryCode
								, DI.ITEM_POS			AS ItemSequenceNumber
								, DI.ALU				AS Alu
								, D.DOC_NO				AS TransactionCode
								, DI.SCAN_UPC			AS Barcode
								, DI.ITEM_POS			AS SequenceNumber
								, DI.CREATED_DATETIME	AS BeginDateTime
								, DI.POST_DATE			AS EndDateTime
								, DI.ALU				AS ItemId
								, DI.DESCRIPTION2		AS Description
								, DI.PRICE				AS RegularPrice
								, DI.ORIG_PRICE			AS ActualPrice
								, DI.PRICE * DI.QTY		AS ExtendedAmount
								, DI.QTY				AS Quantity

							FROM 
								RPS.DOCUMENT D
							LEFT JOIN RPS.STORE S			ON S.SID = D.STORE_SID
							LEFT JOIN RPS.DOCUMENT_ITEM DI	ON DI.DOC_SID = D.SID
							LEFT JOIN RPS.TENDER T			ON T.DOC_SID = D.SID
							LEFT JOIN RPS.CURRENCY C		ON C.SID = T.CURRENCY_SID
							LEFT JOIN RPS.SUBSIDIARY SBS	ON SBS.SID = D.SUBSIDIARY_SID
							LEFT JOIN RPS.COUNTRY CTRY		ON CTRY.SID = SBS.COUNTRY_SID
							WHERE 
								S.STORE_NO = 1
								AND D.STATUS = 4
								AND D.RECEIPT_TYPE IN :ReceiptTypes
								AND D.DOC_NO IS NOT NULL
								AND TRUNC(D.POST_DATE) BETWEEN DATE '2024-01-01' AND DATE '2024-02-17'
							ORDER BY  
								S.STORE_NO ASC
								, D.WORKSTATION_NO ASC
								, D.DOC_NO ASC
					";


					//FETCH FIRST 1 ROWS ONLY
					//AND D.CREATED_DATETIME BETWEEN :FromDate AND :ToDate

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						ReceiptTypes = receiptTypes
					};

					var sales = await connection.QueryAsync<StoreReturnModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching sales data: {ex.Message}");
					Console.WriteLine($"Error fetching sakes data: {ex.Message}");
					return new List<StoreReturnModel>();
				}
			}
		}

	}
}
