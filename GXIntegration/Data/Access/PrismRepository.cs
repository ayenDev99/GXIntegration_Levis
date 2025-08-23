using Dapper;
using GXIntegration_Levis.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class PrismRepository
	{
		private readonly string _connectionString;
		public PrismRepository(string connectionString)
		{
			_connectionString = connectionString;
		}
		public async Task<string> GetRpsJobSid(string jobTitle)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT 
							JOB.SID
						FROM 
							RPS.JOB JOB
						WHERE 
							JOB.JOB_NAME = :JobTitle
					";

					var sid = await connection.QueryFirstOrDefaultAsync<string>(sql, new
					{
						JobTitle = jobTitle
					});

					return sid;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS job SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS job SID: {ex.Message}");
					return null;
				}
			}
		}

		public async Task<dynamic> GetRpsStore(string storeCode)
		{
			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = @"
						SELECT 
							*
						FROM 
							RPS.STORE STORE
						WHERE 
							STORE.ADDRESS5 = :StoreCode
					";

					var result = await connection.QueryFirstOrDefaultAsync(sql, new
					{
						StoreCode = storeCode
					});

					return result;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS job SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS job SID: {ex.Message}");
					return null;
				}
			}
		}


	}
}
