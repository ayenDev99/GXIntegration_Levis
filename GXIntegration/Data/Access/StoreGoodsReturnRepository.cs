using Dapper;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreGoodsReturnRepository
	{
		private readonly string _connectionString;
		public StoreGoodsReturnRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreGoodsReturnModel>> GetStoreGoodsReturnAsync(DateTime from_date, DateTime to_date, string storeCode, string processType)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string dateCondition;
					if (processType == "EOD")
					{
						dateCondition = "TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate";
					}
					else if (processType == "API")
					{
						dateCondition = "VOU.CREATED_DATETIME BETWEEN :FromDate AND :ToDate";
					}
					else
					{
						dateCondition = "TRUNC(VOU.POST_DATE) BETWEEN :FromDate AND :ToDate";
					}

					string sql = @"
							SELECT
								VOU.SID									AS VouSid
   								, '1'									AS TransOrganizationID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS TransRetailStoreID
								, VOU.WORKSTATION						AS TransWorkstationID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID) 
									|| VOU.WORKSTATION					AS TransTillID
								, LPAD(VOU.VOU_NO, 10, '0')	            AS TransSequenceNo
								, TRUNC(VOU.CREATED_DATETIME)           AS TransBusinessDayDate
								, VOU.CREATED_DATETIME	                AS TransBeginDateTime
								, VOU.POST_DATE                         AS TransEndDateTime
								, C.ALPHABETIC_CODE                     AS TransCurrencyCode
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS TransAlternateStoreID
								, TRIM(REGEXP_SUBSTR(
									VOU_COMMENT.COMMENTS, '^[^-]+'))	AS TransReasonCode
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS TransOriginAlternateStoreID
								, CASE WHEN VOU.STATUS = 4 
									THEN 'CLOSED' 
									ELSE 'PENDING' 
									END							        AS TransDocumentStatus
								, LPAD(VOU.VOU_NO, 10, '0')				AS TransDocumentID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS TransOriginatorName
								, VOU.MODIFIED_DATETIME			        AS TransCreationTimestamp
								, VOU.POST_DATE			                AS TransCompletionTimestamp
								, VOU.POST_DATE			                AS TransLastActivityTimestamp

								, '1'							        AS ShipmentSequence
								, VOU.POST_DATE			                AS ActualDeliveryDate
								, VOU.POST_DATE			                AS ActualShipDate
								, VENDOR.VEND_CODE						AS DestinationPartyID
								, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS DestinationRetailLocationID
								, 'SHIPPED'				                AS ShipmentStatusCode
								, ''				                    AS City
								, (SELECT ZIP FROM RPS.STORE 
									WHERE SID = VOU.STORE_SID)			AS PostalCode

								, ISB.ALU								AS ItemID
								, ISB.UPC				                AS ScannedBarcodeID
								, TO_CHAR(VI.QTY)						AS QuantityShipped
								, VI.ITEM_POS					        AS LineNumber
								, ISB.DESCRIPTION2				        AS Description
								, ISB.ITEM_SIZE							AS PTDIM1
								, ISB.ATTRIBUTE							AS PTDIM2
								, ISB.DESCRIPTION1						AS PTStyle
								, ISB.UPC								AS PTEAN
							FROM
								RPS.VOUCHER VOU
							LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
							LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
							LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VI.ITEM_SID
							LEFT JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND VOU.CLERK_SID = EMPLOYEE.SID
							LEFT JOIN RPS.CURRENCY C				ON SBS.BASE_CURRENCY_SID = C.SID
							LEFT JOIN RPS.VOU_COMMENT				ON VOU_COMMENT.VOU_SID = VOU.SID
							LEFT JOIN RPS.VENDOR					ON VENDOR.SID = VOU.VEND_SID
							WHERE
								{DATE_CONDITION}
								AND VOU.VOU_TYPE = 1
								AND VOU.VOU_CLASS = 0
								AND VOU.STATUS = 4
								AND VOU.STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)					
					";

					sql = sql.Replace("{DATE_CONDITION}", dateCondition);

					//Logger.Log($"Generated SQL: {sql}");

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var salesDictionary = new Dictionary<string, StoreGoodsReturnModel>();

					var sales = await connection.QueryAsync<StoreGoodsReturnModel, SGRItems, StoreGoodsReturnModel>(
						sql,
						(sale, item) =>
						{
							// Group by transaction (document)
							if (!salesDictionary.TryGetValue(sale.TransSequenceNo, out var existingSale))
							{
								existingSale = sale;
								existingSale.SGRItems = new List<SGRItems>();
								salesDictionary[sale.TransSequenceNo] = existingSale;
							}

							// --- Handle item ---
							SGRItems existingItem = null;
							if (!string.IsNullOrEmpty(item?.LineNumber))
							{
								existingItem = existingSale.SGRItems
									.FirstOrDefault(i => i.LineNumber == item.LineNumber);

								if (existingItem == null)
								{
									existingItem = item;
									existingSale.SGRItems.Add(existingItem);
								}
							}

							return existingSale;
						}
						, parameters
						, splitOn: "ItemID"
					).ConfigureAwait(false);

					return salesDictionary.Values.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store_Goods_Return data: {ex.Message}");
					return new List<StoreGoodsReturnModel>();
				}
			}
		}

	}
}
