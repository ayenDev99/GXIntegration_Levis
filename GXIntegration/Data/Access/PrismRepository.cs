using Dapper;
using GXIntegration_Levis.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
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

		public async Task<dynamic> GetRpsStore(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> {"ADDRESS5", "ACTIVE"};

			if (!allowedColumns.Contains(columnName.ToUpper()))
				throw new ArgumentException("Invalid column name");

			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = $@"
						SELECT 
							*
						FROM 
							RPS.STORE STORE
						WHERE 
							STORE.{columnName} = :ColumnValue
					";

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, new { ColumnValue = columnValue });
					return results;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS job SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS job SID: {ex.Message}");
					return null;
				}
			}
		}

		public async Task<dynamic> GetRpsInvnSbsItem(string upc)
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
							RPS.INVN_SBS_ITEM ISI
						WHERE 
							ISI.UPC = :Upc
					";

					var result = await connection.QueryFirstOrDefaultAsync(sql, new
					{
						Upc = upc
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

		public async Task<dynamic> GetRpsEmployee(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "USER_NAME"};

			if (!allowedColumns.Contains(columnName.ToUpper()))
				throw new ArgumentException("Invalid column name");

			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					string sql = $@"
						SELECT 
							*
						FROM 
							RPS.EMPLOYEE 
						WHERE 
							{columnName} = :ColumnValue
					";

					Logger.Log(columnName);
					Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, new { ColumnValue = columnValue });
					return results;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS EMPLOYEE SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS EMPLOYEE SID: {ex.Message}");
					return null;
				}
			}
		}

	}
}
