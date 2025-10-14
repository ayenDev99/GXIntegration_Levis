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

		private static void WriteXmlContent(List<StoreGoodsModel> items, XmlWriter writer)
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
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ASN");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "RECEIVING");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

			// Transaction Header Info
			GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, first.TransOrganizationID);
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.TransRetailStoreID);
			GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.TransWorkstationID);
			GlobalOutbound.WriteCDataElement(writer, "TillID", first.TransTillID);
			GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", first.TransSequenceNo);
			GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(first.TransBusinessDayDate));
			GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(first.TransBeginDateTime, true));
			GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(first.TransEndDateTime, true));
			GlobalOutbound.WriteCDataElement(writer, "OperatorID", first.TransOperatorID);
			GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", first.TransCurrencyCode);

			GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
			GlobalOutbound.WritePosTransactionProperties(writer, "REGION", "AMA");
			GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", "PH");
			GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", first.AlternateStoreID);

			// InventoryTransaction Section
			writer.WriteStartElement("InventoryTransaction");

			// ReceiveInventory Section
			writer.WriteStartElement("ReceiveInventory");

			GlobalOutbound.WriteCDataElement(writer, "DocumentStatus", "CLOSED");
			GlobalOutbound.WriteCDataElement(writer, "DocumentID", first.DocumentID);
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.TransRetailStoreID);
			GlobalOutbound.WriteCDataElement(writer, "DocumentTypeDescription", "RECEIVING_ASN");
			GlobalOutbound.WriteCDataElement(writer, "DocumentType", "RECEIVING");
			GlobalOutbound.WriteCDataElement(writer, "DocumentSubType", "ASN");
			GlobalOutbound.WriteCDataElement(writer, "CompletionTimestamp", GlobalOutbound.FormatDate(first.CompletionTimestamp, true));
			GlobalOutbound.WriteCDataElement(writer, "LastActivityTimestamp", GlobalOutbound.FormatDate(first.LastActivityTimestamp, true));

			// Shipment Section
			writer.WriteStartElement("Shipment");
			GlobalOutbound.WriteCDataElement(writer, "ShipmentSequence", first.ShipmentSequence);
			GlobalOutbound.WriteCDataElement(writer, "DestinationRetailLocationID", first.DestinationRetailLocationID);
			GlobalOutbound.WriteCDataElement(writer, "StatusCode", first.ShipmentStatusCode);
			writer.WriteEndElement(); // </Shipment>

			foreach (var item in items)
			{
				//---------------------
				// Carton Section (per item)
				//---------------------
				if (item.SGCarton?.Any() == true)
				{
					// Carton Header
					writer.WriteStartElement("Carton");
					GlobalOutbound.WriteCDataElement(writer, "CartonID", first.CartonID);
					GlobalOutbound.WriteCDataElement(writer, "StatusCode", first.CartonStatusCode);

					foreach (var crtn in item.SGCarton.OrderBy(d => d.ItemID))
					{
						// LineItem inside Carton
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");
						GlobalOutbound.WriteCDataElement(writer, "LineNumber", crtn.LineNumber);
						GlobalOutbound.WriteCDataElement(writer, "ItemID", crtn.ItemID);
						GlobalOutbound.WriteCDataElement(writer, "ActualCount", crtn.ActualCount);
						GlobalOutbound.WriteCDataElement(writer, "ExpectedCount", crtn.ExpectedCount);
						GlobalOutbound.WriteCDataElement(writer, "PostedCount", crtn.PostedCount);

						writer.WriteStartElement("SaleLineItem");
						GlobalOutbound.WriteCDataElement(writer, "RetailLocationID", first.TransRetailStoreID);
						GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.TransWorkstationID);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDate", GlobalOutbound.FormatDate(crtn.SaleLineBusinessDayDate, true));
						GlobalOutbound.WriteCDataElement(writer, "TransactionSequence", crtn.TransactionSequence);
						GlobalOutbound.WriteCDataElement(writer, "LineItemSequence", crtn.LineItemSequence);
						writer.WriteEndElement(); // </SaleLineItem>

						GlobalOutbound.WriteCDataElement(writer, "RecordCreationType", crtn.RecordCreationType);
						GlobalOutbound.WriteCDataElement(writer, "StatusCode", crtn.LineItemStatusCode);
						writer.WriteEndElement(); // </LineItem>
					}

					writer.WriteEndElement(); // </Carton>
				}
				//---------------------
				// Carton Item Section (per item)
				//---------------------
				if (item.SGItems?.Any() == true)
				{
					foreach (var itm in item.SGItems.OrderBy(d => d.ALU))
					{
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");
						GlobalOutbound.WriteCDataElement(writer, "ItemID", itm.ALU);

						GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", itm.PTDIM1);
						GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", itm.PTDIM2);
						GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", itm.PTStyle);
						GlobalOutbound.WriteLineItemProperty(writer, "CONTROL_NUMBER", "STRING", itm.PTControlNumber);
						GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", itm.PTEAN);

						GlobalOutbound.WriteCDataElement(writer, "QuantityOrdered", itm.QuantityOrdered);
						GlobalOutbound.WriteCDataElement(writer, "QuantityReceived", itm.QuantityReceived);
						GlobalOutbound.WriteCDataElement(writer, "CartonNumber", first.CartonID);
						GlobalOutbound.WriteCDataElement(writer, "LineItemNumber", itm.ItemLineNumber);
						GlobalOutbound.WriteCDataElement(writer, "Description", itm.Description);
						writer.WriteEndElement(); // </LineItem>
					}
				}
			}
			writer.WriteEndElement(); // </ReceiveInventory>
			writer.WriteEndElement(); // </InventoryTransaction>
			writer.WriteEndElement(); // </Transaction>
		}

	}
}
