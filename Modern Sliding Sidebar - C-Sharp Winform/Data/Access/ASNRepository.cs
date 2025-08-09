using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class ASNRepository
	{
		private readonly string _connectionString;
		public ASNRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		//public async Task<List<ASNModel>> GetANSAsync(DateTime date)
		//{
		//	using (var connection = new OracleConnection(_connectionString))
		//	{
		//		try
		//		{
		//			await connection.OpenAsync();
		//			string sql = @"
		//					SELECT
		//						'1'						AS ORG_ID
		//						, STORE.ADDRESS5		AS STORE_ID
		//						, VOU.VOU_NO			AS SEQ_NO
		//						, VOU.WORKSTATION
		//						, ''					AS TILL_ID
		//						, VOU.CREATED_DATETIME	AS BUS_DATE
		//						, VOU.CREATED_DATETIME	AS BEG_TIME
		//						, VOU.CREATED_DATETIME	AS END_TIME
		//						, VOU.CLERK_SID			AS OPT_ID
		//						, CURRENCY.ALPHABETIC_CODE AS CURR_CODE
		//						, 'INVENTORY_MOVEMENT_SUCCESS' AS P1_CODE
		//						, 'TRUE' AS P1_VAL
		//						, 'REGION' AS P2_CODE
		//						, REGION.REGION_NAME AS P2_VAL
		//						, 'COUNTRY' AS P3_CODE
		//						, COUNTRY.COUNTRY_CODE AS P3_VAL
		//						, 'ALTERNATE_STOREID' AS P4_CODE
		//						, STORE.ADDRESS5 AS P4_VAL
		//						, CASE WHEN VOU.STATUS = 4 THEN 'CLOSED' ELSE 'PENDING' END AS DOC_STATUS
		//						, VOU.VOU_NO AS DOC_ID
		//						, 'RECEIVING_ASN' AS DOC_TYPE_DESC
		//						, 'RECEIVING' AS DOC_TYPE
		//						, 'ASN' AS DOC_SUBTYPE
		//						, VOU.MODIFIED_DATETIME AS COM_TIMESTAMP
		//						, VOU.MODIFIED_DATETIME AS LAST_ACT_TIMESTAMP
		//						, '' AS SHP_SEQ
		//						, '' AS SHP_LOC
		//						, '' AS SHP_STAT
		//						, VOU_ITEM.CARTON_NO
		//						, VOU_ITEM.CARTON_STATUS
		//						, VOU_ITEM.ITEM_POS AS LINE_NO
		//						, ISB.DESCRIPTION1 AS ITEM_ID
		//						, VOU_ITEM.QTY AS ACT_COUNT
		//						, VOU_ITEM.QTY AS EXP_COUNT
		//						, VOU_ITEM.QTY AS POST_COUNT
		//						, VOU.CREATED_DATETIME AS BUS_DATE2
		//						, '' AS TRANS_SEQ
		//						, '' AS LINE_SEQ
		//						, '' AS REC_CRE
		//						, '' AS REC_STAT
		//						, VOU_ITEM.QTY AS QTY_ORD
		//						, VOU_ITEM.QTY AS QTY_REC
		//					FROM
		//						RPS.VOUCHER VOU
		//					LEFT JOIN RPS.VOU_ITEM 				ON VOU.SID = RPS.VOU_ITEM.VOU_SID
		//					LEFT JOIN RPS.STORE					ON RPS.STORE.SID = VOU.STORE_SID
		//					LEFT JOIN RPS.SUBSIDIARY SUBS		ON SUBS.SID = VOU.SBS_SID
		//					LEFT JOIN RPS.COUNTRY				ON RPS.COUNTRY.SID = SUBS.COUNTRY_SID
		//					LEFT JOIN RPS.REGION_SUBSIDIARY		ON SUBS.SID = RPS.REGION_SUBSIDIARY.SBS_SID
		//					LEFT JOIN RPS.REGION				ON RPS.REGION.SID = RPS.REGION_SUBSIDIARY.REGION_SID
		//					LEFT JOIN RPS.INVN_SBS_ITEM ISB		ON ISB.SID = RPS.VOU_ITEM.ITEM_SID
		//					INNER JOIN RPS.EMPLOYEE				ON SUBS.SID = RPS.EMPLOYEE.SBS_SID AND VOU.CLERK_SID = RPS.EMPLOYEE.SID
		//					LEFT JOIN RPS.CURRENCY CURRENCY     ON SUBS.BASE_CURRENCY_SID = RPS.CURRENCY.SID
		//					WHERE 
		//						TRUNC(VOU.CREATED_DATETIME) BETWEEN 
		//							DATE '2025-08-01' AND 
		//							DATE '2025-08-08'
		//			";
		//			return (await connection.QueryAsync<ASNModel>(sql, new { SaleDate = date })).ToList();
		//		}
		//		catch (Exception ex)
		//		{
		//			Logger.Log($"Error fetching sales data: {ex.Message}");
		//			Console.WriteLine($"Error fetching sakes data: {ex.Message}");
		//			return new List<ASNModel>();
		//		}
		//	}
		//}



	}
}
