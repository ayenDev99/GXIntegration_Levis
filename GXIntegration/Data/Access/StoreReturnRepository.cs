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
	public class StoreReturnRepository
	{
		private readonly string _connectionString;
		public StoreReturnRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreReturnModel>> GetStoreReturnAsync(DateTime fromDate, DateTime toDate, string storeCode, string processType)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string dateCondition;
					if (processType == "EOD")
					{
						dateCondition = "TRUNC(DOC.INVC_POST_DATE) BETWEEN :FromDate AND :ToDate";
					}
					else if (processType == "API")
					{
						dateCondition = "DOC.INVC_POST_DATE BETWEEN :FromDate AND :ToDate";
					}
					else
					{
						dateCondition = "TRUNC(DOC.INVC_POST_DATE) BETWEEN :FromDate AND :ToDate";
					}

					string sql = $@"
							SELECT 
                                DOC.SID					                                AS DocSid
                                , '1'                                                   AS OrganizationID
                                , STORE.ADDRESS4			                            AS RetailStoreID
                                , DOC.WORKSTATION_NO				                    AS WorkstationID
                                , STORE.ADDRESS4 || DOC.WORKSTATION_NO                  AS TillID
                                , DOC.DOC_NO				                            AS SequenceNo
                                , DOC.CREATED_DATETIME				                    AS BusinessDayDate
                                , DOC.CREATED_DATETIME				                    AS BeginDateTime
                                , DOC.INVC_POST_DATE				                    AS EndDateTime
                                , DOC.CASHIER_LOGIN_NAME				                AS OperatorID
                                , CURRENCY.ALPHABETIC_CODE			                    AS CurrencyCode
                                , 'PAPER'		                                        AS ReceiptDeliveryMethod
                                , 'true'                                                AS InventoryMovementSuccess 
                                , 'AMA'                                                 AS Region
                                , 'PH'                                                  AS Country
                                , STORE.ADDRESS4			                            AS AlternateStoreID    
                                , DOC.DOC_NO                                            AS TransactionCode
                                , DOC_ITEM.SCAN_UPC                                     AS Barcode
                                , (SELECT ADDRESS4 FROM RPS.STORE 
								    WHERE SID = 
                                    (SELECT STORE_SID FROM RPS.DOCUMENT 
                                        WHERE SID = DOC.REF_SALE_SID))                  AS ReturnOriginalAltStoreID

                                , ROUND(TO_NUMBER(TENDER.AMOUNT))                       AS TransactionGrandAmount
                                , '0.00'                                                AS RoundedTotal

                                , DOC_ITEM.ITEM_POS                                     AS LineItemSequenceNo
                                , DOC_ITEM.ITEM_POS                                     AS LineItemLineNumber
                                , DOC_ITEM.CREATED_DATETIME                             AS LineItemBeginDateTime
                                , DOC_ITEM.POST_DATE                                    AS LineItemEndDateTime
								, REPLACE(ISI.ALU, '-', '')                             AS SaleItemID
                                , DOC_ITEM.DESCRIPTION2                                 AS SaleDescription
                                , DOC_ITEM.ORIG_PRICE * -1                              AS SaleRegularSalesUnitPrice
                                , DOC_ITEM.PRICE * -1                                   AS SaleActualSalesUnitPrice
                                , (DOC_ITEM.PRICE * DOC_ITEM.QTY) * -1                  AS SaleExtendedAmount
                                , DOC_ITEM.QTY                                          AS SaleQuantity
                                , PREF_REASON.NAME                                      AS SaleReason
                                , 'VERIFIED'                                            AS SaleReturnType
                                , DOC.EMPLOYEE1_LOGIN_NAME                              AS AssociateID
                                , '100'                                                 AS Percentage

                                , 'PH_' || DOC_ITEM.TAX_AREA_NAME                       AS TaxAuthority
                                , ROUND(DOC_ITEM.DIP_PRICE, 2) * -1                     AS TaxableAmount
                                , ROUND(DOC_ITEM.DIP_TAX_AMT  * DOC_ITEM.QTY, 2) * -1   AS Amount
                                , DOC.TAX_AREA_PERC / 100                               AS Percent
                                , DOC.TAX_AREA_PERC / 100                               AS RawTaxPercentage
                                , ''                                                    AS TaxLocationID
                                , '1'                                                   AS TaxGroupID

                                , (SELECT ADDRESS4 FROM RPS.STORE 
								    WHERE SID = 
                                    (SELECT STORE_SID FROM RPS.DOCUMENT 
                                        WHERE SID = DOC.REF_SALE_SID))                  AS TransLinkRetailStoreID
                                , DOC.WORKSTATION_NO                                    AS TransLinkWorkstationID
                                , (SELECT DOC_NO FROM RPS.DOCUMENT 
								    WHERE SID = DOC.REF_SALE_SID)                       AS TransLinkSequenceNumber
                                , DOC_ITEM.ITEM_POS                                     AS TransLinkLineItemSequenceNo
                                , DOC_ITEM.CREATED_DATETIME                             AS TransLinkBusinessDayDate

                                , 'yes'                                                 AS DealItemPercentOff
                                , ''                                                    AS LineItemOriginalTlogSequence
                                , (SELECT ADDRESS4 FROM RPS.STORE 
								    WHERE SID = 
                                    (SELECT STORE_SID FROM RPS.DOCUMENT 
                                        WHERE SID = DOC.REF_SALE_SID))                  AS LineItemReturnOrgAltStoreID
                                , DOC_ITEM.ITEM_POS                                     AS LineItemNum
                                , ISI.ITEM_SIZE						                    AS PTDIM1
                                , ISI.ATTRIBUTE						                    AS PTDIM2
                                , ISI.DESCRIPTION1						                AS PTStyle
                                , ISI.UPC							                    AS PTEAN   

                                , '10'                                                  AS MerchHierarchyDivision
                                , '00674'                                               AS MerchHierarchyDepartment
                                , '00054'                                               AS MerchHierarchySubDepartment
                                , '02'                                                  AS MerchHierarchyClass

                                , DOC_ITEM_DISC.DISC_POS                                AS DiscSequenceNo
                                , (DOC_ITEM_DISC.NEW_DISC_AMT * DOC_ITEM.QTY) * -1      AS DiscAmount
                                , DOC_ITEM_DISC.DISC_REASON                             AS DiscPromotionID
                                , 'TRANSACTION_DISCOUNT'                                AS DiscReasonCode

                                , TENDER.SID                                            AS TenderSID
                                , TENDER.TENDER_POS                                     AS TenderSequenceNo
                                , TENDER.TENDER_POS                                     AS TenderLineNumber
                                , TENDER.CREATED_DATETIME                               AS TenderBeginDateTime
                                , TENDER.POST_DATE                                      AS TenderEndDateTime
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
                                  END                                                   AS TenderType
                                , 'REFUND'                                              AS TypeCode
                                , 'false'                                               AS ChangeFlag
                                , CASE 
                                    WHEN TENDER.TENDER_TYPE = 2 THEN TO_CHAR(TENDER_CREDIT_CARD.CARD_TYPE_NAME)
                                    WHEN TENDER.TENDER_TYPE = 0 THEN 'CASH'
                                    WHEN TENDER.TENDER_TYPE = 15 THEN 'CENTRALGC'
                                    WHEN TENDER.TENDER_TYPE = 9 THEN 'GIFTCERT'
                                    ELSE ''
                                END                                                     AS TenderID
                                , CURRENCY.ALPHABETIC_CODE                              AS AmountCurrency 
                                , TENDER.AMOUNT                                         AS TenderAmount
                                , TENDER_CREDIT_CARD.AUTH_CODE                          AS TenderAuthorizationNumber                   
                            FROM 
                                RPS.DOCUMENT DOC
                            LEFT JOIN RPS.STORE			                                ON STORE.SID = DOC.STORE_SID
                            LEFT JOIN RPS.DOCUMENT_ITEM DOC_ITEM	                    ON DOC_ITEM.DOC_SID = DOC.SID
                            LEFT JOIN RPS.DOCUMENT_ITEM_DISC DOC_ITEM_DISC              ON DOC_ITEM_DISC.DOC_ITEM_SID = DOC_ITEM.SID                        
                            LEFT JOIN RPS.INVN_SBS_ITEM ISI                             ON ISI.SID = DOC_ITEM.INVN_SBS_ITEM_SID
                            LEFT JOIN RPS.TENDER 			                            ON TENDER.DOC_SID = DOC.SID
                            LEFT JOIN RPS.TENDER_CREDIT_CARD		                    ON TENDER_CREDIT_CARD.TENDER_SID = TENDER.SID
                            LEFT JOIN RPS.CURRENCY 		                                ON CURRENCY.SID = TENDER.CURRENCY_SID
                            LEFT JOIN RPS.SUBSIDIARY SBS	                            ON SBS.SID = DOC.SUBSIDIARY_SID
                            LEFT JOIN RPS.COUNTRY 		                                ON COUNTRY.SID = SBS.COUNTRY_SID
                            LEFT JOIN RPS.PREF_REASON			                        ON PREF_REASON.SID = DOC.REASON_CODE
                            WHERE 
                                {dateCondition}
                                AND DOC.STATUS = 4
                                AND DOC.RECEIPT_TYPE = 1
                                AND DOC.DOC_NO IS NOT NULL
                                AND STORE.ADDRESS4 = :StoreCode
                            ORDER BY  
                                STORE.STORE_NO ASC
                                , DOC.WORKSTATION_NO ASC
                                , DOC.DOC_NO ASC
                                , DOC_ITEM.ITEM_POS ASC
                                , DOC_ITEM_DISC.DISC_POS ASC
					";

					//Logger.LogOutbound($"Generated SQL: {sql}");

					var parameters = new
					{
						FromDate = fromDate,
						ToDate = toDate,
						StoreCode = storeCode
					};

					var salesDictionary = new Dictionary<string, StoreReturnModel>();

					var sales = await connection.QueryAsync<StoreReturnModel, ReturnItems, ReturnDiscount, ReturnTender, StoreReturnModel>(
						sql,
						(sale, item, disc, tender) =>
						{
							// Group by transaction (document)
							if (!salesDictionary.TryGetValue(sale.SequenceNo, out var existingSale))
							{
								existingSale = sale;
								existingSale.ReturnItems = new List<ReturnItems>();
								existingSale.ReturnTenders = new List<ReturnTender>();
								salesDictionary[sale.SequenceNo] = existingSale;
							}

							// --- Handle item ---
							ReturnItems existingItem = null;
							if (!string.IsNullOrEmpty(item?.LineItemSequenceNo))
							{
								existingItem = existingSale.ReturnItems
									.FirstOrDefault(i => i.LineItemSequenceNo == item.LineItemSequenceNo);

								if (existingItem == null)
								{
									existingItem = item;
									existingItem.ReturnDiscounts = new List<ReturnDiscount>();
									existingSale.ReturnItems.Add(existingItem);
								}
							}

							// --- Handle discount (attach to correct item only) ---
							if (!string.IsNullOrEmpty(disc?.DiscSequenceNo) && existingItem != null)
							{
								if (!existingItem.ReturnDiscounts.Any(d => d.DiscSequenceNo == disc.DiscSequenceNo))
									existingItem.ReturnDiscounts.Add(disc);
							}

							// --- Handle tender (at sale level) ---
							if (!string.IsNullOrEmpty(tender?.TenderSID))
							{
								if (!existingSale.ReturnTenders.Any(t => t.TenderSID == tender.TenderSID))
									existingSale.ReturnTenders.Add(tender);
							}

							return existingSale;
						}
						, parameters
						, splitOn: "LineItemSequenceNo,DiscSequenceNo,TenderSID"
					).ConfigureAwait(false);

					return salesDictionary.Values.ToList();
				}
				catch (Exception ex)
				{
					Logger.LogError($"Error fetching sales data: {ex.Message}");
					return new List<StoreReturnModel>();
				}
			}
		}

	}
}
