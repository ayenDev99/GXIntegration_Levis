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

		public static async Task Execute(DateTime processDate, List<StoreInventoryCountModel> items, GXConfig config, string generate_type, string storeCode, bool isAuto)
		{
			try
			{
				if (!items.Any()) { return; }

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				string archiveDir = Path.Combine(outboundDir, "ARCHIVE", processDate.ToString("yyyyMMdd"));

				Directory.CreateDirectory(outboundDir);
				Directory.CreateDirectory(archiveDir);

				string countryCode = config.CountryCode ?? "XX";
				string todayPrefix = processDate.ToString("ddMMyyyy");

				var existingFiles = Directory.GetFiles(archiveDir, $"AMA_{countryCode}_{storeCode}_INVENTORYCOUNT_*.xml")
					.Where(f => Path.GetFileName(f).Contains(todayPrefix))
					.ToList();

				int nextSequence = existingFiles.Count + 1;
				string sequenceStr = nextSequence.ToString("D2");
				string timestamp = processDate.ToString("ddMMyyyyHHmmss");

				string fileName = $"AMA_{countryCode}_{storeCode}_INVENTORYCOUNT_{sequenceStr}_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				Logger.LogOutbound($"[EOD] StoreInventoryCount downloaded successfully | StoreCode: {storeCode} | Items Count: {items.Count} | File Name: {fileName}", isAuto);

				GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.LogError($"Error: {ex.Message}", isAuto);
			}
		}

		public static string GenerateXml(List<StoreInventoryCountModel> items, string filePath, string generate_type)
		{
			if (!items.Any()) { return null; }

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

			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.RetailStoreID))
			{
				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationID))
				{
					var item = wsGroup.FirstOrDefault();
					if (item == null) continue;

					//---------------------
					// Transaction Section
					//---------------------
					writer.WriteStartElement("Transaction", GlobalOutbound.NsIXRetail);
					writer.WriteAttributeString("CancelFlag", "false");
					writer.WriteAttributeString("OfflineFlag", "false");
					writer.WriteAttributeString("TrainingModeFlag", "false");
					writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "21.0.3.0.45 - 3.2.8 - 0.0");
					writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "INVENTORY_COUNT");
					writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

					GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, item.OrganizationID);
					GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", item.RetailStoreID);
					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", item.WorkstationID);
					GlobalOutbound.WriteCDataElement(writer, "TillID", item.TillID);
					GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.SequenceNo);
					GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(item.BusinessDayDate));
					GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(item.BeginDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(item.EndDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "OperatorId", item.OperatorID);
					GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", item.CurrencyCode);

					GlobalOutbound.WritePosTransactionProperties(writer, "REGION", item.Region);
					GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", item.Country);
					GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", item.AlternateStoreID);

					// InventoryTransaction
					writer.WriteStartElement("InventoryTransaction");
					writer.WriteStartElement("InventoryCount");

					GlobalOutbound.WriteCDataElement(writer, "CountID", item.CountID);
					GlobalOutbound.WriteCDataElement(writer, "DueDate", GlobalOutbound.FormatDate(item.DueDate));
					GlobalOutbound.WriteCDataElement(writer, "CountType", item.CountType);
					GlobalOutbound.WriteCDataElement(writer, "CountStatus", item.CountStatus);
					GlobalOutbound.WriteCDataElement(writer, "dtv", "VariancesAdjusted", GlobalOutbound.NsDtv, item.VariancesAdjusted);

					// ItemCount
					foreach (var countItem in wsGroup)
					{
						writer.WriteStartElement("ItemCount");
						GlobalOutbound.WriteCDataElement(writer, "ItemID", countItem.ItemCountItemID);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedBarcodeID", GlobalOutbound.NsDtv, countItem.ItemCountScannedBarcodeID);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "DIM1", GlobalOutbound.NsDtv, countItem.ItemCountDIM1);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "DIM2", GlobalOutbound.NsDtv, countItem.ItemCountDIM2);
						GlobalOutbound.WriteCDataElement(writer, "Quantity", countItem.ItemCountQuantity);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "SnapshotQuantity", GlobalOutbound.NsDtv, countItem.ItemCountSnapshotQuantity ?? "0");
						GlobalOutbound.WriteCDataElement(writer, "dtv", "UnitVariance", GlobalOutbound.NsDtv, countItem.ItemCountUnitVariance);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "InventoryBucketId", GlobalOutbound.NsDtv, countItem.ItemCountInventoryBucketID);
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
