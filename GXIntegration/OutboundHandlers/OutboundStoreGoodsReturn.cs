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

		private static void WriteXmlContent(List<StoreGoodsReturnModel> items, XmlWriter writer)
		{
			writer.WriteStartElement("Transaction");	// Transaction
			
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", "http://www.datavantagecorp.com/xstore/", "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", "http://www.datavantagecorp.com/xstore/", "RETURN_TO_DC");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", "http://www.datavantagecorp.com/xstore/", "SHIPPING");
			writer.WriteAttributeString("dtv", "TransactionType", "http://www.datavantagecorp.com/xstore/", "INVENTORY_CONTROL");


			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.OrganizationID))
			{
				var itemStore = storeGroup.FirstOrDefault();
				if (itemStore == null) continue;

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", "http://www.datavantagecorp.com/xstore/", itemStore.OrganizationID);
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
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", vouItems.CurrencyCode);

						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", vouItems.InventoryMovementSuccess);
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", vouItems.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", vouItems.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", vouItems.AlternateStoreID);
						GlobalOutbound.WritePosTransactionProperties(writer, "REASON_CODE", vouItems.ReasonCode);
						GlobalOutbound.WritePosTransactionProperties(writer, "ORIGIN_ALTERNATE_STOREID", vouItems.OriginAlternateStoreID);

						writer.WriteStartElement("InventoryTransaction");
						writer.WriteStartElement("ReturnToVendor");

						GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", vouItems.DocumentStatus);
						GlobalOutbound.WriteCDataElement(writer, "DocumentID", vouItems.DocumentID);
						GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", vouItems.RetailStoreID);
						GlobalOutbound.WriteCDataElement(writer,"OriginatorID", vouItems.OrganizationID);
						GlobalOutbound.WriteCDataElement(writer, "OriginatorName", vouItems.OriginatorName);
						GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", vouItems.DocumentTypeDescription);
						GlobalOutbound.WriteCDataElement(writer, "DocumentType", vouItems.DocumentType);
						GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", vouItems.DocumentSubType);
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
						writer.WriteElementString("DestinationRetailLocationID", vouItems.DestinationRetailLocationID);
						GlobalOutbound.WriteCDataElement(writer, "StatusCode", vouItems.ShipmentStatusCode);

						// Address
						writer.WriteStartElement("Address");
						GlobalOutbound.WriteCDataElement(writer, "City", vouItems.City);
						GlobalOutbound.WriteCDataElement(writer, "PostalCode", vouItems.PostalCode);
						GlobalOutbound.WriteCDataElement(writer, "Country", vouItems.Country);
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
							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);
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
				}
			}

			writer.WriteEndElement(); // Transaction

		}

	}
}
