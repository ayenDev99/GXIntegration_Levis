using Guna.UI.WinForms;
using Modern_Sliding_Sidebar___C_Sharp_Winform;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;


namespace GXIntegration_Levis
{
	public partial class OutboundPage : UserControl
	{
		static GXConfig config;
		private InventoryModel _inventoryModel;
		private GunaDataGridView guna1DataGridView1;
		public OutboundPage()
		{
			config = GXConfig.Load("config.xml");
			_inventoryModel = new InventoryModel(config.MainDbConnection);

			InitializeComponent();
			InitializeTable();
		}
		private void InitializeTable()
		{
			// Create and configure the DataGridView
			guna1DataGridView1 = new GunaDataGridView();
			guna1DataGridView1.Location = new Point(220, 70);
			guna1DataGridView1.Size = new Size(800, 300);

			guna1DataGridView1.ColumnCount = 4;
			guna1DataGridView1.AllowUserToAddRows = false;

			// Enable scrollbars
			guna1DataGridView1.ScrollBars = ScrollBars.Both;

			// Disable auto-sizing to allow scrolling
			guna1DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

			// Set column headers
			guna1DataGridView1.Columns[0].Name = "ID";
			guna1DataGridView1.Columns[1].Name = "Name";
			guna1DataGridView1.Columns[2].Name = "File Name Format";
			guna1DataGridView1.Columns[3].Name = "File Type";

			// Set column widths (sum should exceed DataGridView width to trigger horizontal scroll)
			guna1DataGridView1.Columns[0].Width = 50;
			guna1DataGridView1.Columns[1].Width = 200;
			guna1DataGridView1.Columns[2].Width = 450;
			guna1DataGridView1.Columns[3].Width = 100;

			// Optional: Allow cell text to clip or wrap
			guna1DataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

			// Optional: Styling
			guna1DataGridView1.Theme = GunaDataGridViewPresetThemes.Guna;
			guna1DataGridView1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
			guna1DataGridView1.ThemeStyle.HeaderStyle.ForeColor = Color.White;
			guna1DataGridView1.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
			guna1DataGridView1.BackgroundColor = Color.White;
			guna1DataGridView1.BorderStyle = BorderStyle.None;
			guna1DataGridView1.GridColor = Color.LightGray;

			// EOD rows
			guna1DataGridView1.Rows.Add("1", "Inventory Snapshot", "LS[Country code]_AMA_PSSTKR_[yyyymmddhhmmss]", ".txt");
			guna1DataGridView1.Rows.Add("2", "InTransit", "LS[Country Code]_[REGION Code]_INTRANSIT_[yyyymmddhhmmss]", ".txt");
			guna1DataGridView1.Rows.Add("3", "Price", "[REGION Code]_[Country code]_PRICING_[yyyymmddhhmmss]", ".txt");

			// Add to form
			this.Controls.Add(guna1DataGridView1);
		}

		private async void testInventorySnapshot_Click(object sender, EventArgs e)
		{
			await RunInventorySyncAsync();
		}

		private async void testInTransit_Click(object sender, EventArgs e)
		{
			await RunInTransitSyncAsync();
		}

		public async Task RunInventorySyncAsync()
		{
			try
			{
				var newItems = await _inventoryModel.GetMainData();

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

				// Group items by CurrencyId
				var groupedByCurrency = newItems
					.GroupBy(item => item.CurrencyId ?? "UNKNOWN")
					.OrderBy(g => g.Key);

				foreach (var group in groupedByCurrency)
				{
					string currencyCode = group.Key.Substring(0, 2);
					string fileName = $"LS{currencyCode}_AMA_PSSTKR_{timestamp}.txt";
					string filePath = Path.Combine(outboundDir, fileName);

					string output = FormatInventorySnapshot(group.ToList());

					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

					Logger.Log($"✅ Inventory for currency '{currencyCode}' saved to: {filePath}");
				}

				MessageBox.Show($"✅ Inventory synced.\n{groupedByCurrency.Count()} file(s) created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}

		public async Task RunInTransitSyncAsync()
		{
			try
			{
				var newItems = await _inventoryModel.GetMainData();
				string output = FormatIntransit(newItems);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");

				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string countryCode = config.CountryCode ?? "XX";
				string fileName = $"LS{countryCode}_AMA_INTRANSIT_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

				MessageBox.Show($"✅ Inventory synced.\nSaved to: {newItems}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				Logger.Log($"✅ New inventory file saved to: {filePath}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}

		private string FormatInventorySnapshot(List<Inventory> items)
		{
			var sb = new StringBuilder();
			string d = config.Delimiter ?? "|";

			foreach (var item in items)
			{
				sb.AppendLine(
					$"{item.CurrencyId}" +
					$"{d}{item.StoreId}" +
					$"{d}BIN_TYPE:" +
					$"{d}{item.ProductCode}" +
					$"{d}{item.Sku}" +
					$"{d}{item.Waist}" +
					$"{d}{item.Inseam}" +
					$"{d}" +
					$"{d}STOCK_FETCH_DATE:" +
					$"{d}{item.LastMovementDate}" +
					$"{d}QUANTITY_SIGN:" +
					$"{d}{item.Quantity}" +
					$"{d}0" +
					$"{d}{item.RetailPrice}" +
					$"{d}0" +
					$"{d}0" +
					$"{d}AMA" +
					$"{d}{item.CountryCode}" +
					$"{d}{item.ManufactureUpc}" +
					$"{d}{item.Division}" +
					$"{d}" +
					$"{d}" +
					$"{d}" +
					$"{d}UNITCOUNT_SIGN:" +
					$"{d}UNITCOUNT:{d}"
				);
			}

			return sb.ToString();
		}

		private string FormatIntransit(List<Inventory> items)
		{
			var sb = new StringBuilder();
			string d = config.Delimiter ?? "|";

			foreach (var item in items)
			{
				sb.AppendLine(
					$"{item.ProductCode}" +
					$"{d}{item.Sku}" +
					$"{d}{item.Waist}" +
					$"{d}{item.Inseam}" +
					$"{d}{item.StoreId}" +
					$"{d}{item.Quantity}{d}"
				);
			}

			return sb.ToString();
		}


	}
}
