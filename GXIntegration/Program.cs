using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace GXIntegration
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			//// 🔧 Create AppData folder inside bin\Debug or bin\Release
			//string folderPath = Path.Combine(Application.StartupPath, "AppData");

			//// Make sure the directory exists
			//if (!Directory.Exists(folderPath))
			//{
			//	Directory.CreateDirectory(folderPath);
			//}

			//// 🗃️ Full path to the SQLite database
			//string dbPath = Path.Combine(folderPath, "APISender.db");

			//// Create the database file if it doesn't exist
			//if (!File.Exists(dbPath))
			//{
			//	SQLiteConnection.CreateFile(dbPath);
			//	Console.WriteLine($"✅ Database created at: {dbPath}");
			//}

			//// Connect and create table
			//string connectionString = $"Data Source={dbPath};Version=3;";
			//using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			//{
			//	conn.Open();

			//	string createTableQuery = @"
   //                 CREATE TABLE IF NOT EXISTS APISender (
   //                     ID INTEGER PRIMARY KEY AUTOINCREMENT,
   //                     SID TEXT NOT NULL,
			//			TYPE TEXT,
			//			DATE TEXT,
			//			STATUS TEXT NOT NULL
   //                 );";

			//	using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
			//	{
			//		cmd.ExecuteNonQuery();
			//		Console.WriteLine("✅ Table created successfully.");
			//	}

			//	// Insert fake data if table is empty
			//	string countQuery = "SELECT COUNT(*) FROM APISender;";
			//	using (SQLiteCommand countCmd = new SQLiteCommand(countQuery, conn))
			//	{
			//		long count = (long)countCmd.ExecuteScalar();
			//		if (count == 0)
			//		{
			//			string insertQuery = @"
			//				INSERT INTO APISender (SID, TYPE, DATE, STATUS) VALUES
			//				('SID001', 'storesale', '25-JUN-24 11.41.26.000000000 PM +08:00', 'Active'),
			//				('SID002', 'storeshipping', '25-JUN-24 11.41.26.000000000 PM +08:00', 'Inactive'),
			//				('SID003', 'storereturn', '25-JUN-24 11.41.26.000000000 PM +08:00', 'Pending');
			//			";

			//			using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, conn))
			//			{
			//				int rowsInserted = insertCmd.ExecuteNonQuery();
			//				Console.WriteLine($"✅ Inserted {rowsInserted} fake records into APISender.");
			//			}
			//		}
			//		else
			//		{
			//			Console.WriteLine("⚠️ Fake data already exists, skipping insert.");
			//		}
			//	}
			//}

			

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}


	}
	
}
