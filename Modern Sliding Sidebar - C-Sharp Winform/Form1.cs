using Dapper;
using GXIntegration_Levis;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
using Oracle.ManagedDataAccess.Client;
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


namespace Modern_Sliding_Sidebar___C_Sharp_Winform
{
	public partial class Form1 : Form
    {

		static GXConfig config;
		private InventoryModel _inventoryModel;

		bool sideBar_Expand = true;
		private Guna.UI.WinForms.GunaButton _activeButton = null;

		public Form1()
        {
            InitializeComponent();
			config = GXConfig.Load("config.xml");
			_inventoryModel = new InventoryModel(config.MainDbConnection);
		}

        private void Form1_Load(object sender, EventArgs e)
        {
			SetActiveSidebarButton(Home_Button);
			LoadPage(new HomePage());
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

		// *********************************************************
		// Sidebar Buttons
		// *********************************************************
		private void Home_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
			LoadPage(new HomePage());
		}

		private void Configuration_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
		}
		private void Inbound_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
		}

		private void Outbound_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
		}
		private void About_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
			LoadPage(new AboutPage());
		}







		private async void btnSync_Click(object sender, EventArgs e)
		{
			await RunInventorySyncAsync();
		}

		private async Task RunInventorySyncAsync()
		{
			try
			{
				var newItems = await _inventoryModel.GetMainData();
				string output = FormatInventory(newItems);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");

				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string countryCode = config.CountryCode ?? "XX";
				string fileName = $"LS{countryCode}_AMA_PSSTKR_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Required
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

				MessageBox.Show($"✅ Inventory synced.\nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				Log($"✅ New inventory file saved to: {filePath}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Log($"❌ Error: {ex.Message}");
			}
		}

		private string FormatInventory(List<Inventory> items)
		{
			var sb = new StringBuilder();
			string d = config.Delimiter ?? "|";

			foreach (var item in items)
			{
				sb.AppendLine($" CURRENCY_ID: {item.AlphabeticCode}" + // CURRENCY_ID
					$"{d} STORE_ID: " + // STORE_ID
					$"{d} BIN_TYPE: " +	// BIN_TYPE
					$"{d} PRODUCT_CODE: {item.Description1}" + // PRODUCT_CODE 
					$"{d} ALU: " + // SKU 
					$"{d} WAIST: " + // WAIST 
					$"{d} INSEAM: " + // INSEAM 
					$"{d}" +    // EMPTY
					$"{d} STOCK_FETCH_DATE: " +    // STOCK_FETCH_DATE
					$"{d} LAST_MOVEMENT_DATE: " +    // LAST_MOVEMENT_DATE
					$"{d} QUANTITY_SIGN: " +    // QUANTITY_SIGN
					$"{d} QUANTITY: " +    // QUANTITY
					$"{d} PURCHASE_COST: 0 " + // PURCHASE_COST
					$"{d} RETAIL_PRICE: " + // RETAIL_PRICE
					$"{d} AVERAGE_COST: 0 " + // AVERAGE_COST
					$"{d} MANUFACTURE_COST: 0 " + // MANUFACTURE_COST
					$"{d} REGION: " + // REGION
					$"{d} COUNTRY_CODE: " + // COUNTRY_CODE
					$"{d} MANUFACTURE_UPC: " + // MANUFACTURE_UPC
					$"{d} DIVISION: " + // DIVISION
					$"{d}" +    // EMPTY
					$"{d}" +    // EMPTY
					$"{d}" +    // EMPTY
					$"{d} UNITCOUNT_SIGN: " + // UNITCOUNT_SIGN
					$"{d} UNITCOUNT: "  // UNITCOUNT
				);	
			}
			return sb.ToString();
		}


		// *********************************************************
		// Helpers
		// *********************************************************
		private void Log(string message)
		{
			string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
			Directory.CreateDirectory(logDir);

			string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string logMessage = $"[{timestamp}] {message}";

			Console.WriteLine(logMessage);
			File.AppendAllText(logFile, logMessage + Environment.NewLine);
		}
		private void SetActiveSidebarButton(Guna.UI.WinForms.GunaButton button)
		{
			// Reset previous active button
			if (_activeButton != null)
			{
				_activeButton.BaseColor = Color.Transparent;
				_activeButton.ForeColor = Color.White;
				_activeButton.OnHoverBaseColor = Color.FromArgb(40, 40, 100);
				_activeButton.OnHoverForeColor = Color.White;
			}

			// Set new active button
			_activeButton = button;
			_activeButton.BaseColor = Color.FromArgb(60, 60, 120); // Static active color
			_activeButton.ForeColor = Color.White;
			_activeButton.OnHoverBaseColor = _activeButton.BaseColor; // Lock hover color
			_activeButton.OnHoverForeColor = _activeButton.ForeColor;
		}

		private void LoadPage(UserControl page)
		{
			MainContentPanel.Controls.Clear();
			page.Dock = DockStyle.Fill;
			MainContentPanel.Controls.Add(page);
		}

	}

}
