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
	public class StoreShippingRepository
	{
		private readonly string _connectionString;
		public StoreShippingRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<StoreShippingModel>> GetStoreShippingAsync(DateTime from_date, DateTime to_date, string storeCode, string processType)
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
							'1'										            AS ORGANIZATIONID
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.OUT_STORE_SID)				AS RETAILSTOREID
							, SLIP.WORKSTATION						            AS WORKSTATIONID
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.OUT_STORE_SID) 
									|| SLIP.WORKSTATION							AS TILLID
							, SLIP.SLIP_NO							            AS SEQUENCENO
							, TRUNC(SLIP.CREATED_DATETIME)						AS BUSINESSDAYDATE
							, SLIP.CREATED_DATETIME								AS BEGINDATETIME
							, SLIP.POST_DATE							        AS ENDDATETIME
							, EMPLOYEE.EMPL_NAME					            AS OPERATORID
							, C.ALPHABETIC_CODE						            AS CURRENCYCODE
							, 'true'											AS INVENTORYMOVEMENTSUCCESS
							, 'AMA'												AS REGION
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.OUT_STORE_SID)				AS ALTERNATESTOREID
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.IN_STORE_SID)				AS DESTINATIONALTERNATESTOREID
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.OUT_STORE_SID)				AS ORIGINALTERNATESTOREID
							, 'CLOSED'											AS DOCUMENTSTATUS
							, VOU.PKG_NO  										AS DOCUMENTID
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.OUT_STORE_SID)				AS ORIGINATORID
							, (SELECT STORE_NAME FROM RPS.STORE 
									WHERE SID = SLIP.OUT_STORE_SID)				AS ORIGINATORNAME
							, 'SHIPPING_STORE_TRANSFER'							AS DOCUMENTTYPEDESCRIPTION
							, 'SHIPPING'							            AS DOCUMENTTYPE
							, 'STORE_TRANSFER'									AS DOCUMENTSUBTYPE
							, 'STORE'								            AS RECORDCREATIONTYPE
							, SLIP.CREATED_DATETIME								AS CREATIONTIMESTAMP
							, SLIP.MODIFIED_DATETIME							AS COMPLETIONTIMESTAMP
							, SLIP.MODIFIED_DATETIME							AS LASTACTIVITYTIMESTAMP
							, '1'												AS SHIPMENTSEQUENCE
							, SLIP.CREATED_DATETIME								AS ACTUALDELIVERYDATE                                
							, SLIP.CREATED_DATETIME								AS ACTUALSHIPDATE                                
							, (SELECT ADDRESS4 FROM RPS.STORE 
									WHERE SID = SLIP.IN_STORE_SID)				AS DESTINATIONRETAILLOCATIONID
							, ''												AS SHIPPINGCARRIER
							, SLIP.TRACKING_NO									AS TRACKINGNUMBER
							, 'SHIPPED'											AS STATUSCODE
							, ''												AS POSTALCODE
							, 'PHP'												AS COUNTRY
							, ISB.DESCRIPTION1									AS ITEMID
							, ISB.ITEM_SIZE										AS PTDIM1
							, ISB.ATTRIBUTE										AS PTDIM2
							, ISB.DESCRIPTION1									AS PTSTYLE
							, SLIP.SLIP_NO										AS PTCONTROLNUMBER
							, ISB.UPC											AS PTEAN
							, VI.QTY 											AS QUANTITYSHIPPED
							, VI.ITEM_POS										AS LINENUMBER
							, ISB.DESCRIPTION2									AS DESCRIPTION
						FROM
							RPS.VOUCHER VOU
						LEFT JOIN RPS.VOU_ITEM VI				ON VOU.SID = VI.VOU_SID
						LEFT JOIN RPS.SLIP                      ON SLIP.VOU_SID = VOU.SID
						LEFT JOIN RPS.STORE	S					ON S.SID = SLIP.OUT_STORE_SID
						LEFT JOIN RPS.SUBSIDIARY SBS			ON SBS.SID = VOU.SBS_SID
						LEFT JOIN RPS.COUNTRY					ON COUNTRY.SID = SBS.COUNTRY_SID
						LEFT JOIN RPS.REGION_SUBSIDIARY			ON SBS.SID = REGION_SUBSIDIARY.SBS_SID
						LEFT JOIN RPS.REGION					ON REGION.SID = REGION_SUBSIDIARY.REGION_SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISB			ON ISB.SID = VI.ITEM_SID
						LEFT JOIN RPS.EMPLOYEE					ON SBS.SID = EMPLOYEE.SBS_SID AND SLIP.CLERK_SID = EMPLOYEE.SID
						LEFT JOIN RPS.CURRENCY C				ON SBS.BASE_CURRENCY_SID = C.SID
						LEFT JOIN RPS.PREF_REASON VOU_REASON	ON SLIP.TRANS_REASON_SID = VOU_REASON.SID
						WHERE
							{dateCondition}
                            AND SLIP.SLIP_NO IS NOT NULL
                            AND VOU.VOU_TYPE = 0
                            AND SLIP.STATUS = 4
							AND SLIP.OUT_STORE_SID IN (SELECT SID FROM RPS.STORE WHERE ADDRESS4 = :StoreCode)
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

					var sales = await connection.QueryAsync<StoreShippingModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching Store Shipping data: {ex.Message}");
					Console.WriteLine($"Error fetching Store Shipping data: {ex.Message}");
					return new List<StoreShippingModel>();
				}
			}
		}

	}
}
