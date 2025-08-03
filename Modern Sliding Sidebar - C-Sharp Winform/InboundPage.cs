using Modern_Sliding_Sidebar___C_Sharp_Winform;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
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

namespace GXIntegration_Levis
{
	public partial class InboundPage : UserControl
	{
		static GXConfig config;
		private InventoryModel _inventoryModel;
		public InboundPage()
		{
			InitializeComponent();
			config = GXConfig.Load("config.xml");
			_inventoryModel = new InventoryModel(config.MainDbConnection);

		}

		private async void inventoryButton_Click(object sender, EventArgs e)
		{
			await RunInventorySyncAsync();
		}

		public async Task RunInventorySyncAsync()
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

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

				MessageBox.Show($"✅ Inventory synced.\nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				Logger.Log($"✅ New inventory file saved to: {filePath}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
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
					$"{d} BIN_TYPE: " + // BIN_TYPE
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

	}
}
