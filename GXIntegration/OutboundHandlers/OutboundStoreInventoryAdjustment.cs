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
		public static string GenerateXml(List<StoreInventoryAdjustmentModel> items, string filePath, string generate_type)
		{
			if (items == null || !items.Any())
				return string.Empty;

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
					var grouped = items.GroupBy(s => s.SequenceNo?.Trim() ?? string.Empty).ToList();

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

		public static void WriteXmlContent(List<StoreInventoryAdjustmentModel> items, XmlWriter writer)
		{
			var first = items.FirstOrDefault();
			if (first == null) return;

			//---------------------
			// Transaction Section
			//---------------------
			writer.WriteStartElement("Transaction", GlobalOutbound.NsIXRetail);
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ADJUSTMENT");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "ADJUSTMENT");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

			// Transaction Header Info
			GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, first.OrganizationID);
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.RetailStoreID);
			GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.WorkstationID);
			GlobalOutbound.WriteCDataElement(writer, "TillID", first.TillID);
			GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", first.SequenceNo);
			GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(first.BusinessDayDate));
			GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(first.BeginDateTime, true));
			GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(first.EndDateTime, true));
			GlobalOutbound.WriteCDataElement(writer, "OperatorID", first.OperatorID);
			GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", first.CurrencyCode);

			GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", first.InventoryMovementSuccess);
			GlobalOutbound.WritePosTransactionProperties(writer, "REGION", first.Region);
			GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", first.Country);
			GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", first.AlternateStoreID);

			writer.WriteStartElement("InventoryTransaction");
			GlobalOutbound.WriteCDataElement(writer, "CountID", first.CountID);
			GlobalOutbound.WriteCDataElement(writer, "CountType", first.CountType);
			GlobalOutbound.WriteCDataElement(writer, "CountStatus", first.CountStatus);
			GlobalOutbound.WriteCDataElement(writer, "ReasonCode", first.ReasonCode ?? "");
			GlobalOutbound.WriteCDataElement(writer, "Comment", first.Comments ?? "");

			//---------------------
			// Line Items
			//---------------------
			foreach (var item in items)
			{
				if (item.ItemID.Any() == true)
				{
					foreach (var invTransGroup in GlobalOutbound.GroupBySafe(items, i => i.ItemID))
					{
						var lineItem = invTransGroup.FirstOrDefault();
						if (lineItem == null) continue;

					
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
				}

				writer.WriteEndElement(); // </InventoryTransaction>
			}
			
			writer.WriteEndElement(); // </Transaction>
		}

	}
}
