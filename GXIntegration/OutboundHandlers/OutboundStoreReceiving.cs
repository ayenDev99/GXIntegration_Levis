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
	public static class OutboundStoreReceiving
	{
		//public static async Task Execute(StoreReceivingRepository repository, GXConfig config, string generate_type)
		//{
		//	try
		//	{
		//		DateTime from_date = DateTime.Today; // 00:00:00
		//		DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
		//		//var items = await repository.GetStoreReceivingAsync(from_date, to_date);

		//		string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
		//		Directory.CreateDirectory(outboundDir);

		//		string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
		//		string fileName = $"StoreReceiving_{timestamp}.xml";
		//		string filePath = Path.Combine(outboundDir, fileName);

		//		//Logger.Log($"EOD StoreReceiving downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
		//		//GenerateXml(items, filePath, generate_type);
		//	}
		//	catch (Exception ex)
		//	{
		//		MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//		Logger.Log($"Error: {ex.Message}");
		//	}
		//}

		public static string GenerateXml(List<StoreReceivingModel> items, string filePath, string generate_type)
		{
			if (!items.Any()) { return null; }

			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8,
				OmitXmlDeclaration = true
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

		private static void WriteXmlContent(List<StoreReceivingModel> items, XmlWriter writer)
		{
			writer.WriteStartElement("Transaction");    // Transaction

			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "STORE_TRANSFER");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "RECEIVING");
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

					foreach (var vouGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
					{
						var item = vouGroup.FirstOrDefault();
						if (item == null) continue;

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.SequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(item.BusinessDayDate));
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(item.BeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(item.EndDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "OperatorID", item.OperatorID);
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", item.CurrencyCode);

						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", item.InventoryMovementSuccess);
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", item.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", item.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", item.AlternateStoreId);
						GlobalOutbound.WritePosTransactionProperties(writer, "DEST_ALTERNATE_STOREID", item.DestinationAlternateStoreId);
						GlobalOutbound.WritePosTransactionProperties(writer, "ORIGIN_ALTERNATE_STOREID", item.OriginAlternateStoreId);

						writer.WriteStartElement("InventoryTransaction");
						writer.WriteStartElement("ReceiveInventory");

						GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", item.DocumentStatus);
						GlobalOutbound.WriteCDataElement(writer, "DocumentID", item.DocumentID);
						GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", item.RetailStoreID);
						GlobalOutbound.WriteCDataElement(writer, "OriginatorID", item.OriginatorID);
						GlobalOutbound.WriteCDataElement(writer, "OriginatorName", item.OriginatorName);
						GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", item.DocumentTypeDescription);
						GlobalOutbound.WriteCDataElement(writer, "DocumentType", item.DocumentType);
						GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", item.DocumentSubType);
						GlobalOutbound.WriteCDataElement(writer, "RecordCreationType", item.RecordCreationType);
						GlobalOutbound.WriteCDataElement(writer, "CreationTimestamp", GlobalOutbound.FormatDate(item.CreationTimestamp, true));
						GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(item.CompletionTimestamp, true));
						GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(item.LastActivityTimestamp, true));

						// Shipment
						writer.WriteStartElement("Shipment");
						GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", item.ShipmentSequence);
						GlobalOutbound.WriteCDataElement(writer, "DestinationRetailLocationID", item.DestinationRetailLocationId);
						GlobalOutbound.WriteCDataElement(writer, "ShippingCarrier", item.ShippingCarrier);
						GlobalOutbound.WriteCDataElement(writer, "TrackingNumber", item.TrackingNumber);  
						GlobalOutbound.WriteCDataElement(writer, "StatusCode", item.StatusCode);
						writer.WriteEndElement(); // </Shipment>

						// Carton block
						writer.WriteStartElement("Carton");

						foreach (var lineItem in vouGroup)
						{
							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "LineNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);
							GlobalOutbound.WriteCDataElement(writer, "ActualCount", lineItem.ActualCount);
							GlobalOutbound.WriteCDataElement(writer, "ExpectedCount", lineItem.ExpectedCount);
							GlobalOutbound.WriteCDataElement(writer, "PostedCount", lineItem.PostedCount);
							GlobalOutbound.WriteCDataElement(writer, "RecordCreationType", item.RecordCreationType);
							GlobalOutbound.WriteCDataElement(writer, "StatusCode", item.StatusCode);
							GlobalOutbound.WriteCDataElement(writer, "QuantityOrdered", lineItem.QuantityOrdered);
							GlobalOutbound.WriteCDataElement(writer, "QuantityReceived", lineItem.QuantityReceived);
							GlobalOutbound.WriteCDataElement(writer, "CartonNumber", lineItem.CartonNumber);
							GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "Description", lineItem.Description);

							GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
							GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
							GlobalOutbound.WriteLineItemProperty(writer, "CONTROL_NUMBER", "STRING", lineItem.PTControlNumber);
							GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

							writer.WriteEndElement(); // </LineItem>
						}

						writer.WriteEndElement(); // </Carton>

						// Repeat same LineItems outside Carton
						foreach (var lineItem in vouGroup)
						{
							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "LineNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);
							GlobalOutbound.WriteCDataElement(writer, "ActualCount", lineItem.ActualCount);
							GlobalOutbound.WriteCDataElement(writer, "ExpectedCount", lineItem.ExpectedCount);
							GlobalOutbound.WriteCDataElement(writer, "PostedCount", lineItem.PostedCount);
							writer.WriteElementString("RecordCreationType", "");
							writer.WriteElementString("StatusCode", "");
							GlobalOutbound.WriteCDataElement(writer, "QuantityOrdered", lineItem.QuantityOrdered);
							GlobalOutbound.WriteCDataElement(writer, "QuantityReceived", lineItem.QuantityReceived);
							writer.WriteElementString("CartonNumber", "");
							GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "Description", lineItem.Description);

							GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
							GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
							GlobalOutbound.WriteLineItemProperty(writer, "CONTROL_NUMBER", "STRING", lineItem.PTControlNumber);
							GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

							writer.WriteEndElement(); // </LineItem>
						}

						writer.WriteEndElement(); // </ReceiveInventory>
						writer.WriteEndElement(); // </InventoryTransaction>
					}
				}
			}

			writer.WriteEndElement(); // </Transaction>

		}
	}
}
