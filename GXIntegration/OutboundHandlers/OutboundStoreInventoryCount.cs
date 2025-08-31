using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using GXIntegration.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.OutboundHandlers
{
	public class OutboundStoreInventoryCount
	{
		public static async Task Execute(StoreInventoryCountRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				DateTime from_date = DateTime.Today; // 00:00:00
				DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
				var items = await repository.GetStoreInventoryCountAsync(from_date, to_date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreInventoryCount_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				Logger.Log($"EOD StoreInventoryCount downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");

				GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreInventoryCountModel> items, string filePath, string generate_type)
		{
			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8,
				OmitXmlDeclaration = false
			};

			if (generate_type == "template")
			{
				using (var stringWriter = new StringWriter())
				using (var writer = XmlWriter.Create(stringWriter, settings))
				{
					WriteXmlContent(items, writer);
					writer.Flush();
					return stringWriter.ToString();
				}
			}
			else if (generate_type == "xml")
			{
				using (var writer = XmlWriter.Create(filePath, settings))
				{
					WriteXmlContent(items, writer);
					writer.Flush();
				}
				return null;
			}
			else
			{
				throw new ArgumentException("Invalid generate_type. Must be 'xml' or 'template'.");
			}

		}

		public static void WriteXmlContent(List<StoreInventoryCountModel> items, XmlWriter writer)
		{
			writer.WriteStartDocument();

			// Root
			writer.WriteStartElement("InventoryCountLog", GlobalOutbound.NsIXRetail);
			writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
			writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
			writer.WriteAttributeString("xs", "schemaLocation", GlobalOutbound.NsXsi,
				$"{GlobalOutbound.NsIXRetail} Inventory.xsd {GlobalOutbound.NsDtv} DtvInventory.xsd");

			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.StoreCode))
			{
				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationNo))
				{
					var item = wsGroup.FirstOrDefault();
					if (item == null) continue;

					writer.WriteStartElement("Transaction");
					writer.WriteAttributeString("CancelFlag", "false");
					writer.WriteAttributeString("OfflineFlag", "false");
					writer.WriteAttributeString("TrainingModeFlag", "false");
					writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "21.0.3.0.45 - 3.2.8 - 0.0");
					writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "INVENTORY_COUNT");
					writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

					GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");
					GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);
					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", "1");
					GlobalOutbound.WriteCDataElement(writer, "TillID", item.StoreCode + item.WorkstationNo + item.SequenceNo);
					GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.SequenceNo);
					GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(item.BusinessDayDate));
					GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(item.BeginDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(item.EndDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "OperatorId", item.OperatorId);
					GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", item.CurrencyCode);

					GlobalOutbound.WritePosTransactionProperties(writer, "REGION", item.Region);
					GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", item.Country);
					GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", item.AlternateStoreId);

					// InventoryTransaction
					writer.WriteStartElement("InventoryTransaction");
					writer.WriteStartElement("InventoryCount");

					GlobalOutbound.WriteCDataElement(writer, "CountID", item.CountID);
					GlobalOutbound.WriteCDataElement(writer, "DueDate", GlobalOutbound.FormatDate(item.BusinessDayDate));
					GlobalOutbound.WriteCDataElement(writer, "CountType", item.CountType);
					GlobalOutbound.WriteCDataElement(writer, "CountStatus", item.CountStatus);
					GlobalOutbound.WriteCDataElement(writer, "dtv", "VariancesAdjusted", GlobalOutbound.NsDtv, item.CountStatus.ToString().ToLower());

					// ItemCount
					foreach (var countItem in wsGroup)
					{
						writer.WriteStartElement("ItemCount");
						GlobalOutbound.WriteCDataElement(writer, "ItemID", countItem.ItemId);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedBarcodeID", GlobalOutbound.NsDtv, countItem.ItemId);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "DIM1", GlobalOutbound.NsDtv, countItem.PTDIM1);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "DIM2", GlobalOutbound.NsDtv, countItem.PTDIM2);
						GlobalOutbound.WriteCDataElement(writer, "Quantity", countItem.ItemId);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "SnapshotQuantity", GlobalOutbound.NsDtv, countItem.ItemId);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "UnitVariance", GlobalOutbound.NsDtv, countItem.ItemId);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "InventoryBucketId", GlobalOutbound.NsDtv, countItem.InventoryBucketId);
						writer.WriteEndElement(); // </ItemCount>
					}

					writer.WriteEndElement(); // </InventoryCount>
					writer.WriteEndElement(); // </InventoryTransaction>
					writer.WriteEndElement(); // </Transaction>
				}
			}

			writer.WriteEndElement(); // </InventoryCountLog>
			writer.WriteEndDocument();
		}

	}
}
