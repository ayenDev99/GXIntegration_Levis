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
	public static class OutboundStoreGoodsReturn
	{
		public static async Task Execute(StoreGoodsReturnRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetStoreGoodsReturnAsync(date);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreGoodsReturn_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath);

				//MessageBox.Show($"RETURN TO DC/Store Goods Return synced.\nFile Name: {fileName}. \nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static void GenerateXml(List<StoreGoodsReturnModel> items, string filePath)
		{
			Logger.Log($"Generating XML for {items.Count} items");
			var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// Root: POSLog
				writer.WriteStartElement("POSLog");
				writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
				writer.WriteAttributeString("xs", "schemaLocation", null, GlobalOutbound.NsIXRetail + "POSLog.xsd");

				writer.WriteStartElement("Transaction");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "AppVersion", "http://www.datavantagecorp.com/xstore/", "");
				writer.WriteAttributeString("dtv", "InventoryDocumentSubType", "http://www.datavantagecorp.com/xstore/", "RETURN_TO_DC");
				writer.WriteAttributeString("dtv", "InventoryDocumentType", "http://www.datavantagecorp.com/xstore/", "SHIPPING");
				writer.WriteAttributeString("dtv", "TransactionType", "http://www.datavantagecorp.com/xstore/", "INVENTORY_CONTROL");

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", "http://www.datavantagecorp.com/xstore/", "1");

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
							GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", vouItems.CurrencyCode);

							GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
							GlobalOutbound.WritePosTransactionProperties(writer, "REGION", vouItems.Region);
							GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", vouItems.Country);
							GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", vouItems.AlternateStoreId);
							GlobalOutbound.WritePosTransactionProperties(writer, "REASON_CODE", vouItems.ReasonCode);
							GlobalOutbound.WritePosTransactionProperties(writer, "ORIGIN_ALTERNATE_STOREID", vouItems.AlternateStoreId);

							writer.WriteStartElement("InventoryTransaction");
							writer.WriteStartElement("ReturnToVendor");

							GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", vouItems.DocumentStatus);
							GlobalOutbound.WriteCDataElement(writer, "DocumentID", vouItems.DocumentId);
							writer.WriteElementString("RetailStoreID", string.Empty);
							writer.WriteElementString("OriginatorID", string.Empty);
							GlobalOutbound.WriteCDataElement(writer, "OriginatorName", vouItems.OriginatorName);
							GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", "SHIPPING_RTV_FROM_DAMAGED");
							GlobalOutbound.WriteCDataElement(writer, "DocumentType", "SHIPPING");
							GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", "RTV_to_DC");
							GlobalOutbound.WriteCDataElement(writer, "ReasonCode", vouItems.ReasonCode);
							GlobalOutbound.WriteCDataElement(writer, "CreationTimestamp", GlobalOutbound.FormatDate(vouItems.BeginDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(vouItems.EndDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(vouItems.EndDateTime, true));

							// Shipment
							writer.WriteStartElement("Shipment");
							GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", vouItems.ShipmentSequence);
							GlobalOutbound.WriteCDataElement(writer, "ActualDeliveryDate", GlobalOutbound.FormatDate(vouItems.ActualDeliveryDate, true));
							GlobalOutbound.WriteCDataElement(writer, "ActualShipDate", GlobalOutbound.FormatDate(vouItems.ActualShipDate, true));
							GlobalOutbound.WriteCDataElement(writer, "DestinationPartyID", vouItems.DestinationPartyID);
							writer.WriteElementString("DestinationRetailLocationID", string.Empty);
							GlobalOutbound.WriteCDataElement(writer, "StatusCode", vouItems.ShipmentStatusCode);

							// Address
							writer.WriteStartElement("Address");
							writer.WriteElementString("City", string.Empty);
							GlobalOutbound.WriteCDataElement(writer, "PostalCode", vouItems.ShipmentPostalCode);
							GlobalOutbound.WriteCDataElement(writer, "Country", vouItems.ShipmentCountry);
							writer.WriteStartElement("AddressLine1");
							writer.WriteAttributeString("Type", "Text");
							writer.WriteString(string.Empty);
							writer.WriteEndElement(); // AddressLine1
							writer.WriteStartElement("Territory");
							writer.WriteAttributeString("TypeCode", "State");
							writer.WriteString(string.Empty);
							writer.WriteEndElement(); // Territory
							writer.WriteEndElement(); // Address

							writer.WriteEndElement(); // Shipment

							// LineItems
							foreach (var lineItem in items)
							{
								writer.WriteStartElement("LineItem");
								writer.WriteAttributeString("VoidFlag", "false");
								GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemId);
								GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedBarcodeID", "http://www.datavantagecorp.com/xstore/", lineItem.PTEAN);
								GlobalOutbound.WriteCDataElement(writer, "dtv", "QuantityShipped", "http://www.datavantagecorp.com/xstore/", lineItem.QuantityShipped);
								GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", lineItem.LineNumber);
								GlobalOutbound.WriteCDataElement(writer, "Description", lineItem.Description);

								// LineItemProperty
								GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
								GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
								GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
								GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

								writer.WriteEndElement(); // LineItem
							}

							writer.WriteEndElement(); // ReturnToVendor
							writer.WriteEndElement(); // InventoryTransaction
						

						}
						writer.WriteEndElement(); // Transaction
						writer.WriteEndElement(); // POSLog
						writer.WriteEndDocument();
					}
				}
				
			}
		}
		
	}
}
