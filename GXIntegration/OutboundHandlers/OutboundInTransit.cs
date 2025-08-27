using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using GXIntegration;
using GXIntegration.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundInTransit
	{
		public static async Task Execute(InTransitRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetInventoryAsync(date);
				string countryCode = config.CountryCode ?? "XX";

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string todayPrefix = DateTime.Now.ToString("ddMMyyyy");
				var existingFiles = Directory.GetFiles(outboundDir, $"AMA_{countryCode}_INTRANSIT_*.txt")
									.Where(f => Path.GetFileName(f).Contains(todayPrefix))
									.ToList();

				int nextSequence = existingFiles.Count + 1;
				string sequenceStr = nextSequence.ToString("D3");

				string timestamp = DateTime.Now.ToString("ddMMyyyyHHmmss");
				string fileName = $"AMA_{countryCode}_INTRANSIT_{sequenceStr}_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				Logger.Log($"EOD InTransit downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");

				string output = Format(items, "|");

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

				//MessageBox.Show($"Intransit synced.\n{grouped.Count()} file(s) created.");
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
