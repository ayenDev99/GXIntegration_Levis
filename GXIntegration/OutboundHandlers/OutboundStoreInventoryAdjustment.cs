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
	public static class OutboundStoreInventoryAdjustment
	{
		public static async Task Execute(StoreInventoryAdjustmentRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				DateTime from_date = DateTime.Today; // 00:00:00
				DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
				//var items = await repository.GetStoreInventoryAdjustmentAsync(from_date, to_date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreInventoryAdjustment_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				//Logger.Log($"EOD StoreInventoryAdjustment downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
				//GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreInventoryAdjustmentModel> items, string filePath, string generate_type)
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

		public static void WriteXmlContent(List<StoreInventoryAdjustmentModel> items, XmlWriter writer)
		{
			writer.WriteStartDocument();

			// Root element with namespaces
			writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
			writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
			writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
			writer.WriteAttributeString("dtv", GlobalOutbound.NsDtv);
			writer.WriteAttributeString("xs", GlobalOutbound.NsXsi);
			writer.WriteAttributeString("xs", "schemaLocation", GlobalOutbound.NsXsi, $"{GlobalOutbound.NsIXRetail} POSLog.xsd");

			// Transaction element
			writer.WriteStartElement("Transaction");
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ADJUSTMENT");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "ADJUSTMENT");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

			// Grouping by store and processing
			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.OrganizationID))
			{
				var itemStore = storeGroup.FirstOrDefault();
				if (itemStore == null) continue;

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, itemStore.OrganizationID);
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", itemStore.RetailStoreID);

				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationID))
				{
					var itemWs = wsGroup.FirstOrDefault();
					if (itemWs == null) continue;

					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", itemWs.WorkstationID);
					GlobalOutbound.WriteCDataElement(writer, "TillID", itemWs.TillID);

					GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", itemWs.SequenceNo);
					GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(itemWs.BusinessDayDate));
					GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(itemWs.BeginDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(itemWs.EndDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "OperatorID", itemWs.OperatorID);
					GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", itemWs.CurrencyCode);

					GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", itemWs.InventoryMovementSuccess);
					GlobalOutbound.WritePosTransactionProperties(writer, "REGION", itemWs.Region);
					GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", itemWs.Country);
					GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", itemWs.AlternateStoreID);

					// InventoryTransaction block
					foreach (var invTransGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
					{
						var invTransItem = invTransGroup.FirstOrDefault();

						foreach (var itemItem in invTransGroup)
						{
							writer.WriteStartElement("InventoryTransaction");
							GlobalOutbound.WriteCDataElement(writer, "CountID", itemItem.CountID);
							GlobalOutbound.WriteCDataElement(writer, "CountType", itemItem.CountType);
							GlobalOutbound.WriteCDataElement(writer, "CountStatus", itemItem.CountStatus);
							GlobalOutbound.WriteCDataElement(writer, "ReasonCode", itemItem.ReasonCode ?? "");
							GlobalOutbound.WriteCDataElement(writer, "Comment", itemItem.Comments ?? "");

							foreach (var lineItem in invTransGroup)
							{
								// ItemCount block
								writer.WriteStartElement("ItemCount");
								writer.WriteAttributeString("VoidFlag", "false");
								GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);
								GlobalOutbound.WriteCDataElement(writer, "Quantity", lineItem.QuantityShipped);
								GlobalOutbound.WriteCDataElement(writer, "dtv", "InventoryBucketId", GlobalOutbound.NsDtv, lineItem.InventoryBucketID);

								// LineItem properties
								GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
								GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
								GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
								GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

								writer.WriteEndElement(); // </ItemCount>
							}

							writer.WriteEndElement(); // </InventoryTransaction>
						}
					}
				}

			}

			writer.WriteEndElement(); // </POSLog>
			writer.WriteEndDocument();
			
		}
	
	}
}
