using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
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
		public static async Task Execute(InventoryRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetInventoryAsync(date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);
				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

				var grouped = items.GroupBy(i => i.CurrencyId ?? "UNKNOWN");

				foreach (var group in grouped)
				{
					string currencyCode = group.Key.Substring(0, 2);
					string fileName = $"LSPH_AMA_PSSTKR_{timestamp}.txt";
					string filePath = Path.Combine(outboundDir, fileName);

					string output = Format(group.ToList(), config.Delimiter ?? "|");

					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));
				}

				MessageBox.Show($"Inventory synced.\n{grouped.Count()} file(s) created.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log("Error: " + ex.Message);
			}
		}

		private static string Format(List<InventoryModel> items, string d)
		{
			var sb = new StringBuilder();
			string StockFetchDate = DateTime.Now.ToString("yyyyMMdd");

			foreach (var item in items)
			{
				sb.AppendLine(
					$"{item.CurrencyId}" +
					$"{d}{item.StoreCode}" +
					$"{d}ON_HAND" +
					$"{d}{item.ProductCode}" +
					$"{d}{item.Sku}" +
					$"{d}{item.Waist}" +
					$"{d}{item.Inseam}" +
					$"{d}" +
					$"{d}{StockFetchDate}" +
					$"{d}{item.LastMovementDate:yyyyMMdd}" +
					$"{d}{item.QuantitySign}" +
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
					$"{d}{item.QuantitySign}" +	// UNITCOUNT_SIGN tempo
					$"{d}{item.Quantity}" +		// UNITCOUNT tempo
					$"{d}"
				);
			}

			return sb.ToString();
		}
	}
}
