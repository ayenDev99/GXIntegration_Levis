using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using Oracle.ManagedDataAccess.Client;


namespace Modern_Sliding_Sidebar___C_Sharp_Winform
{
	public partial class Form1 : Form
    {

		AppConfig config;
		

		bool sideBar_Expand = true;
        public Form1()
        {
            InitializeComponent();
			config = AppConfig.Load("config.xml");
			btnSync.Click += btnSync_Click;
		}

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void gunaPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void Close_Button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Timer_Sidebar_Menu_Tick(object sender, EventArgs e)
        {
            if (sideBar_Expand)
            {
                SideBar.Width -= 10;
                if (SideBar.Width == SideBar.MinimumSize.Width)
                {
                    sideBar_Expand = false;
                    Timer_Sidebar_Menu.Stop();
                }
            }
            else
                {
                    SideBar.Width += 10;
                    if (SideBar.Width == SideBar.MaximumSize.Width)
                    {
                        sideBar_Expand = true;
                        Timer_Sidebar_Menu.Stop();
                    }
                }
        }   
        
        

        private void Menu_Button_Click(object sender, EventArgs e)
        {
            Timer_Sidebar_Menu.Start();
        }

        private void gunaImageButton1_Click(object sender, EventArgs e)
        {

        }

        private void Link_Github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            
        }

		private async void btnSync_Click(object sender, EventArgs e)
		{

			await RunInventorySyncAsync();
			// This button can be used to trigger the sidebar toggle manually
			// For example, you can call the Timer_Sidebar_Menu.Start() method here
			//         if (Timer_Sidebar_Menu.Enabled)
			//         {
			//             Timer_Sidebar_Menu.Stop();
			//         }
			//         else
			//         {
			//             Timer_Sidebar_Menu.Start();
			//}


			//try
			//{
			//	config = AppConfig.Load("config.xml");
			//	Log("Starting Inventory Sync Service...");

			//	var newItems = await GetMainData();
			//	Log($"Queried {newItems.Count} records from main database.");

			//	string output = FormatItems(newItems);

			//	string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
			//	Directory.CreateDirectory(outboundDir);

			//	string countryCode = config.CountryCode ?? "XX";
			//	string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
			//	string fileName = $"LS{countryCode}_AMA_PSSTKR_{timestamp}.txt";
			//	string filePath = Path.Combine(outboundDir, fileName);

			//	// Call this once at program start or before you need legacy encodings
			//	Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			//	// Now you can use:
			//	var ansiEncoding = Encoding.GetEncoding(1252);
			//	File.WriteAllText(filePath, output, ansiEncoding); File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

			//	MessageBox.Show($"✅ New items saved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//	Log($"✅ New items saved to: {filePath}");
			//}
			//catch (Exception ex)
			//{
			//	MessageBox.Show($"❌ Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//	Log($"❌ Error: {ex.Message}");
			//}



		}

		private async Task RunInventorySyncAsync()
		{
			try
			{
				var newItems = await GetMainData();
				string output = FormatItems(newItems);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");

				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string countryCode = config.CountryCode ?? "XX";
				string fileName = $"LS{countryCode}_AMA_PSSTKR_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Required
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

				MessageBox.Show($"✅ Inventory synced.\nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async Task<List<ItemData>> GetMainData()
		{
			using (var connection = new OracleConnection(config.MainDbConnection))
			{
				await connection.OpenAsync();

				string sql = @"
				SELECT 
					isi.sid,
					qty.qty,
					s.store_code,
					isi.alu,
					isi.upc
				FROM rps.invn_sbs_item isi
				LEFT JOIN rps.invn_sbs_item_qty qty ON qty.invn_sbs_item_sid = isi.sid
				LEFT JOIN rps.store s ON s.sid = qty.store_sid
				FETCH FIRST 1 ROWS ONLY";

				var data = await connection.QueryAsync<ItemData>(sql);
				return data.AsList();
			}
		}

		private string FormatItems(List<ItemData> items)
		{
			var sb = new StringBuilder();
			string d = config.Delimiter ?? "|";

			foreach (var item in items)
			{
				sb.AppendLine($"SID: {item.Sid}" +
					$"{d} Qty: {item.Qty}" +
					$"{d} Store: {item.StoreCode}" +
					$"{d} ALU: {item.Alu}" +
					$"{d} UPC: {item.Upc}");
			}
			return sb.ToString();
		}
		private void Log(string message)
		{
			string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
			Directory.CreateDirectory(logDir);

			string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

			string logMessage = $"[{timestamp}] {message}";
			// Optionally update a TextBox or Label here for live logs
			Console.WriteLine(logMessage);
			File.AppendAllText(logFile, logMessage + Environment.NewLine);
		}

	}

	public class ItemData
	{
		public long Sid { get; set; }
		public int Qty { get; set; }
		public string StoreCode { get; set; }
		public string Alu { get; set; }
		public string Upc { get; set; }
	}

	public class AppConfig
	{
		public string MainDbConnection { get; set; }
		public string CountryCode { get; set; }
		public string Delimiter { get; set; }

		public static AppConfig Load(string path)
		{
			// TODO: Implement your XML config loading logic here
			return new AppConfig
			{
				MainDbConnection = "User Id = reportuser; Password = report; Data Source = localhost:1521 / RPROODS",
				CountryCode = "US",
				Delimiter = "|"
			};
		}
	}
}
