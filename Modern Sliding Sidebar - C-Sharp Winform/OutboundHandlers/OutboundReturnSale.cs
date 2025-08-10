using GXIntegration_Levis.Data.Access;
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
	public static class OutboundReturnSale
	{
		public static async Task Execute(SalesRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var receipt_type = new List<int> { 1 };
				var items = await repository.GetSalesAsync(date, receipt_type);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreReturn_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				OutboundRetailSale.GenerateXml(items, filePath);

				MessageBox.Show($"Return Sale synced.\nSaved to: {outboundDir}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}

	}
}
