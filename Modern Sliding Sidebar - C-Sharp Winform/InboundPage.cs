using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
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
		private InventoryRepository _inventoryRepository;
		public InboundPage()
		{
			InitializeComponent();
			config = GXConfig.Load("config.xml");
			_inventoryRepository = new InventoryRepository(config.MainDbConnection);

		}

		private async void inventoryButton_Click(object sender, EventArgs e)
		{
			await RunInventorySyncAsync();
		}

		public async Task RunInventorySyncAsync()
		{
			try
			{
				DateTime date = DateTime.Today;
				var Items = await _inventoryRepository.GetInventoryAsync(date);
				string output = FormatInventorySnapshot(Items);

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

		private string FormatInventorySnapshot(List<InventoryModel> items)
		{
			var sb = new StringBuilder();
			string d = config.Delimiter ?? "|";

			foreach (var item in items)
			{
				sb.AppendLine($"{item.CurrencyId}" +	// CURRENCY_ID
					$"{d}{item.StoreId}" +				// STORE_ID
					$"{d}BIN_TYPE:" +					// BIN_TYPE
					$"{d}{item.ProductCode}" +			// PRODUCT_CODE 
					$"{d}{item.Sku}" +					// SKU 
					$"{d}{item.Waist}" +				// WAIST 
					$"{d}{item.Inseam}" +				// INSEAM 
					$"{d}" +							// EMPTY
					$"{d}STOCK_FETCH_DATE:" +			// STOCK_FETCH_DATE
					$"{d}{item.LastMovementDate}" +		// LAST_MOVEMENT_DATE
					$"{d}QUANTITY_SIGN:" +				// QUANTITY_SIGN
					$"{d}{item.Quantity}" +				// QUANTITY
					$"{d}0" +							// PURCHASE_COST
					$"{d}{item.RetailPrice}" +			// RETAIL_PRICE
					$"{d}0" +							// AVERAGE_COST
					$"{d}0" +							// MANUFACTURE_COST
					$"{d}AMA" +							// REGION
					$"{d}{item.CountryCode}" +			// COUNTRY_CODE
					$"{d}{item.ManufactureUpc}" +		// MANUFACTURE_UPC
					$"{d}{item.Division}" +				// DIVISION
					$"{d}" +							// EMPTY
					$"{d}" +							// EMPTY
					$"{d}" +							// EMPTY
					$"{d}UNITCOUNT_SIGN:" +				// UNITCOUNT_SIGN
					$"{d}UNITCOUNT:"					// UNITCOUNT
				);
			}
			return sb.ToString();
		}

	}
}
