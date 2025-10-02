using Dapper;
using GXIntegration_Levis.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class StoreReceivingRepository
	{
		private readonly string _connectionString;
		public StoreReceivingRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreReceivingModel>> GetStoreReceivingAsync(DateTime from_date, DateTime to_date, string storeCode, string processType)
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

					string sql = $@"
							SELECT
								'1'										    AS ORGANIZATIONID
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS RETAILSTOREID
								, VOU.WORKSTATION						    AS WORKSTATIONID
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)
										|| VOU.WORKSTATION					AS TILLID
								, VOU.VOU_NO							    AS SEQUENCENO
								, TRUNC(VOU.CREATED_DATETIME)				AS BUSINESSDAYDATE
								, VOU.CREATED_DATETIME						AS BEGINDATETIME
								, VOU.POST_DATE								AS ENDDATETIME
								, EMPLOYEE.EMPL_NAME					    AS OPERATORID
								, C.ALPHABETIC_CODE						    AS CURRENCYCODE
								, 'true'									AS INVENTORYMOVEMENTSUCCESS
								, 'AMA'										AS REGION
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS ALTERNATESTOREID
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS DESTINATIONALTERNATESTOREID
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS ORIGINALTERNATESTOREID
								, 'CLOSED'									AS DOCUMENTSTATUS
								, VOU.PKG_NO 								AS DOCUMENTID
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS ORIGINATORID
								, (SELECT STORE_NAME FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS ORIGINATORNAME
								, 'SHIPPING_STORE_TRANSFER'					AS DOCUMENTTYPEDESCRIPTION
								, 'SHIPPING'							    AS DOCUMENTTYPE
								, 'STORE_TRANSFER'							AS DOCUMENTSUBTYPE
								, 'STORE'								    AS RECORDCREATIONTYPE
								, VOU.CREATED_DATETIME						AS CREATIONTIMESTAMP
								, VOU.MODIFIED_DATETIME						AS COMPLETIONTIMESTAMP
								, VOU.MODIFIED_DATETIME						AS LASTACTIVITYTIMESTAMP
								, '1'										AS SHIPMENTSEQUENCE
								, VOU.CREATED_DATETIME						AS ACTUALDELIVERYDATE                                
								, VOU.CREATED_DATETIME						AS ACTUALSHIPDATE                                
								, (SELECT ADDRESS4 FROM RPS.STORE
										WHERE SID = VOU.STORE_SID)			AS DESTINATIONRETAILLOCATIONID
								, ''										AS SHIPPINGCARRIER
								, VOU.TRACKING_NO							AS TRACKINGNUMBER
								, 'SHIPPED'									AS STATUSCODE
								, ''										AS POSTALCODE
								, VI.ITEM_POS								AS LINENUMBER
								, ISB.DESCRIPTION1							AS ITEMID
								, VI.QTY									AS ActualCount
								, VI.ORIG_QTY								AS ExpectedCount
								, VI.QTY									AS PostedCount
								, VI.ORIG_QTY								AS QuantityOrdered
								, VI.QTY									AS QuantityReceived
								, ''										AS CartonNumber
								, ISB.DESCRIPTION2							AS DESCRIPTION
								, ISB.ITEM_SIZE								AS PTDIM1
								, ISB.ATTRIBUTE								AS PTDIM2
								, ISB.DESCRIPTION1							AS PTSTYLE
								, VOU.VOU_NO								AS PTCONTROLNUMBER
								, 'PHP'										AS COUNTRY
								, ISB.UPC									AS PTEAN
								FROM
									RPS.VOUCHER VOU
								LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
								LEFT JOIN RPS.STORE	S					ON S.SID = VOU.STORE_SID
								LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
								LEFT JOIN RPS.COUNTRY					ON COUNTRY.SID = SBS.COUNTRY_SID
								LEFT JOIN RPS.REGION_SUBSIDIARY			ON SBS.SID = REGION_SUBSIDIARY.SBS_SID
								LEFT JOIN RPS.REGION					ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
								LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VI.ITEM_SID
								LEFT JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND VOU.CLERK_SID = EMPLOYEE.SID
								LEFT JOIN RPS.CURRENCY C				ON SBS.BASE_CURRENCY_SID = C.SID
								LEFT JOIN RPS.PREF_REASON VOU_REASON	ON VOU.VOU_REASON_SID = VOU_REASON.SID
								WHERE
									{dateCondition}
									AND VOU.SLIP_FLAG = 1
									AND VOU.VOU_TYPE = 0
									AND VOU.VOU_CLASS = 0
									AND VOU.STATUS = 4						
									AND VOU.STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)
								ORDER BY 
									VOU.POST_DATE DESC
					";

					//Logger.Log($"Generated SQL: {sql}");

					var parameters = new
					{
						FromDate = from_date,
						ToDate = to_date,
						StoreCode = storeCode
					};

					var sales = await connection.QueryAsync<StoreReceivingModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store_Receiving data: {ex.Message}");
					Console.WriteLine($"Error fetching Store_Receiving data: {ex.Message}");
					return new List<StoreReceivingModel>();
				}
			}
		}

	}
}
