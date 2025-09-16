using Dapper;
using GXIntegration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GXIntegration_Levis.Model;
using System.Threading.Tasks;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Data.Access
{
	public class InventoryRepository
	{
		private readonly string _connectionString;

		public InventoryRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<InventoryModel>> GetInventoryAsync(DateTime date)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{

					await connection.OpenAsync();

					string sql = @"
						SELECT 
							'PHP'						        AS CurrencyId
							, S.ACTIVE_PRICE_LVL_SID
							, TO_CHAR(S.ADDRESS4)				AS StoreCode
							, ISI.DESCRIPTION1					AS ProductCode
							, ISI.ALU							AS Sku
							, ISI.ITEM_SIZE						AS Waist
							, ISI.ATTRIBUTE						AS Inseam
							, LM.DT				              	AS LastMovementDate
							, CASE 
								WHEN ISIQ.QTY >= 0 THEN 'P'
								WHEN ISIQ.QTY < 0 THEN 'N'
							  END AS QuantitySign
							, ISIQ.QTY							AS Quantity
							, ISP.PRICE							AS RetailPrice
							, SUBSTR(CT.COUNTRY_CODE, 1, 2)		AS CountryCode
							, ISI.UPC							AS ManufactureUpc
							, ISI.UDF5_STRING					AS Division
							, ISI.SID
						FROM rps.INVN_SBS_ITEM ISI
						LEFT JOIN rps.INVN_SBS_ITEM_QTY ISIQ ON ISIQ.INVN_SBS_ITEM_SID = ISI.SID
						LEFT JOIN rps.STORE S ON S.SID = ISIQ.STORE_SID
						LEFT JOIN rps.CURRENCY C ON C.SID = ISI.CURRENCY_SID
						LEFT JOIN rps.SUBSIDIARY SBS ON SBS.SID = ISI.SBS_SID
						LEFT JOIN rps.COUNTRY CT ON CT.SID = SBS.COUNTRY_SID
						LEFT JOIN RPS.INVN_SBS_PRICE ISP ON ISP.INVN_SBS_ITEM_SID = ISI.SID
						LEFT JOIN
						  (SELECT ITEM_SID, DT , SBS_SID, STORE_SID
							FROM ( SELECT ITEM_SID,DT,SBS_SID, STORE_SID,ROW_NUMBER() OVER (PARTITION BY ITEM_SID ORDER BY DT DESC) RN
								FROM 
								  ( SELECT AA.INVN_SBS_ITEM_SID AS ITEM_SID,AAH.CREATED_DATETIME AS DT,AAH.SUBSIDIARY_SID AS SBS_SID,AAH.STORE_SID FROM RPS.DOCUMENT_ITEM AA LEFT JOIN RPS.DOCUMENT AAH ON AA.DOC_SID = AAH.SID
									UNION ALL
									SELECT BB.ITEM_SID AS ITEM_SID,BBH.CREATED_DATETIME AS DT,BBH.SBS_SID, BBH.STORE_SID FROM RPS.VOU_ITEM BB LEFT JOIN RPS.VOUCHER BBH ON BB.VOU_SID = BBH.SID
									UNION ALL
									SELECT CC.ITEM_SID, CCH.CREATED_DATETIME AS DT,CCH.OUT_SBS_SID AS SBS_SID,CCH.OUT_STORE_SID AS STORE_SID FROM RPS.SLIP_ITEM CC LEFT JOIN RPS.SLIP CCH ON CC.SLIP_SID = CCH.SID
									UNION ALL
									SELECT DD.ITEM_SID, DDH.CREATED_DATETIME AS DT,DDH.SBS_SID, DDH.STORE_SID FROM RPS.PO_ITEM DD LEFT JOIN RPS.PO DDH ON DD.PO_SID = DDH.SID
									UNION ALL
									SELECT EE.ITEM_SID, EEH.CREATED_DATETIME AS DT,EEH.SBS_SID,EEH.STORE_SID FROM RPS.ADJ_ITEM EE LEFT JOIN RPS.ADJUSTMENT EEH ON EE.ADJ_SID = EEH.SID)
								WHERE DT IS NOT NULL
							)
							WHERE RN = 1 
							) LM
						ON ISI.SID = LM.ITEM_SID AND ISIQ.STORE_SID = LM.STORE_SID 
						WHERE 
						ISI.active = 1
							AND S.ACTIVE = 1
							AND TRUNC(ISI.POST_DATE) BETWEEN DATE '2025-07-01' AND DATE '2025-09-30'
					";

					//TRUNC(ISI.post_date) BETWEEN
					//		TO_DATE('01-MAR-25', 'DD-MON-YY') AND
					//		TO_DATE('31-MAR-25', 'DD-MON-YY')


					return (await connection.QueryAsync<InventoryModel>(sql, new { CreatedDate = date })).ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching inventory data: {ex.Message}");
					Console.WriteLine($"Error fetching inventory data: {ex.Message}");
					return new List<InventoryModel>();
				}
			}
		}
	}
}
