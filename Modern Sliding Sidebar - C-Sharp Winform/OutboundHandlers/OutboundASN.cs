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

				MessageBox.Show($"ASN - RECEIVING synced.\nFile Name: {fileName}. \nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}

		public static void GenerateXml(List<ASNModel> items, string filePath)
		{
			var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				writer.WriteStartElement("POSLog"); // No default namespace
				writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
				writer.WriteAttributeString("xs", "schemaLocation", null,
					"http://www.nrf-arts.org/IXRetail/namespace/ POSLog.xsd");

				// Transaction start
				writer.WriteStartElement("Transaction");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
				writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "RETURN_TO_DC");
				writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "SHIPPING");
				writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", "OO01");
				writer.WriteElementString("WorkstationID", "");
				GlobalOutbound.WriteCDataElement(writer, "TillID", "OO01");
				GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", "6010170803");
				GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", "2025-02-19");
				GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", "2025-02-19T10:49:30.827");
				GlobalOutbound.WriteCDataElement(writer, "EndDateTime", "2025-02-19T10:49:32.467");
				GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", "TRY");

				// Transaction properties
				GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
				GlobalOutbound.WritePosTransactionProperties(writer, "REGION", "AMA");
				GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", "TR");
				GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", "3501");
				GlobalOutbound.WritePosTransactionProperties(writer, "REASON_CODE", "T121");
				GlobalOutbound.WritePosTransactionProperties(writer, "ORIGIN_ALTERNATE_STOREID", "3501");

				// InventoryTransaction -> ReturnToVendor
				writer.WriteStartElement("InventoryTransaction");
				writer.WriteStartElement("ReturnToVendor");

				GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", "CLOSED");
				GlobalOutbound.WriteCDataElement(writer, "DocumentID", "6010170803");
				writer.WriteElementString("RetailStoreID", "");
				writer.WriteElementString("OriginatorID", "");
				GlobalOutbound.WriteCDataElement(writer, "OriginatorName", "LS ISTANBUL BEYOGLU");
				GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", "SHIPPING_RTV_FROM_DAMAGED");
				GlobalOutbound.WriteCDataElement(writer, "DocumentType", "SHIPPING");
				GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", "RTV_to_DC");
				GlobalOutbound.WriteCDataElement(writer, "ReasonCode", "T121");
				GlobalOutbound.WriteCDataElement(writer, "CreationTimestamp", "2025-02-19T10:49:30.827");
				GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", "2025-02-19T10:49:32.467");
				GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", "2025-02-19T10:49:32.467");

				// Shipment
				writer.WriteStartElement("Shipment");
				GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", "1");
				GlobalOutbound.WriteCDataElement(writer, "ActualDeliveryDate", "2025-02-19T10:49:32.467");
				GlobalOutbound.WriteCDataElement(writer, "ActualShipDate", "2025-02-19T10:49:32.467");
				GlobalOutbound.WriteCDataElement(writer, "DestinationPartyID", "1018");
				writer.WriteElementString("DestinationRetailLocationID", "");
				GlobalOutbound.WriteCDataElement(writer, "StatusCode", "SHIPPED");

				// Address
				writer.WriteStartElement("Address");
				writer.WriteElementString("City", "");
				GlobalOutbound.WriteCDataElement(writer, "PostalCode", "34755");
				GlobalOutbound.WriteCDataElement(writer, "Country", "TR");

				writer.WriteStartElement("AddressLine1");
				writer.WriteAttributeString("Type", "Text");
				writer.WriteCData("");
				writer.WriteEndElement();

				writer.WriteStartElement("Territory");
				writer.WriteAttributeString("TypeCode", "State");
				writer.WriteCData("");
				writer.WriteEndElement();

				writer.WriteEndElement(); // </Address>
				writer.WriteEndElement(); // </Shipment>

				// LineItem
				writer.WriteStartElement("LineItem");
				writer.WriteAttributeString("VoidFlag", "false");
				GlobalOutbound.WriteCDataElement(writer, "ItemID", "002VL00010L");
				GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedBarcodeID", GlobalOutbound.NsDtv, "5401187102436");
				GlobalOutbound.WriteCDataElement(writer, "dtv", "QuantityShipped", GlobalOutbound.NsDtv, "1");
				GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", "1");
				GlobalOutbound.WriteCDataElement(writer, "Description", "BLR BLAIR SHIRT STYLE WT BLAIR SHIRT STY");

				GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", "L");
				GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", "-");
				GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", "002VL00010");
				GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", "5401187102436");

				writer.WriteEndElement(); // </LineItem>

				writer.WriteEndElement(); // </ReturnToVendor>
				writer.WriteEndElement(); // </InventoryTransaction>
				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndElement(); // </POSLog>

				writer.WriteEndDocument();
			}
		}

	}
}
