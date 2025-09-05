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
	public static class OutboundInventorySnapshots
	{
		public static async Task Execute(InventoryRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetInventoryAsync(date);

				// Outbound directory setup
				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);
				string timestamp = DateTime.Now.ToString("ddMMyyyyHHmmss");

				// Archive directory setup
				string archiveRootDir = Path.Combine(outboundDir, "ARCHIVE");
				string archiveDateDir = Path.Combine(archiveRootDir, DateTime.Now.ToString("yyyyMMdd"));
				Directory.CreateDirectory(archiveDateDir);

				// Grouped by StoreCode
				var grouped = items.GroupBy(i => (i.StoreCode ?? "UNKNOWN").Trim());

				foreach (var group in grouped)
				{
					string storeCode = group.Key ?? "XX";

					string todayPrefix = DateTime.Now.ToString("ddMMyyyy");
					var existingFiles = Directory.GetFiles(archiveDateDir, $"AMA_PH_{storeCode}_*.txt")
										.Where(f => Path.GetFileName(f).Contains(todayPrefix))
										.ToList();

					int nextSequence = existingFiles.Count + 1;
					string sequenceStr = nextSequence.ToString("D3");

					string fileName = $"AMA_PH_{storeCode}_PSSTKR_{sequenceStr}_{timestamp}.txt";
					string filePath = Path.Combine(outboundDir, fileName);

					Logger.Log($"[OUTBOUND - EOD] [TXT] InventorySnapshots downloaded successfully | StoreCode: {storeCode} | Items Count: {group.Count()} | File Name: {fileName}");

					string output = Format(group.ToList(), config.Delimiter ?? "|");
					//Logger.Log($"Output Preview for {storeCode}:\n{output.Substring(0, Math.Min(500, output.Length))}");

					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));
				}

				//MessageBox.Show($"Inventory synced.\n{grouped.Count()} file(s) created.");
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
