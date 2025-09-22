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
				var (fromDate, toDate) = GlobalHelper.GetProcessingTimeWindow(config);
				var items = await repository.GetInventoryAsync(fromDate, toDate);
				string countryCode = config.CountryCode ?? "XX";
				if (!items.Any())
				{
					Logger.Log("[OUTBOUND - EOD] [TXT] No INTRANSIT data was found in Prism for today.");
					return;
				}

				// Outbound directory setup
				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);
				string timestamp = DateTime.Now.ToString("ddMMyyyyHHmmss");

				// Archive directory setup
				string archiveRootDir = Path.Combine(outboundDir, "ARCHIVE");
				string archiveDateDir = Path.Combine(archiveRootDir, DateTime.Now.ToString("yyyyMMdd"));
				Directory.CreateDirectory(archiveDateDir);

				string todayPrefix = DateTime.Now.ToString("ddMMyyyy");
				var existingFiles = Directory.GetFiles(archiveDateDir, $"AMA_{countryCode}_INTRANSIT_*.txt")
									.Where(f => Path.GetFileName(f).Contains(todayPrefix))
									.ToList();

				int nextSequence = existingFiles.Count + 1;
				string sequenceStr = nextSequence.ToString("D2");

				string fileName = $"AMA_{countryCode}_INTRANSIT_{sequenceStr}_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				Logger.Log($"[OUTBOUND - EOD] [TXT]	InTransit downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");

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

			sb.AppendLine(
				$"ProductCode" +
				$"{d}SKU" +
				$"{d}DIM1" +
				$"{d}DIM2" +
				$"{d}StoreCode" +
				$"{d}TotalOpenQTY"
			);

			foreach (var item in items)
			{
				sb.AppendLine(
					$"{item.ProductCode}" +
					$"{d}{item.Sku}" +
					$"{d}{item.Waist}" +
					$"{d}{item.Inseam}" +
					$"{d}{item.StoreCode}" +
					$"{d}{item.Quantity}"
				);
			}

			return sb.ToString();
		}
	}

}
