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
	public class ASNRepository
	{
		private readonly string _connectionString;
		public ASNRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<ASNModel>> GetASNAsync(DateTime date, List<int> vouType, List<int> vouClass)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
							SELECT
								'1'						AS ORG_ID
								, S.ADDRESS5			AS STORE_ID
								, VOU.VOU_NO			AS SEQ_NO
								, VOU.WORKSTATION
								, ''					AS TILL_ID
								, VOU.CREATED_DATETIME	AS BUS_DATE
								, VOU.CREATED_DATETIME	AS BEG_TIME
								, VOU.CREATED_DATETIME	AS END_TIME
								, VOU.CLERK_SID			AS OPT_ID
								, C.ALPHABETIC_CODE		AS CURR_CODE
								, REGION.REGION_NAME	AS P2_VAL
								, COUNTRY.COUNTRY_CODE	AS P3_VAL
								, S.ADDRESS5			AS P4_VAL
								, CASE WHEN VOU.STATUS = 4 THEN 'CLOSED' ELSE 'PENDING' END AS DOC_STATUS
								, VOU.VOU_NO			AS DOC_ID
								, 'RECEIVING_ASN'		AS DOC_TYPE_DESC
								, 'RECEIVING'			AS DOC_TYPE
								, 'ASN'					AS DOC_SUBTYPE
								, VOU.MODIFIED_DATETIME AS COM_TIMESTAMP
								, VOU.MODIFIED_DATETIME AS LAST_ACT_TIMESTAMP
								, ''					AS SHP_SEQ
								, ''					AS SHP_LOC
								, ''					AS SHP_STAT
								, VI.CARTON_NO
								, VI.CARTON_STATUS
								, VI.ITEM_POS			AS LINE_NO
								, DESCRIPTION1			AS ITEM_ID
								, VI.QTY				AS ACT_COUNT
								, VI.QTY				AS EXP_COUNT
								, VI.QTY				AS POST_COUNT
								, VOU.CREATED_DATETIME	AS BUS_DATE2
								, ''					AS TRANS_SEQ
								, ''					AS LINE_SEQ
								, ''					AS REC_CRE
								, ''					AS REC_STAT
								, VI.QTY				AS QTY_ORD
								, VI.QTY				AS QTY_REC
							FROM
								RPS.VOUCHER VOU
							LEFT JOIN RPS.VOU_ITEM VI		ON VOU.SID = VI.VOU_SID
							LEFT JOIN RPS.STORE	S			ON S.SID = VOU.STORE_SID
							LEFT JOIN RPS.SUBSIDIARY SBS	ON SBS.SID = VOU.SBS_SID
							LEFT JOIN RPS.COUNTRY			ON COUNTRY.SID = SBS.COUNTRY_SID
							LEFT JOIN RPS.REGION_SUBSIDIARY ON SBS.SID = REGION_SUBSIDIARY.SBS_SID
							LEFT JOIN RPS.REGION			ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
							LEFT JOIN RPS.INVN_SBS_ITEM ISB ON ISB.SID = VI.ITEM_SID
							INNER JOIN RPS.EMPLOYEE			ON SBS.SID = EMPLOYEE.SBS_SID AND VOU.CLERK_SID = EMPLOYEE.SID
							LEFT JOIN RPS.CURRENCY C		ON SBS.BASE_CURRENCY_SID = C.SID
							WHERE
								TRUNC(VOU.CREATED_DATETIME) BETWEEN DATE '2020-08-01' AND DATE '2025-08-07'
								AND VOU.VOU_TYPE = 0
								AND VOU.VOU_CLASS = 2
							--  AND VOU.STATUS = 4
							FETCH FIRST 1 ROWS ONLY
					";
					return (await connection.QueryAsync<ASNModel>(sql, new { SaleDate = date })).ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching sales data: {ex.Message}");
					Console.WriteLine($"Error fetching sakes data: {ex.Message}");
					return new List<ASNModel>();
				}
			}
		}



	}
}
