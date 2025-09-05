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
	
	}
}
