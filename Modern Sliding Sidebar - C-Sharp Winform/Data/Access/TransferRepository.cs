using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace GXIntegration_Levis.Data.Access
{
	public class TransferRepository
	{
		private readonly string _connectionString;
		public TransferRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<TransferModel>> GetTransferAsync(DateTime date, List<int> vouType, List<int> vouClass)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT
								'1'								AS ORG_ID
								, VOU.ORIG_STORE_SID
								, STORE_FROM.ADDRESS5			AS STORE_ID
								, VOU.WORKSTATION
								, ''							AS TILL_ID
								, VOU.VOU_NO					AS SEQ_NO
								, VOU.CREATED_DATETIME			AS BUS_DATE
								, VOU.CREATED_DATETIME			AS BEG_TIME
								, VOU.CREATED_DATETIME			AS END_TIME
								, VOU.CLERK_SID					AS OPT_ID
								, CURRENCY.ALPHABETIC_CODE		AS CURR_CODE_TO
								, 'INVENTORY_MOVEMENT_SUCCESS'	AS P1_CODE
								, 'TRUE'						AS P1_VAL
								, 'REGION'						AS P2_CODE
								, REGION.REGION_NAME			AS P2_VAL
								, 'COUNTRY'						AS P3_CODE
								, COUNTRY.COUNTRY_CODE			AS P3_VAL
								, 'ALTERNATE_STOREID'			AS P4_CODE
								, ''							AS P4_VAL
								, 'CLOSED'						AS STATUS
								, VOU.VOU_NO					AS DOC_ID
								, STORE.ADDRESS5				AS RSTORE_ID
								, STORE_FROM.ADDRESS5			AS ORIGIN_STORE
								, STORE_FROM.STORE_NAME			AS ORIGIN_STORE_NAME
								, 'SHIPPING_STORE_TRANSFER'		AS DOC_TYPE_DESC
								, 'SHIPPING'					AS DOC_TYPE
								, 'STORE_TRANSFER'				AS DOC_SUBTYPE
								, 'STORE'						AS REC_CRE_TYPE
								, VOU.CREATED_DATETIME			AS CREATION_TS
								, CASE WHEN VOU.STATUS = 4 
									THEN VOU.MODIFIED_DATETIME 
									ELSE NULL END				AS COMPLETION_TS
								, '1'							AS SHIP_SEQ
								, ''							AS DEL_DATE
								, ''							AS SHIP_DATE
								, ''							AS LOCATION_ID
								, ''							AS SHIPPING_CARRIER
								, VOU.TRACKING_NO				AS TRACKING_NO
								, VOU.SHIPMENT_NO
								, ''							AS SHIP_STAT_CODE
								, STORE.ADDRESS1
								, STORE.ADDRESS2
								, STORE.ADDRESS3				AS CITY
								, STORE.ZIP						AS POSTAL
								, 'PH'							AS COUNTRY
								, VOU_ITEM.ITEM_POS				AS LINE_NO
								, ISB.DESCRIPTION1				AS LINE_PROP_CODE
								, ISB.DESCRIPTION2				AS LINE_PROP_DESC
								, VOU_ITEM.QTY * VOU_ITEM.PRICE AS LINE_PROP_VALUE
  
								FROM
									RPS.VOUCHER VOU
								LEFT JOIN RPS.VOU_ITEM					ON VOU.SID = VOU_ITEM.VOU_SID
								LEFT JOIN RPS.STORE						ON STORE.SID = VOU.STORE_SID
								LEFT JOIN RPS.STORE STORE_FROM			ON STORE_FROM.SID = VOU.ORIG_STORE_SID
								LEFT JOIN RPS.SUBSIDIARY SUBS			ON SUBS.SID = VOU.SBS_SID
								LEFT JOIN RPS.COUNTRY					ON COUNTRY.SID = SUBS.COUNTRY_SID
								LEFT JOIN RPS.REGION_SUBSIDIARY			ON SUBS.SID = REGION_SUBSIDIARY.SBS_SID
								LEFT JOIN RPS.REGION					ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
								LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VOU_ITEM.ITEM_SID
								INNER JOIN RPS.EMPLOYEE					ON SUBS.SID = EMPLOYEE.SBS_SID AND VOU.CLERK_SID = RPS.EMPLOYEE.SID
								LEFT JOIN RPS.CURRENCY					ON SUBS.BASE_CURRENCY_SID = CURRENCY.SID
								LEFT JOIN RPS.PREF_REASON VOU_REASON	ON VOU.VOU_REASON_SID = VOU_REASON.SID
								WHERE
									TRUNC(VOU.CREATED_DATETIME) BETWEEN DATE '2025-08-01' AND DATE '2025-08-31'
									AND VOU.VOU_TYPE = 0 
									AND VOU.SLIP_FLAG = 1 
									AND VOU.STATUS = 4
								ORDER BY 
									VOU.CREATED_DATETIME DESC
							FETCH FIRST 1 ROWS ONLY
					";

					var parameters = new
					{
						SaleDate = date.Date,
						VoucherTypes = vouType,
						VoucherClass = vouClass
					};

					var sales = await connection.QueryAsync<TransferModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching ASN - Receiving data: {ex.Message}");
					Console.WriteLine($"Error fetching ASN - Receiving data: {ex.Message}");
					return new List<TransferModel>();
				}
			}
		}

	}
}
