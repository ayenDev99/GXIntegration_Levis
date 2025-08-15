using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using GXIntegration.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GXIntegration_Levis.Helpers;

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

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"AMA_PH_PRICING_{timestamp}.txt";
				string filePath = Path.Combine(outboundDir, fileName);

				string output = Format(items, ",");

				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				File.WriteAllText(filePath, output, Encoding.GetEncoding(1252));
		
				//MessageBox.Show($"✅ Price synced file(s) created.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log("❌ Error: " + ex.Message);
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
