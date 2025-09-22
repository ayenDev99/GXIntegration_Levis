using Dapper;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreSaleRepository
	{
		private readonly string _connectionString;
		public StoreSaleRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreSaleModel>> GetStoreSaleAsync(DateTime from_date, DateTime to_date, string storeCode)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT 
                                '1'                                     AS OrganizationID
                                , STORE.ADDRESS4			            AS RetailStoreID
                                , DOC.WORKSTATION_NO				    AS WorkstationID
                                , STORE.ADDRESS4 || DOC.WORKSTATION_NO  AS TillID
                                , DOC.DOC_NO				            AS SequenceNo
				                , DOC.CREATED_DATETIME				    AS BusinessDayDate
                                , DOC.CREATED_DATETIME				    AS BeginDateTime
                                , DOC.INVC_POST_DATE				    AS EndDateTime
                                , DOC.CASHIER_LOGIN_NAME				AS OperatorID
                                , CURRENCY.ALPHABETIC_CODE			    AS CurrencyCode
                                , 'PAPER'                               AS ReceiptDeliveryMethod
                                , 'true'                                AS InventoryMovementSuccess 
                                , 'AMA'                                 AS Region
                                , COUNTRY.COUNTRY_CODE                  AS Country
				                , STORE.ADDRESS4			            AS AlternateStoreID
                                , DOC.DOC_NO                            AS TransactionCode
                                , DOC_ITEM.SCAN_UPC                     AS Barcode
                                , DOC_ITEM.ITEM_POS                     AS LineItemSequenceNo
                                , DOC_ITEM.ITEM_POS                     AS LineItemLineNumber
				                , DOC_ITEM.CREATED_DATETIME             AS LineItemBeginDateTime
                                , DOC_ITEM.POST_DATE                    AS LineItemEndDateTime
                                , DOC_ITEM.ALU                          AS SaleItemID
                                , DOC_ITEM.DESCRIPTION2                 AS SaleDescription
                                , DOC_ITEM.PRICE                        AS SaleRegularSalesUnitPrice
                                , DOC_ITEM.ORIG_PRICE                   AS SaleActualSalesUnitPrice
                                , DOC_ITEM.PRICE * DOC_ITEM.QTY         AS SaleExtendedAmount
                                , DOC_ITEM.QTY                          AS SaleQuantity
                                , '10'                                  AS Division
                                , '00674'                               AS Department
                                , '00054'                               AS SubDepartment
                                , '02'                                  AS Class
                                , DOC_ITEM.SCAN_UPC                     AS ScannedItemID
                                , ''                                    AS GiftReceiptFlag
                                , ''                                    AS AssociateID
                                , ''                                    AS Percentage
                                , ''                                    AS TaxAuthority
                                , DOC.TRANSACTION_TOTAL_TAX_AMT         AS TaxableAmount
                                , DOC.TRANSACTION_TOTAL_AMT             AS Amount
                                , DOC.TAX_AREA_PERC                     AS Percent
                                , DOC.TAX_AREA_PERC                     AS RawTaxPercentage
                                , ''                                    AS TaxGroupID
                                , 'yes'                                 AS DealItemPercentOff
                                , ISI.ITEM_SIZE						    AS PTDIM1
                                , ISI.ATTRIBUTE						    AS PTDIM2
                                , ''								    AS PTStyle
                                , ISI.ALU							    AS PTEAN   
                                , TENDER.TENDER_POS                     AS TenderSequenceNo
                                , TENDER.TENDER_POS                     AS TenderLineNumber
                                , TENDER.CREATED_DATETIME               AS TenderBeginDateTime
                                , TENDER.POST_DATE                      AS TenderEndDateTime
                                , CASE 
                                    WHEN TENDER.TENDER_TYPE = 0 THEN 'Cash'
                                    WHEN TENDER.TENDER_TYPE = 1 THEN 'Check'
                                    WHEN TENDER.TENDER_TYPE = 2 THEN 'CreditCard'
                                    WHEN TENDER.TENDER_TYPE = 3 THEN 'COD'
                                    WHEN TENDER.TENDER_TYPE = 4 THEN 'Charge'
                                    WHEN TENDER.TENDER_TYPE = 5 THEN 'StoreCredit'
                                    WHEN TENDER.TENDER_TYPE = 6 THEN 'Split'
                                    WHEN TENDER.TENDER_TYPE = 7 THEN 'Deposit'
                                    WHEN TENDER.TENDER_TYPE = 8 THEN 'Payments'
                                    WHEN TENDER.TENDER_TYPE = 9 THEN 'GiftCertificate'
                                    WHEN TENDER.TENDER_TYPE = 10 THEN 'GiftCard'
                                    WHEN TENDER.TENDER_TYPE = 11 THEN 'DebitCard'
                                    WHEN TENDER.TENDER_TYPE = 12 THEN 'ForeignCurrency'
                                    WHEN TENDER.TENDER_TYPE = 13 THEN 'TravelerCheck'
                                    WHEN TENDER.TENDER_TYPE = 14 THEN 'ForeignCheck'
                                    WHEN TENDER.TENDER_TYPE = 15 THEN 'CentralGiftCard'
                                    WHEN TENDER.TENDER_TYPE = 16 THEN 'CentralGiftCertificate'
                                    WHEN TENDER.TENDER_TYPE = 17 THEN 'CentralCustomerCredit'
                                    WHEN TENDER.TENDER_TYPE = 18 THEN 'CentralCustomerLoyalty'
                                    ELSE ''
                                  END                                   AS TenderType
                                , CASE 
                                    WHEN DOC.RECEIPT_TYPE = 0 THEN 'SALE'
                                    WHEN DOC.RECEIPT_TYPE = 2 THEN 'DEPOSIT'
                                    ELSE ''
                                  END                                   AS TypeCode
                                , 'false'                               AS ChangeFlag
                                , CASE 
                                    WHEN TENDER.TENDER_TYPE = 2 THEN TO_CHAR(TENDER_CREDIT_CARD.CARD_TYPE_NAME)
                                    ELSE ''
                                    END                                 AS TenderID
                                , CURRENCY.ALPHABETIC_CODE              AS AmountCurrency 
                                , TENDER.AMOUNT                         AS TenderAmount
                                , TENDER.AMOUNT                         AS TransactionGrandAmount
                                , ROUND(TO_NUMBER(TENDER.AMOUNT))       AS RoundedTotal
                                , DOC.SID					            AS DocSid
                            FROM 
				                RPS.DOCUMENT DOC
                            LEFT JOIN RPS.STORE			            ON STORE.SID = DOC.STORE_SID
                            LEFT JOIN RPS.DOCUMENT_ITEM DOC_ITEM	ON DOC_ITEM.DOC_SID = DOC.SID
                            LEFT JOIN RPS.INVN_SBS_ITEM ISI         ON ISI.SID = DOC_ITEM.INVN_SBS_ITEM_SID
                            LEFT JOIN RPS.TENDER 			        ON TENDER.DOC_SID = DOC.SID
                            LEFT JOIN RPS.TENDER_CREDIT_CARD		ON TENDER_CREDIT_CARD.TENDER_SID = TENDER.SID
                            LEFT JOIN RPS.CURRENCY 		            ON CURRENCY.SID = TENDER.CURRENCY_SID
                            LEFT JOIN RPS.SUBSIDIARY SBS	        ON SBS.SID = DOC.SUBSIDIARY_SID
                            LEFT JOIN RPS.COUNTRY 		            ON COUNTRY.SID = SBS.COUNTRY_SID
                            WHERE 
				                DOC.STATUS = 4
				                AND DOC.RECEIPT_TYPE IN (0,2)
				                AND DOC.DOC_NO IS NOT NULL
                                AND DOC.POST_DATE BETWEEN :FromDate AND :ToDate
                                AND STORE.ADDRESS4 = :StoreCode
                            ORDER BY  
				                STORE.STORE_NO ASC
				                , DOC.WORKSTATION_NO ASC
				                , DOC.DOC_NO ASC
					";

					//FETCH FIRST 1 ROWS ONLY
					//AND DOC.POST_DATE BETWEEN :FromDate AND :ToDate
					//AND TRUNC(DOC.POST_DATE) BETWEEN DATE '2025-09-15' AND DATE '2025-09-20'

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreSaleModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching sales data: {ex.Message}");
					Console.WriteLine($"Error fetching sakes data: {ex.Message}");
					return new List<StoreSaleModel>();
				}
			}
		}

	}
}
