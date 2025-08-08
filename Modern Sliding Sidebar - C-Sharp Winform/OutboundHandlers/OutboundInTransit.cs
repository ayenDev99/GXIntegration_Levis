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
	public static class OutboundInTransit
	{
		public static async Task Execute(InTransitRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var Items = await repository.GetInventoryAsync(date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);
				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

				var grouped = Items.GroupBy(i => i.CurrencyId ?? "UNKNOWN");

				foreach (var group in grouped)
				{
					string countryCode = config.CountryCode ?? "XX";
					string fileName = $"LS{countryCode}_AMA_INTRANSIT_{timestamp}.txt";
					string filePath = Path.Combine(outboundDir, fileName);

					string output = Format(group.ToList(), config.Delimiter ?? "|");
					
					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));
				}

				MessageBox.Show($"Intransit synced.\n{grouped.Count()} file(s) created.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}
		private static string Format(List<InTransitModel> items, string d)
		{
			var sb = new StringBuilder();

			foreach (var item in items)
			{
				sb.AppendLine(
					$"{item.ProductCode}" +
					$"{d}{item.Sku}" +
					$"{d}{item.Waist}" +
					$"{d}{item.Inseam}" +
					$"{d}{item.StoreCode}" +
					$"{d}{item.Quantity}{d}"
				);
			}

			return sb.ToString();
		}
	}

}
