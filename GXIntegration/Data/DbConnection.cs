using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


namespace GXIntegration_Levis.Data
{
	public static class DbConnection
	{
		public static IDbConnection CreateConnection()
		{
			// Get connection string from App.config
			string connectionString = ConfigurationManager.ConnectionStrings["OracleDb"].ConnectionString;

			// Return a new OracleConnection
			return new OracleConnection(connectionString);
		}
	}
}
