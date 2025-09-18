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
		public async Task<List<StoreReturnModel>> GetStoreReturnAsync(DateTime from_date, DateTime to_date, string storeCode)
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
                                , 'PAPER'		                        AS ReceiptDeliveryMethod
                                , 'true'                                AS InventoryMovementSuccess 
                                , 'AMA'                                 AS Region
                                , COUNTRY.COUNTRY_CODE                  AS Country
                                , STORE.ADDRESS4			            AS AlternateStoreID    
                                , DOC.DOC_NO                            AS TransactionCode
                                , DOC_ITEM.SCAN_UPC                     AS Barcode
                                , STORE.ADDRESS4                        AS ReturnOriginalAltStoreID
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
                                , PREF_REASON.NAME                      AS SaleReason
                                , ''                                    AS SaleReturnType
                                , ''                                    AS AssociateID
                                , ''                                    AS Percentage
                                , ''                                    AS TaxAuthority
                                , DOC.TRANSACTION_TOTAL_TAX_AMT         AS TaxableAmount
                                , DOC.TRANSACTION_TOTAL_AMT             AS Amount
                                , DOC.TAX_AREA_PERC                     AS Percent
                                , DOC.TAX_AREA_PERC                     AS RawTaxPercentage
                                , ''                                    AS TaxGroupID
                                , STORE.ADDRESS4                        AS TransLinkRetailStoreID
                                , DOC.WORKSTATION_NO                    AS TransLinkWorkstationID
                                , DOC.DOC_NO                            AS TransLinkSequenceNumber
                                , DOC_ITEM.ITEM_POS                     AS TransLinkLineItemSequenceNo
                                , DOC_ITEM.CREATED_DATETIME             AS TransLinkBusinessDayDate
                                , 'yes'                                 AS DealItemPercentOff
                                , ''                                    AS LineItemOriginalTlogSequence
                                , STORE.ADDRESS4                        AS LineItemReturnOrgAltStoreID
                                , ''                                    AS LineItemNum
                                , ISI.ITEM_SIZE						    AS PTDIM1
                                , ISI.ATTRIBUTE						    AS PTDIM2
                                , ''								    AS PTStyle
                                , ISI.ALU							    AS PTEAN   
                                , '10'                                  AS MerchHierarchyDivision
                                , '00674'                               AS MerchHierarchyDepartment
                                , '00054'                               AS MerchHierarchySubDepartment
                                , '02'                                  AS MerchHierarchyClass
                                , ''                                    AS TaxAuthority1
                                , DOC.TRANSACTION_TOTAL_TAX_AMT         AS TaxableAmount1
                                , DOC.TRANSACTION_TOTAL_AMT             AS Amount1
                                , DOC.TAX_AREA_PERC                     AS Percent1
                                , DOC.TAX_AREA_PERC                     AS RawTaxPercentage1
                                , ''                                    AS TaxLocationID1
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
                                    WHEN DOC.RECEIPT_TYPE = 1 THEN 'REFUND'
                                    ELSE ''
                                  END                                   AS TypeCode
                                , 'false'                               AS ChangeFlag
                                , CASE 
                                    WHEN TENDER.TENDER_TYPE = 2 THEN TO_CHAR(TENDER_CREDIT_CARD.CARD_TYPE_NAME)
                                    ELSE ''
                                    END                                 AS TenderID
                                , CURRENCY.ALPHABETIC_CODE              AS AmountCurrency 
                                , TENDER.AMOUNT                         AS TenderAmount
        
                                , 'REFUND'                              AS VoucherTypeCode
                                , ''                                    AS VoucherDescription
                                , ''                                    AS VoucherFaceValueAmount
                                , ''                                    AS VoucherUnspentAmount
                                , ''                                    AS VoucherCardNumber
                                , TENDER.AMOUNT                         AS TransGrandAmount
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
                            LEFT JOIN RPS.PREF_REASON			    ON PREF_REASON.SID = DOC.REASON_CODE
                            WHERE 
                                DOC.STATUS = 4
                                AND DOC.RECEIPT_TYPE = 1
                                AND DOC.DOC_NO IS NOT NULL
                                AND TRUNC(DOC.POST_DATE) BETWEEN DATE '2025-01-15' AND DATE '2025-09-20'
                                AND STORE.ADDRESS4 = :StoreCode
                            ORDER BY  
                                STORE.STORE_NO ASC
                                , DOC.WORKSTATION_NO ASC
                                , DOC.DOC_NO ASC
					";


					//FETCH FIRST 1 ROWS ONLY
					//AND D.CREATED_DATETIME BETWEEN :FromDate AND :ToDate

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
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
