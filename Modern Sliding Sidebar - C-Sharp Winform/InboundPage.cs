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
				sb.AppendLine($" CURRENCY_ID: {item.CurrencyId}" + // CURRENCY_ID
					$"{d} STORE_ID: {item.StoreId}" + // STORE_ID
					$"{d} BIN_TYPE: " + // BIN_TYPE
					$"{d} PRODUCT_CODE: {item.ProductCode}" + // PRODUCT_CODE 
					$"{d} ALU: {item.Sku}" + // SKU 
					$"{d} WAIST: {item.Waist}" + // WAIST 
					$"{d} INSEAM: {item.Inseam}" + // INSEAM 
					$"{d}" +    // EMPTY
					$"{d} STOCK_FETCH_DATE: " +    // STOCK_FETCH_DATE
					$"{d} LAST_MOVEMENT_DATE: {item.LastMovementDate}" +    // LAST_MOVEMENT_DATE
					$"{d} QUANTITY_SIGN: " +    // QUANTITY_SIGN
					$"{d} QUANTITY: {item.Quantity}" +    // QUANTITY
					$"{d} PURCHASE_COST: 0 " + // PURCHASE_COST
					$"{d} RETAIL_PRICE: {item.RetailPrice}" + // RETAIL_PRICE
					$"{d} AVERAGE_COST: 0 " + // AVERAGE_COST
					$"{d} MANUFACTURE_COST: 0 " + // MANUFACTURE_COST
					$"{d} REGION: AMA" + // REGION
					$"{d} COUNTRY_CODE: {item.CountryCode}" + // COUNTRY_CODE
					$"{d} MANUFACTURE_UPC: {item.ManufactureUpc}" + // MANUFACTURE_UPC
					$"{d} DIVISION: {item.Division}" + // DIVISION
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
