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
	public static class OutboundASN
	{
		public static async Task Execute(ASNRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var vou_type = new List<int> { 0 }; // [0] Regular
				var vou_class = new List<int> { 2 }; // [2] ASN
				var items = await repository.GetASNAsync(date, vou_type, vou_class);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreGoods_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath);

				//MessageBox.Show($"ASN - RECEIVING synced.\nFile Name: {fileName}. \nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static void GenerateXml(List<ASNModel> items, string filePath)
		{
			var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
				writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
				writer.WriteAttributeString("dtv", GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xs", GlobalOutbound.NsXsi);
				writer.WriteAttributeString("schemaLocation", GlobalOutbound.NsIXRetail + "POSLog.xsd");

				// Transaction
				writer.WriteStartElement("Transaction");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
				writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ASN");
				writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "RECEIVING");
				writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");

				foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.StoreCode))
				{
					GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);

					foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationNo))
					{
						GlobalOutbound.WriteCDataElement(writer, "WorkstationID", wsGroup.Key);
						GlobalOutbound.WriteCDataElement(writer, "TillID", storeGroup.Key + wsGroup.Key);

						foreach (var vouGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
						{
							var vouItems = vouGroup.FirstOrDefault();
							if (vouItems == null) continue;

							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", vouItems.SequenceNo);
							GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(vouItems.BusinessDayDate));
							GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(vouItems.BeginDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(vouItems.EndDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "OperatorID", vouItems.OperatorId);
							GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", vouItems.CurrencyCode);

							GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
							GlobalOutbound.WritePosTransactionProperties(writer, "REGION", vouItems.Region);
							GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", vouItems.Country);
							GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", vouItems.AlternateStoreId);

							writer.WriteStartElement("InventoryTransaction");
							writer.WriteStartElement("ReceiveInventory");

							GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", vouItems.DocumentStatus);
							GlobalOutbound.WriteCDataElement(writer, "DocumentID", vouItems.DocumentId);
							GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", "");
							GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", "RECEIVING_ASN");
							GlobalOutbound.WriteCDataElement(writer, "DocumentType", "RECEIVING");
							GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", "ASN");
							GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(vouItems.CompletionTimestamp));
							GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(vouItems.LastActivityTimestamp));

							// Shipment
							writer.WriteStartElement("Shipment");
							GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", vouItems.ShipmentSequence);
							GlobalOutbound.WriteCDataElement(writer, "DestinationRetailLocationID", vouItems.DestinationRetailLocationId);
							GlobalOutbound.WriteCDataElement(writer, "StatusCode", vouItems.ShipmentStatusCode);
							writer.WriteEndElement(); // </Shipment>

							// Carton
							writer.WriteStartElement("Carton");
							GlobalOutbound.WriteCDataElement(writer, "CartonID", vouItems.SequenceNo);
							GlobalOutbound.WriteCDataElement(writer, "StatusCode", "1");

							foreach (var lineItem in vouGroup)
							{
								writer.WriteStartElement("LineItem");
								writer.WriteAttributeString("VoidFlag", "false");
								GlobalOutbound.WriteCDataElement(writer, "LineNumber", lineItem.LineNumber);
								GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemId);
								GlobalOutbound.WriteCDataElement(writer, "ActualCount", lineItem.ActualCount);
								GlobalOutbound.WriteCDataElement(writer, "ExpectedCount", lineItem.ExpectedCount);
								GlobalOutbound.WriteCDataElement(writer, "PostedCount", lineItem.PostedCount);

								writer.WriteStartElement("SaleLineItem");
								GlobalOutbound.WriteCDataElement(writer, "RetailLocationID", "");
								GlobalOutbound.WriteCDataElement(writer, "WorkstationID", "");
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
								GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemId);

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
				writer.WriteEndElement(); // </POSLog>
				writer.WriteEndDocument();
			}
		}

	}
}
