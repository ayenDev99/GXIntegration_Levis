using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundStoreInventoryAdjustment
	{
		public static async Task Execute(StoreInventoryAdjustmentRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetStoreInventoryAdjustmentAsync(date);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreReceiving_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath);

				MessageBox.Show($"Store Receiving synced.\nFile Name: {fileName}. \nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static void GenerateXml(List<StoreInventoryAdjustmentModel> items, string filePath)
		{
			var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// Root element with namespaces
				writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
				writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
				writer.WriteAttributeString("dtv", GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xs", GlobalOutbound.NsXsi);
				writer.WriteAttributeString("xs", "schemaLocation", GlobalOutbound.NsXsi, $"{GlobalOutbound.NsIXRetail} POSLog.xsd");

				// Transaction
				writer.WriteStartElement("Transaction");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
				writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "STORE_TRANSFER");
				writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "SHIPPING");
				writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

				//// Grouping by store and processing
				//foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.StoreCode))
				//{
				//	GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");
				//	GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);

				//	foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationNo))
				//	{
				//		GlobalOutbound.WriteCDataElement(writer, "WorkstationID", "");
				//		GlobalOutbound.WriteCDataElement(writer, "TillID", storeGroup.Key);

				//		foreach (var vouGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
				//		{
				//			var item = vouGroup.FirstOrDefault();
				//			if (item == null) continue;

				//			GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.SequenceNo);
							
				//		}
				//	}
				//}

				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndElement(); // </POSLog>
				writer.WriteEndDocument();
			}
		}

	}
}
