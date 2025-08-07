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
using System.Xml;
using System.Xml.Serialization;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static  class OutboundRetailSale
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
					string fileName = $"StoreSale_{timestamp}.xml";
					string filePath = Path.Combine(outboundDir, fileName);

					GenerateXml(group.ToList(), filePath);
				}

				MessageBox.Show($"✅ Inventory synced.\nSaved to: {outboundDir}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}
		private static void GenerateXml(List<InventoryModel> items, string filePath)
		{
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8
			};

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// <POSLog>
				writer.WriteStartElement("POSLog", "http://www.nrf-arts.org/IXRetail/namespace/");

				// Add namespaces as attributes
				writer.WriteAttributeString("xmlns", "dtv", null, "http://www.datavantagecorp.com/xstore/");
				writer.WriteAttributeString("xmlns", "xs", null, "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("dtv", "http://www.datavantagecorp.com/xstore/");
				writer.WriteAttributeString("xs", "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("schemaLocation", "http://www.nrf-arts.org/IXRetail/namespace/ POSLog.xsd");

				// <Transaction>
				writer.WriteStartElement("Transaction", "http://www.nrf-arts.org/IXRetail/namespace/");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");

				writer.WriteAttributeString("dtv", "TransactionType", "http://www.datavantagecorp.com/xstore/", "RETAIL_SALE");

				// <dtv:OrganizationID><![CDATA[1]]></dtv:OrganizationID>
				writer.WriteStartElement("dtv", "OrganizationID", "http://www.datavantagecorp.com/xstore/");
				writer.WriteCData("1");
				writer.WriteEndElement(); // </dtv:OrganizationID>

				// Optionally, loop and add items here
				foreach (var item in items)
				{
					writer.WriteStartElement("Item");

					writer.WriteElementString("ProductCode", item.ProductCode ?? "");
					writer.WriteElementString("Sku", item.Sku ?? "");
					writer.WriteElementString("Quantity", item.Quantity?.ToString() ?? "0");

					writer.WriteEndElement(); // </Item>
				}

				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndElement(); // </POSLog>

				writer.WriteEndDocument();
			}
		}
	}
}
