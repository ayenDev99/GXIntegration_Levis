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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Text.Json;


namespace GXIntegration_Levis
{
	public partial class OutboundPage : UserControl
	{
		static GXConfig config;
		private InventoryModel _inventoryModel;

		public OutboundPage()
		{
			InitializeComponent();
			config = GXConfig.Load("config.xml");
			_inventoryModel = new InventoryModel(config.MainDbConnection);
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
					$"{d}UNITCOUNT:"
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
					$"{d}{item.ProductCode}" +
					$"{d}{item.Sku}" +
					$"{d}{item.Waist}" +
					$"{d}{item.Inseam}" +
					// STORE_CODE
					$"{d}{item.Quantity}"
				);
			}

			return sb.ToString();
		}


	}
}
