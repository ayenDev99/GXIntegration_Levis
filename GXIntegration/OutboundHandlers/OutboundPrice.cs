using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundPrice
	{
		public static async Task Execute(PriceRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetPriceAsync(date);

				// Outbound directory setup
				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);
				string timestamp = DateTime.Now.ToString("ddMMyyyyHHmmss");

				// Archive directory setup
				string archiveRootDir = Path.Combine(outboundDir, "ARCHIVE");
				string archiveDateDir = Path.Combine(archiveRootDir, DateTime.Now.ToString("yyyyMMdd"));
				Directory.CreateDirectory(archiveDateDir);

				string todayPrefix = DateTime.Now.ToString("ddMMyyyy");
				var existingFiles = Directory.GetFiles(archiveDateDir, "AMA_PH_PRICING_*.txt")
									.Where(f => Path.GetFileName(f).Contains(todayPrefix))
									.ToList();

				int nextSequence = existingFiles.Count + 1;
				string sequenceStr = nextSequence.ToString("D3");

				string fileName = $"AMA_PH_PRICING_{sequenceStr}_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				Logger.Log($"[TXT] Price downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");

				string output = Format(items, ",");

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));

				return;
				//MessageBox.Show($"✅ Price synced file(s) created.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log("❌ Error: " + ex.Message);

				return;
			}
		}
		private static string Format(List<PriceModel> items, string d)
		{
			var sb = new StringBuilder();

			sb.AppendLine(
				$"SALES_ORG" +
				$"{d}PC9" +
				$"{d}PRICE_LIST" +
				$"{d}CONDITION_TYPE" +
				$"{d}PRICE_START_DATE" +
				$"{d}PRICE_END_DATE" +
				$"{d}PRICE" +
				$"{d}FLAG"
			);

			foreach (var item in items)
			{
				sb.AppendLine(
					$"{item.SalesOrg}" +
					$"{d}{item.PC9}" +
					$"{d}{item.PriceLevel}" +
					$"{d}{item.ConditionType}" +
					$"{d}{item.PriceStartDate}" +
					$"{d}{item.PriceEndDate}" +
					$"{d}{item.Price}" +
					$"{d}{item.Flag}"
				);
			}
			
			return sb.ToString();
		}
	}
}
