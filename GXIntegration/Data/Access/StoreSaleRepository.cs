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

		public async Task<List<StoreSaleModel>> GetStoreSaleAsync(DateTime fromDate, DateTime toDate, string storeCode, string processType)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync().ConfigureAwait(false);

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
						dateCondition = "TRUNC(ADJ.POST_DATE) BETWEEN :FromDate AND :ToDate";
					}

					string sql = $@"
                        SELECT 
                            DOC.SID                                             AS DocSid
                            , '1'                                               AS TransOrganizationID
                            , STORE.ADDRESS4                                    AS TransRetailStoreID
                            , DOC.WORKSTATION_NO                                AS TransWorkstationID
                            , STORE.ADDRESS4 || DOC.WORKSTATION_NO              AS TransTillID
                            , ''                                                AS TransCashDrawerID
                            , DOC.DOC_NO                                        AS TransSequenceNo
                            , DOC.CREATED_DATETIME                              AS TransBusinessDayDate
                            , DOC.CREATED_DATETIME                              AS TransBeginDateTime
                            , DOC.INVC_POST_DATE                                AS TransEndDateTime
                            , DOC.CASHIER_LOGIN_NAME                            AS TransOperatorID
                            , CURRENCY.ALPHABETIC_CODE                          AS TransCurrencyCode
                            , STORE.ADDRESS4                                    AS TransAlternateStoreID
                            , DOC.DOC_NO                                        AS TransTransactionCode
                            , DOC_ITEM.SCAN_UPC                                 AS TransBarcode

                            , DOC.SALE_TOTAL_AMT                                AS TransGrandAmount
                            , '0.00'                                            AS TransRoundedTotal

                            , DOC_ITEM.ITEM_POS                                 AS ItemSequenceNo
                            , DOC_ITEM.ITEM_POS                                 AS ItemLineNumber
                            , DOC_ITEM.CREATED_DATETIME                         AS ItemBeginDateTime
                            , DOC_ITEM.POST_DATE                                AS ItemEndDateTime
                            , ISI.NON_INVENTORY                                 AS NonInvnFlag1
                            , ISI.KIT_TYPE                                      AS NonInvnFlag2
							, REPLACE(ISI.ALU, '-', '')                         AS SaleItemID
                            , DOC_ITEM.DESCRIPTION2                             AS SaleDescription
                            , DOC_ITEM.ORIG_PRICE                               AS SaleRegularSalesUnitPrice
                            , DOC_ITEM.PRICE                                    AS SaleActualSalesUnitPrice
                            , DOC_ITEM.PRICE * DOC_ITEM.QTY                     AS SaleExtendedAmount
                            , DOC_ITEM.QTY                                      AS SaleQuantity
                            , '10'                                              AS SaleBrand
                            , '10-0001-00673'                                   AS SaleCategory
                            , '10-0001-00673-1020'                              AS SaleClass
                            , '10-0001-00673-1020-2200'                         AS SaleSubClass
                            , DOC_ITEM.SCAN_UPC                                 AS SaleScannedItemID
                            , 'false'                                           AS SaleGiftReceiptFlag
                            , ISI.ITEM_SIZE                                     AS PTDIM1
                            , ISI.ATTRIBUTE                                     AS PTDIM2
                            , ISI.DESCRIPTION1                                  AS PTStyle
                            , ISI.UPC                                           AS PTEAN
                            , 'PH_' || DOC_ITEM.TAX_AREA_NAME                   AS TaxAuthority
                            , ROUND(DOC_ITEM.DIP_PRICE, 2)                      AS TaxableAmount
                            , ROUND(DOC_ITEM.DIP_TAX_AMT * DOC_ITEM.QTY , 2)    AS Amount
                            , DOC.TAX_AREA_PERC / 100                           AS Percent
                            , DOC.TAX_AREA_PERC / 100                           AS RawTaxPercentage
                            , ''                                                AS TaxLocationID
                            , '1'                                               AS TaxGroupID

                            , DOC_ITEM_DISC.DISC_POS                            AS DiscSequenceNo
                            , DOC_ITEM_DISC.NEW_DISC_AMT * DOC_ITEM.QTY         AS DiscAmount
                            , DOC_ITEM_DISC.DISC_REASON                         AS DiscPromotionID
                            , 'TRANSACTION_DISCOUNT'                            AS DiscReasonCode

                            , TENDER.SID                                        AS TenderSID
                            , TENDER.TENDER_POS                                 AS TenderSequenceNo
                            , TENDER.TENDER_POS                                 AS TenderLineNumber
                            , TENDER.CREATED_DATETIME                           AS TenderBeginDateTime
                            , TENDER.POST_DATE                                  AS TenderEndDateTime
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
                            END                                             AS TenderType
                            , CASE 
                                WHEN DOC.RECEIPT_TYPE = 0 THEN 'SALE'
                                WHEN DOC.RECEIPT_TYPE = 2 THEN 'DEPOSIT'
                                ELSE ''
                            END                                             AS TenderTypeCode
                            , CASE 
                                WHEN TENDER.TENDER_TYPE = 2 THEN TO_CHAR(TENDER_CREDIT_CARD.CARD_TYPE_NAME)
                                WHEN TENDER.TENDER_TYPE = 0 THEN 'CASH'
                                WHEN TENDER.TENDER_TYPE = 15 THEN 'CENTRALGC'
                                WHEN TENDER.TENDER_TYPE = 9 THEN 'GIFTCERT'
                                ELSE ''
                            END                                             AS TenderID
                            , CURRENCY.ALPHABETIC_CODE                      AS AmountCurrency
                            , TENDER.AMOUNT                                 AS TenderAmount
                            , TENDER_CREDIT_CARD.AUTH_CODE                  AS TenderAuthorizationNumber
                        FROM 
                            RPS.DOCUMENT DOC
                        LEFT JOIN RPS.STORE                                 ON STORE.SID = DOC.STORE_SID
                        LEFT JOIN RPS.DOCUMENT_ITEM DOC_ITEM                ON DOC_ITEM.DOC_SID = DOC.SID
                        LEFT JOIN RPS.DOCUMENT_ITEM_DISC DOC_ITEM_DISC      ON DOC_ITEM_DISC.DOC_ITEM_SID = DOC_ITEM.SID
                        LEFT JOIN RPS.INVN_SBS_ITEM ISI                     ON ISI.SID = DOC_ITEM.INVN_SBS_ITEM_SID
                        LEFT JOIN RPS.TENDER                                ON TENDER.DOC_SID = DOC.SID
                        LEFT JOIN RPS.TENDER_CREDIT_CARD                    ON TENDER_CREDIT_CARD.TENDER_SID = TENDER.SID
                        LEFT JOIN RPS.CURRENCY                              ON CURRENCY.SID = TENDER.CURRENCY_SID
                        LEFT JOIN RPS.SUBSIDIARY SBS                        ON SBS.SID = DOC.SUBSIDIARY_SID
                        LEFT JOIN RPS.COUNTRY                               ON COUNTRY.SID = SBS.COUNTRY_SID
                        WHERE 
                            {dateCondition}
                            AND DOC.STATUS = 4
                            AND DOC.RECEIPT_TYPE IN (0, 2)
                            AND DOC.DOC_NO IS NOT NULL
                            AND UPPER(STORE.ADDRESS4) = UPPER(:StoreCode)
                        ORDER BY  
                            STORE.STORE_NO ASC
                            , DOC.WORKSTATION_NO ASC
                            , DOC.DOC_NO ASC
                            , DOC_ITEM.ITEM_POS ASC
                            , DOC_ITEM_DISC.DISC_POS ASC
                    ";

					//Logger.Log($"Generated SQL: {sql}");

					var parameters = new
					{
						FromDate = fromDate,
						ToDate = toDate,
						StoreCode = storeCode
					};

					var salesDictionary = new Dictionary<string, StoreSaleModel>();

					var sales = await connection.QueryAsync<StoreSaleModel, Items, Discount, Tender, StoreSaleModel>(
						sql,
						(sale, item, disc, tender) =>
						{
							// Group by transaction (document)
							if (!salesDictionary.TryGetValue(sale.TransSequenceNo, out var existingSale))
							{
								existingSale = sale;
								existingSale.Items = new List<Items>();
								existingSale.Tenders = new List<Tender>();
								salesDictionary[sale.TransSequenceNo] = existingSale;
							}

							// --- Handle item ---
							Items existingItem = null;
							if (!string.IsNullOrEmpty(item?.ItemSequenceNo))
							{
								existingItem = existingSale.Items
									.FirstOrDefault(i => i.ItemSequenceNo == item.ItemSequenceNo);

								if (existingItem == null)
								{
									existingItem = item;
									existingItem.Discounts = new List<Discount>();
									existingSale.Items.Add(existingItem);
								}
							}

							// --- Handle discount (attach to correct item only) ---
							if (!string.IsNullOrEmpty(disc?.DiscSequenceNo) && existingItem != null)
							{
								if (!existingItem.Discounts.Any(d => d.DiscSequenceNo == disc.DiscSequenceNo))
									existingItem.Discounts.Add(disc);
							}

							// --- Handle tender (at sale level) ---
							if (!string.IsNullOrEmpty(tender?.TenderSID))
							{
								if (!existingSale.Tenders.Any(t => t.TenderSID == tender.TenderSID))
									existingSale.Tenders.Add(tender);
							}

							return existingSale;
						}
						, parameters
						, splitOn: "ItemSequenceNo,DiscSequenceNo,TenderSID"
					).ConfigureAwait(false);

					return salesDictionary.Values.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching sales data: {ex.Message}"); 
                    return new List<StoreSaleModel>();
				}
			}
		}
	}
}
