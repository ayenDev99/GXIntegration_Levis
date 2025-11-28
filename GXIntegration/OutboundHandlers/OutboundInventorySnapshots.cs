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
		public static async Task Execute(InventoryRepository repository, GXConfig config, dynamic prismStores, DateTime procDate, bool isAuto)
		{
			try
			{
				foreach (var store in prismStores)
				{
					string storeCode = ((IDictionary<string, object>)store).TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";

					var items = await repository.GetInventoryAsync(procDate, storeCode);
					if (!items.Any())
					{
						// Logger.LogOutbound($"[- EOD] [TXT] No INVENTORY SNAPSHOTS data was found in Prism for today for StoreCode: {storeCode}", isAuto);
						return;
					}

					// Outbound directory setup
					string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
					Directory.CreateDirectory(outboundDir);
					string timestamp = procDate.ToString("ddMMyyyyHHmmss");

					// Archive directory setup
					string archiveRootDir = Path.Combine(outboundDir, "ARCHIVE");
					string archiveDateDir = Path.Combine(archiveRootDir, procDate.ToString("yyyyMMdd"));
					Directory.CreateDirectory(archiveDateDir);

			
					string todayPrefix = procDate.ToString("ddMMyyyy");
					var existingFiles = Directory.GetFiles(archiveDateDir, $"AMA_PH_{storeCode}_*.txt")
										.Where(f => Path.GetFileName(f).Contains(todayPrefix))
										.ToList();

					int nextSequence = existingFiles.Count + 1;
					string sequenceStr = nextSequence.ToString("D2");

					string fileName = $"AMA_PH_{storeCode}_PSSTKR_{sequenceStr}_{timestamp}.txt";
					string filePath = Path.Combine(outboundDir, fileName);
					string output = Format(items, config.Delimiter ?? "|");

					// Logger.LogOutbound($"Output Preview for {storeCode}:\n{output.Substring(0, Math.Min(500, output.Length))}");

					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

					Logger.LogOutbound($"[EOD] [InventorySnapshots] File Name: {fileName} | Item Count: {items.Count} | StoreCode: {storeCode}", isAuto);
					await GlobalOutbound.UploadToSftpAsync();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.LogOutbound("Error: " + ex.Message, isAuto);
			}
		}

		private static string Format(List<InventoryModel> items, string d)
		{
			var sb = new StringBuilder();
			string StockFetchDate = DateTime.Now.ToString("yyyyMMdd");

			foreach (var item in items)
			{
				var productCode = item.ProductCode;
				var trimmedProductCode = productCode.Remove(productCode.Length - 1, 1);

				//var result = $"{d}{item.StoreCode}";
				//var trimmed = result.Remove(result.Length - 1, 1);


				sb.AppendLine(
					$"{item.CurrencyId}" +
					$"{d}{item.StoreCode}" +
					$"{d}ON_HAND" +
					$"{d}{trimmedProductCode}" +
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
					$"{d}{item.QuantitySign}" +
					$"{d}{item.Quantity}"
				);
			}

			return sb.ToString();
		}
	}
}
