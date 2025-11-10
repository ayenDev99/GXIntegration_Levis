using Dapper;
using GXIntegration_Levis.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
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

		public async Task<List<dynamic>> GetRpsStore(string columnName, string columnValue)
		{
			var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"ADDRESS4",
				"ACTIVE"
			};

			if (!allowedColumns.Contains(columnName))
				throw new ArgumentException("Invalid column name");

			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();

					bool useLike = columnName.Equals("ADDRESS4", StringComparison.OrdinalIgnoreCase);

					string sql = $@"
									SELECT 
										*
									FROM 
										RPS.STORE STORE
									WHERE 
										STORE.ADDRESS4 IS NOT NULL
										AND STORE.{columnName} {(useLike ? "LIKE" : "=")} :ColumnValue
								";

					var parameter = useLike
						? new { ColumnValue = $"%{columnValue}%" }
						: new { ColumnValue = columnValue };

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, parameter);
					return results.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS store data: {ex.Message}");
					Console.WriteLine($"Error fetching RPS store data: {ex.Message}");
					return new List<dynamic>();
				}
			}
		}


		public async Task<dynamic> GetRpsInvnSbsItem(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "UPC", "DESCRIPTION1", "ALU" };

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
							RPS.INVN_SBS_ITEM 
						WHERE 
							{columnName} = :ColumnValue
					";

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, new { ColumnValue = columnValue });
					return results;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS INVN_SBS_ITEM SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS INVN_SBS_ITEM SID: {ex.Message}");
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

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

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

		public async Task<dynamic> GetRpsEmployeeExtend(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "EMPLOYEE_SID" };

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
							RPS.EMPLOYEE_EXTEND 
						WHERE 
							{columnName} = :ColumnValue
					";

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, new { ColumnValue = columnValue });
					return results;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS EMPLOYEE_EXTEND SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS EMPLOYEE_EXTEND SID: {ex.Message}");
					return null;
				}
			}
		}

		public async Task<dynamic> GetRpsSubsidiary(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "ACTIVE", "SBS_NO" };

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
							RPS.Subsidiary 
						WHERE 
							{columnName} = :ColumnValue
					";

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, new { ColumnValue = columnValue });
					return results;
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS Subsidiary SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS Subsidiary SID: {ex.Message}");
					return null;
				}
			}
		}

		public async Task<dynamic> GetRpsUserGroup(string columnName, string columnValue)
		{
			Logger.Log("test");
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "USER_GROUP_NAME" };

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
							RPS.USER_GROUP 
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
					Logger.Log($"Error fetching RPS USER_GROUP: {ex.Message}");
					Console.WriteLine($"Error fetching RPS USER_GROUP: {ex.Message}");
					return null;
				}
			}
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

		public async Task<List<dynamic>> GetRpsPO(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "PO_NO" };

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
							RPS.PO PO
						LEFT JOIN RPS.PO_ITEM POI ON POI.PO_SID = PO.SID
						LEFT JOIN RPS.INVN_SBS_ITEM ISI ON ISI.SID = POI.ITEM_SID
						WHERE 
							{columnName} = :ColumnValue
					";

					//Logger.Log(columnName);
					//Logger.Log(columnValue);

					var results = await connection.QueryAsync(sql, new { ColumnValue = columnValue });
					return results.ToList();
				}
				catch (Exception ex)
				{
					Logger.Log($"Error fetching RPS PO SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS PO SID: {ex.Message}");
					return new List<dynamic>();
				}
			}
		}

		public async Task<IEnumerable<dynamic>> GetInboundItemsAsync(Dictionary<string, object> filters)
		{
			// Whitelisted column names with their table aliases
			var allowedColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "DESCRIPTION1", "INV" },
				{ "ACTIVE", "INV" },
				{ "ATTRIBUTE", "INV" },
				{ "ITEM_SIZE", "INV" },
				{ "SBS_NO", "SBS" },
				{ "PRICE_LVL_NAME", "PL" },
				{ "SBS_SID", "INV" }
			};

			var whereClauses = new List<string>();
			var parameters = new DynamicParameters();

			foreach (var filter in filters)
			{
				if (!allowedColumns.ContainsKey(filter.Key))
					throw new ArgumentException($"Invalid filter column: {filter.Key}");

				string tableAlias = allowedColumns[filter.Key];
				string paramName = $"p_{filter.Key}";

				whereClauses.Add($"{tableAlias}.{filter.Key} = :{paramName}");
				parameters.Add(paramName, filter.Value);
			}

			string whereSql = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

			string sql = $@"
					SELECT 
						INV.DESCRIPTION1,
						INV.ACTIVE,
						SBS.SBS_NO,
						PL.PRICE_LVL_NAME,
						PL.SID AS ACTIVE_PRICE_LVL_SID,
						SBS.SID AS SBS_SID,
						INV.SID,
						INV.ITEM_SIZE,
						INV.ATTRIBUTE
					FROM 
						RPS.INVN_SBS_ITEM INV
					LEFT JOIN RPS.SUBSIDIARY SBS ON SBS.SID = INV.SBS_SID
					LEFT JOIN RPS.PRICE_LEVEL PL ON PL.SBS_SID = SBS.SID
					{whereSql}
				";

			using (var connection = new OracleConnection(_connectionString))
			{
				try
				{
					await connection.OpenAsync();
					return await connection.QueryAsync(sql, parameters);
				}
				catch (Exception ex)
				{
					Logger.Log($"[ERROR] Failed to query inbound items: {ex.Message}");
					return Enumerable.Empty<dynamic>();
				}
			}
		}

		public async Task<dynamic> GetRpsAdjustment(string columnName, string columnValue)
		{
			// Validate columnName to prevent SQL injection
			// Add column names to this list as needed
			var allowedColumns = new HashSet<string> { "SID" };

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
							RPS.ADJUSTMENT 
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
					Logger.Log($"Error fetching RPS ADJUSTMENT SID: {ex.Message}");
					Console.WriteLine($"Error fetching RPS ADJUSTMENT SID: {ex.Message}");
					return null;
				}
			}
		}
	}
}
