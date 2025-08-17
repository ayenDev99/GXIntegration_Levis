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
	public static class OutboundStoreShipping
	{
		public static async Task Execute(StoreShippingRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				DateTime from_date = DateTime.Today; // 00:00:00
				DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
				var items = await repository.GetStoreShippingAsync(from_date, to_date);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreShipping_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreShippingModel> items, string filePath, string generate_type)
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

		private static void WriteXmlContent(List<StoreShippingModel> items, XmlWriter writer)
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

			// Grouping by store and processing
			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.StoreCode))
			{
				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);

				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationNo))
				{
					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", "");
					GlobalOutbound.WriteCDataElement(writer, "TillID", storeGroup.Key);

					foreach (var vouGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
					{
						var item = vouGroup.FirstOrDefault();
						if (item == null) continue;

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.SequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(item.BusinessDayDate));
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(item.BeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(item.EndDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "OperatorID", item.OperatorId);
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", item.CurrencyCode);

						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", item.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", item.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", item.AlternateStoreId);
						GlobalOutbound.WritePosTransactionProperties(writer, "DEST_ALTERNATE_STOREID", item.DestinationAlternateStoreId);
						GlobalOutbound.WritePosTransactionProperties(writer, "ORIGIN_ALTERNATE_STOREID", item.OriginAlternateStoreId);

						writer.WriteStartElement("InventoryTransaction");
						writer.WriteStartElement("TransferOut");

						GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", item.DocumentStatus);
						GlobalOutbound.WriteCDataElement(writer, "DocumentID", item.DocumentId);
						GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", item.StoreCode);
						GlobalOutbound.WriteCDataElement(writer, "OriginatorID", "1");
						GlobalOutbound.WriteCDataElement(writer, "OriginatorName", "");
						GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", "SHIPPING_STORE_TRANSFER");
						GlobalOutbound.WriteCDataElement(writer, "DocumentType", "SHIPPING");
						GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", "STORE_TRANSFER");
						GlobalOutbound.WriteCDataElement(writer, "RecordCreationType", "STORE");
						GlobalOutbound.WriteCDataElement(writer, "CreationTimestamp", GlobalOutbound.FormatDate(item.CreationTimestamp, true));
						GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(item.CompletionTimestamp, true));
						GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(item.LastActivityTimestamp, true));

						// Shipment
						writer.WriteStartElement("Shipment");
						GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", item.ShipmentSequence);
						GlobalOutbound.WriteCDataElement(writer, "ActualDeliveryDate", GlobalOutbound.FormatDate(item.ActualDeliveryDate, true));
						GlobalOutbound.WriteCDataElement(writer, "ActualShipDate", GlobalOutbound.FormatDate(item.ActualShipDate, true));
						GlobalOutbound.WriteCDataElement(writer, "DestinationRetailLocationID", item.DestinationRetailLocationId);
						GlobalOutbound.WriteCDataElement(writer, "ShippingCarrier", item.ShippingCarrier);
						GlobalOutbound.WriteCDataElement(writer, "TrackingNumber", item.TrackingNumber);
						GlobalOutbound.WriteCDataElement(writer, "StatusCode", item.ShipmentStatusCode);

						// Address block
						writer.WriteStartElement("Address");
						writer.WriteStartElement("AddressLine1");
						writer.WriteAttributeString("Type", "Text");
						writer.WriteString(""); // Fill with value if available
						writer.WriteEndElement(); // </AddressLine1>
						writer.WriteStartElement("AddressLine2");
						writer.WriteAttributeString("Type", "Text");
						writer.WriteString(""); // Fill with value if available
						writer.WriteEndElement(); // </AddressLine2>
						writer.WriteElementString("City", "");
						GlobalOutbound.WriteCDataElement(writer, "PostalCode", item.PostalCode);
						GlobalOutbound.WriteCDataElement(writer, "Country", item.Country);
						writer.WriteEndElement(); // </Address>
						writer.WriteEndElement(); // </Shipment>

						// Line Items
						foreach (var lineItem in vouGroup)
						{
							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemId);

							GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
							GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
							GlobalOutbound.WriteLineItemProperty(writer, "CONTROL_NUMBER", "STRING", lineItem.PTControlNumber);
							GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

							GlobalOutbound.WriteCDataElement(writer, "dtv", "QuantityShipped", GlobalOutbound.NsDtv, lineItem.QuantityShipped);
							GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "Description", lineItem.Description);

							writer.WriteEndElement(); // </LineItem>
						}

						writer.WriteEndElement(); // </TransferOut>
						writer.WriteEndElement(); // </InventoryTransaction>
					}
				}
			}

			writer.WriteEndElement(); // </Transaction>
			writer.WriteEndElement(); // </POSLog>
			writer.WriteEndDocument();
		}

	}
}
