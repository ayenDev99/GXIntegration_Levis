using Modern_Sliding_Sidebar___C_Sharp_Winform;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundInventorySnapshots
	{
		public static async Task Execute(InventoryModel model, GXConfig config)
		{
			try
			{
				var items = await model.GetMainData();

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);
				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

				var grouped = items.GroupBy(i => i.CurrencyId ?? "UNKNOWN");

				foreach (var group in grouped)
				{
					string currencyCode = group.Key.Substring(0, 2);
					string fileName = $"LS{currencyCode}_AMA_PSSTKR_{timestamp}.txt";
					string filePath = Path.Combine(outboundDir, fileName);

					string output = Format(group.ToList(), config.Delimiter ?? "|");

					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));
				}

				MessageBox.Show($"✅ Inventory synced.\n{grouped.Count()} file(s) created.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log("❌ Error: " + ex.Message);
			}
		}

		private static string Format(List<Inventory> items, string d)
		{
			var sb = new StringBuilder();

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
					$"{d}UNITCOUNT:" +
					$"{d}"
				);
			}

			return sb.ToString();
		}
	}
}
