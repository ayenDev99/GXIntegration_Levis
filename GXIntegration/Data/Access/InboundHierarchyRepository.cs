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
	public class InboundHierarchyRepository
	{
		private readonly string _connectionString;
		public InboundHierarchyRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<List<dynamic>> GetUdfDetailsAsync(string udfNo, string udfOption, string sbsSid)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
						SELECT 
							SBS.SBS_NAME,
							SBS.SID,
							U.UDF_NO,
							U.SID AS UDF_SID,
							O.UDF_OPTION
						FROM 
							RPS.INVN_UDF U
						LEFT JOIN RPS.SUBSIDIARY SBS ON SBS.SID = U.SBS_SID
						LEFT JOIN RPS.INVN_UDF_OPTION O ON U.SID = O.UDF_SID
						WHERE 
							U.UDF_NO = :UdfNo
							AND O.UDF_OPTION = :UdfOption
							AND SBS.SID = :SbsSid
					";

					var result = await connection.QueryAsync(sql, new
					{
						UdfNo = udfNo,
						UdfOption = udfOption,
						SbsSid = sbsSid
					});

					

					return result.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching UDF details: {ex.Message}");
					Console.WriteLine($"Error fetching UDF details: {ex.Message}");
					return new List<dynamic>();
				}
			}
		}

		public async Task<List<dynamic>> GetInvnUdfSidAsync(string udfNo, string sbsSid)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					string sql = @"
						SELECT 
							U.SID,
							COUNT(DISTINCT O.UDF_OPTION) AS OptionCount
						FROM 
							RPS.INVN_UDF U
						LEFT JOIN RPS.SUBSIDIARY SBS ON SBS.SID = U.SBS_SID
						LEFT JOIN RPS.INVN_UDF_OPTION O ON U.SID = O.UDF_SID
						WHERE 
							U.UDF_NO = :UdfNo
							AND SBS.SID = :SbsSid
						GROUP BY U.SID
					";

					//Console.WriteLine(sql);
					//Console.WriteLine(udfNo);
					//Console.WriteLine(sbsSid);

					var result = await connection.QueryAsync(sql, new
					{
						UdfNo = udfNo,
						SbsSid = sbsSid
					});



					return result.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching UDF details: {ex.Message}");
					Console.WriteLine($"Error fetching UDF details: {ex.Message}");
					return new List<dynamic>();
				}
			}
		}


		public async Task<List<dynamic>> GetSbsListAsync()
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT 
							SID
							, SBS_NAME
						FROM 
							RPS.SUBSIDIARY
					";

					var result = await connection.QueryAsync(sql);

					return result.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching SBS list: {ex.Message}");
					Console.WriteLine($"Error fetching SBS list: {ex.Message}");
					return new List<dynamic>();
				}
			}
		}



		public async Task<int> CountUdfOptionAsync(string udfNos)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT 
							COUNT(O.UDF_OPTION) AS TotalUdfOptionCount
						FROM 
							RPS.INVN_UDF U
						LEFT JOIN RPS.INVN_UDF_OPTION O ON U.SID = O.UDF_SID
						WHERE 
							U.UDF_NO IN :UdfNos";

					var result = await connection.QuerySingleAsync<int>(sql, new { UdfNos = udfNos });

					return result;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching UDF option count for {string.Join(",", udfNos)}: {ex.Message}");
					Console.WriteLine($"Error fetching UDF option count for {string.Join(",", udfNos)}: {ex.Message}");
					return 0;
				}
			}
		}

	}
}
