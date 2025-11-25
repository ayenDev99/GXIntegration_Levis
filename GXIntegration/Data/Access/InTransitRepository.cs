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
	public class InTransitRepository
	{
		private readonly string _connectionString;
		public InTransitRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<InTransitModel>> GetIntransitAsync(DateTime procDate)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
					    SELECT 
                            SourceType
                            , ProductCode
                            , Sku
                            , Waist
                            , Inseam
                            , StoreCode
                            , SUM(Quantity)                     AS TotalQuantity
                            , MAX(PostDate)                     AS LastPostDate
                            , MAX(ModifiedDatetime)             AS LastModifiedDatetime
                        FROM (
                            -- ==============================
                            -- PO Source
                            -- ==============================
                            SELECT 
                                'PO'                            AS SourceType
                                , CASE 
                                    WHEN SUBSTR(ISI.DESCRIPTION1, -1) = '0' 
                                    THEN SUBSTR(ISI.DESCRIPTION1, 1, LENGTH(ISI.DESCRIPTION1) - 1)
                                    ELSE ISI.DESCRIPTION1
                                END                             AS ProductCode
                                , REPLACE(ISI.ALU, '-', '')     AS Sku
                                , ISI.ITEM_SIZE                 AS Waist
                                , ISI.ATTRIBUTE                 AS Inseam
                                , TO_CHAR(STORE.ADDRESS4)       AS StoreCode
                                , PO_ITEM.ORD_QTY               AS Quantity
                                , PO.POST_DATE                  AS PostDate
                                , PO.MODIFIED_DATETIME          AS ModifiedDatetime
                            FROM 
                                RPS.PO
                            LEFT JOIN RPS.PO_ITEM PO_ITEM       ON PO.SID = PO_ITEM.PO_SID
                            LEFT JOIN RPS.STORE STORE           ON STORE.SID = PO.STORE_SID
                            LEFT JOIN RPS.INVN_SBS_ITEM ISI     ON ISI.SID = PO_ITEM.ITEM_SID
                            WHERE 
                                ISI.ACTIVE = 1
                                AND STORE.ACTIVE = 1
                                AND STORE.ADDRESS4 IS NOT NULL
                                AND PO.PO_NO NOT IN (
                                    SELECT 
                                        VOU.PO_NO 
                                    FROM 
                                        RPS.VOUCHER VOU 
                                    WHERE 
                                        VOU.PO_NO IS NOT NULL
                                        AND VOU.STATUS = 4
                                )
                            -- AND TRUNC(PO.POST_DATE) <= :ProcDate

                            UNION ALL

                            -- ==============================
                            -- VOUCHER Source
                            -- ==============================
                            SELECT 
                                'VOUCHER'                       AS SourceType
                               , CASE 
                                    WHEN SUBSTR(ISI.DESCRIPTION1, -1) = '0' 
                                    THEN SUBSTR(ISI.DESCRIPTION1, 1, LENGTH(ISI.DESCRIPTION1) - 1)
                                    ELSE ISI.DESCRIPTION1
                                END                             AS ProductCode
                                , REPLACE(ISI.ALU, '-', '')     AS Sku
                                , ISI.ITEM_SIZE                 AS Waist
                                , ISI.ATTRIBUTE                 AS Inseam
                                , TO_CHAR(STORE.ADDRESS4)       AS StoreCode
                                , VOU_ITEM.QTY                  AS Quantity
                                , VOU.POST_DATE                 AS PostDate
                                , VOU.MODIFIED_DATETIME         AS ModifiedDatetime
                            FROM 
                                RPS.VOUCHER VOU
                            LEFT JOIN RPS.VOU_ITEM VOU_ITEM     ON VOU.SID = VOU_ITEM.VOU_SID
                            LEFT JOIN RPS.STORE STORE           ON STORE.SID = VOU.STORE_SID
                            LEFT JOIN RPS.INVN_SBS_ITEM ISI     ON ISI.SID = VOU_ITEM.ITEM_SID
                            WHERE 
                                VOU.SLIP_FLAG = 1 
                                AND VOU.STATUS = 3
                                AND ISI.ACTIVE = 1
                                AND STORE.ACTIVE = 1
                                AND STORE.ADDRESS4 IS NOT NULL
                            -- AND TRUNC(VOU.POST_DATE) <= :ProcDate
                        ) Combined
                        GROUP BY 
                            SourceType
                            , ProductCode
                            , Sku
                            , Waist
                            , Inseam
                            , StoreCode
                        ORDER BY 
                            LastPostDate DESC
                            , LastModifiedDatetime DESC
					";

					//AND TRUNC(VOU.POST_DATE) <= :ToDate
					//VOU.POST_DATE BETWEEN :FromDate AND :ToDate
					//TRUNC(VOU.POST_DATE) BETWEEN DATE '2025-07-20' AND DATE '2025-09-16'

					var parameters = new
					{
						ProcDate = procDate,
					};

					var sales = await connection.QueryAsync<InTransitModel>(sql, parameters);
					return sales.ToList();
				}
				catch (Exception ex)
				{
					Logger.LogError($"Error fetching INTRANSIT data: {ex.Message}");
					Console.WriteLine($"Error fetching INTRANSIT data: {ex.Message}");
					return new List<InTransitModel>();
				}
			}
		}

	}
}
