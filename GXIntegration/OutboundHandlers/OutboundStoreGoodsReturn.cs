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
		public static async Task Execute(StoreGoodsReturnRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				DateTime from_date = DateTime.Today; // 00:00:00
				DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
				//var items = await repository.GetStoreGoodsReturnAsync(from_date, to_date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreGoodsReturn_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				//Logger.Log($"EOD StoreGoodsReturn downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
				//GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreGoodsReturnModel> items, string filePath, string generate_type)
		{
			if (!items.Any()) { return null; }

			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment
			};

			if (generate_type == "template")
			{
				using (var stringWriter = new StringWriter())
				using (var writer = XmlWriter.Create(stringWriter, settings))
				{
					var grouped = items.GroupBy(s => s.TransSequenceNo?.Trim() ?? string.Empty).ToList();

					foreach (var g in grouped)
					{
						WriteXmlContent(g.ToList(), writer);
					}

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

		private static void WriteXmlContent(List<StoreGoodsReturnModel> items, XmlWriter writer)
		{
			var first = items.FirstOrDefault();
			if (first == null) return;

			//---------------------
			// Transaction Section
			//---------------------
			writer.WriteStartElement("Transaction");
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "RETURN_TO_DC");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "SHIPPING");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

			// Transaction Header Info
			GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, first.TransOrganizationID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.TransRetailStoreID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.TransWorkstationID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "TillID", first.TransTillID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", first.TransSequenceNo ?? "");
			GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(first.TransBusinessDayDate) ?? "");
			GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(first.TransBeginDateTime, true) ?? "");
			GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(first.TransEndDateTime, true) ?? "");
			GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", first.TransCurrencyCode ?? "");

			GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
			GlobalOutbound.WritePosTransactionProperties(writer, "REGION", "AMA");
			GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", "PH");
			GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", first.TransAlternateStoreID ?? "");
			GlobalOutbound.WritePosTransactionProperties(writer, "REASON_CODE", first.TransReasonCode ?? "");
			GlobalOutbound.WritePosTransactionProperties(writer, "ORIGIN_ALTERNATE_STOREID", first.TransOriginAlternateStoreID ?? "");

			// InventoryTransaction
			writer.WriteStartElement("InventoryTransaction");
			writer.WriteStartElement("ReturnToVendor");

			GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", first.TransDocumentStatus);
			GlobalOutbound.WriteCDataElement(writer, "DocumentID", first.TransDocumentID);
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.TransRetailStoreID);
			GlobalOutbound.WriteCDataElement(writer, "OriginatorID", first.TransOrganizationID);
			GlobalOutbound.WriteCDataElement(writer, "OriginatorName", first.TransOriginatorName);
			GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", "SHIPPING_RTV_FROM_DAMAGED");
			GlobalOutbound.WriteCDataElement(writer, "DocumentType", "SHIPPING");
			GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", "RTV_to_DC");
			GlobalOutbound.WriteCDataElement(writer, "ReasonCode", first.TransReasonCode);
			GlobalOutbound.WriteCDataElement(writer, "CreationTimestamp", GlobalOutbound.FormatDate(first.TransCreationTimestamp, true));
			GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(first.TransCompletionTimestamp, true));
			GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(first.TransLastActivityTimestamp, true));

			//---------------------
			// Shipment Section
			//---------------------
			writer.WriteStartElement("Shipment");
			GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", first.ShipmentSequence);
			GlobalOutbound.WriteCDataElement(writer, "ActualDeliveryDate", GlobalOutbound.FormatDate(first.ActualDeliveryDate, true));
			GlobalOutbound.WriteCDataElement(writer, "ActualShipDate", GlobalOutbound.FormatDate(first.ActualShipDate, true));
			GlobalOutbound.WriteCDataElement(writer, "DestinationPartyID", first.DestinationPartyID);
			GlobalOutbound.WriteCDataElement(writer, "DestinationRetailLocationID", first.DestinationRetailLocationID);
			GlobalOutbound.WriteCDataElement(writer, "StatusCode", first.ShipmentStatusCode);

				//---------------------
				// Address Section
				//---------------------
				writer.WriteStartElement("Address");
				GlobalOutbound.WriteCDataElement(writer, "City", first.City);
				GlobalOutbound.WriteCDataElement(writer, "PostalCode", first.PostalCode);
				GlobalOutbound.WriteCDataElement(writer, "Country", "PH");
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

			foreach (var item in items)
			{
				//---------------------
				// Items Section (per item)
				//---------------------
				if (item.SGRItems?.Any() == true)
				{
					foreach (var itm in item.SGRItems.OrderBy(d => d.LineNumber))
					{
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");
						GlobalOutbound.WriteCDataElement(writer, "ItemID", itm.ItemID);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedBarcodeID", GlobalOutbound.NsDtv, itm.PTEAN);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "QuantityShipped", GlobalOutbound.NsDtv, itm.QuantityShipped);
						GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", itm.LineNumber);
						GlobalOutbound.WriteCDataElement(writer, "Description", itm.Description);

						GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", itm.PTDIM1);
						GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", itm.PTDIM2);
						GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", itm.PTStyle);
						GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", itm.PTEAN);

						writer.WriteEndElement(); // LineItem
					}

				}
			}

			writer.WriteEndElement(); // ReturnToVendor
			writer.WriteEndElement(); // InventoryTransaction

			writer.WriteEndElement(); // Transaction
		}

	}
}
