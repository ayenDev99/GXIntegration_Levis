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
	public static class OutboundStoreGoods
	{
		public static async Task Execute(StoreGoodsRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				var (fromDate, toDate) = GlobalHelper.GetProcessingTimeWindow(config);
				//var items = await repository.GetStoreGoodsAsync(fromDate, toDate);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreGoods_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				//Logger.Log($"EOD StoreGoods downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
				//return GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreGoodsModel> items, string filePath, string generate_type)
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

		private static void WriteXmlContent(List<StoreGoodsModel> items, XmlWriter writer)
		{
			writer.WriteStartElement("Transaction");    // Transaction

			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ASN");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "RECEIVING");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

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
						var vouItems = vouGroup.FirstOrDefault();
						if (vouItems == null) continue;

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", vouItems.SequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(vouItems.BusinessDayDate));
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(vouItems.BeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(vouItems.EndDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "OperatorID", vouItems.OperatorID);
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", vouItems.CurrencyCode);

						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", vouItems.InventoryMovementSuccess);
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", vouItems.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", vouItems.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", vouItems.AlternateStoreID);

						writer.WriteStartElement("InventoryTransaction");
						writer.WriteStartElement("ReceiveInventory");

						GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", vouItems.DocumentStatus);
						GlobalOutbound.WriteCDataElement(writer, "DocumentID", vouItems.DocumentID);
						GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", vouItems.RetailStoreID);
						GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", vouItems.DocumentTypeDescription);
						GlobalOutbound.WriteCDataElement(writer, "DocumentType", vouItems.DocumentType);
						GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", vouItems.DocumentSubType);
						GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(vouItems.CompletionTimestamp, true));
						GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(vouItems.LastActivityTimestamp, true));

						// Shipment
						writer.WriteStartElement("Shipment");
						GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", vouItems.ShipmentSequence);
						GlobalOutbound.WriteCDataElement(writer, "DestinationRetailLocationID", vouItems.DestinationRetailLocationID);
						GlobalOutbound.WriteCDataElement(writer, "StatusCode", vouItems.ShipmentStatusCode);
						writer.WriteEndElement(); // </Shipment>

						// Carton
						writer.WriteStartElement("Carton");
						GlobalOutbound.WriteCDataElement(writer, "CartonID", vouItems.CartonID);
						GlobalOutbound.WriteCDataElement(writer, "StatusCode", vouItems.CartonStatusCode);

						foreach (var lineItem in vouGroup)
						{
							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");
							GlobalOutbound.WriteCDataElement(writer, "LineNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);
							GlobalOutbound.WriteCDataElement(writer, "ActualCount", lineItem.ActualCount);
							GlobalOutbound.WriteCDataElement(writer, "ExpectedCount", lineItem.ExpectedCount);
							GlobalOutbound.WriteCDataElement(writer, "PostedCount", lineItem.PostedCount);

							writer.WriteStartElement("SaleLineItem");
							GlobalOutbound.WriteCDataElement(writer, "RetailLocationID", lineItem.RetailStoreID);
							GlobalOutbound.WriteCDataElement(writer, "WorkstationID", lineItem.WorkstationID);
							GlobalOutbound.WriteCDataElement(writer, "BusinessDate", GlobalOutbound.FormatDate(lineItem.SaleLineBusinessDayDate, true));
							GlobalOutbound.WriteCDataElement(writer, "TransactionSequence", lineItem.TransactionSequence);
							GlobalOutbound.WriteCDataElement(writer, "LineItemSequence", lineItem.LineItemSequence);
							writer.WriteEndElement(); // </SaleLineItem>

							GlobalOutbound.WriteCDataElement(writer, "RecordCreationType", lineItem.RecordCreationType);
							GlobalOutbound.WriteCDataElement(writer, "StatusCode", lineItem.LineItemStatusCode);
							writer.WriteEndElement(); // </LineItem>
						}

						writer.WriteEndElement(); // </Carton>

						// LineItems outside Carton
						foreach (var lineItem in vouGroup)
						{
							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");
							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);

							GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
							GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
							GlobalOutbound.WriteLineItemProperty(writer, "CONTROL_NUMBER", "STRING", lineItem.PTControlNumber);
							GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

							GlobalOutbound.WriteCDataElement(writer, "QuantityOrdered", lineItem.QuantityOrdered);
							GlobalOutbound.WriteCDataElement(writer, "QuantityReceived", lineItem.QuantityReceived);
							GlobalOutbound.WriteCDataElement(writer, "CartonNumber", vouItems.SequenceNo);
							GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", lineItem.LineNumber);
							GlobalOutbound.WriteCDataElement(writer, "Description", lineItem.Description);
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
