using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using GXIntegration_Levis.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundStoreSale
	{
		public static async Task Execute(StoreSaleRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				DateTime from_date = DateTime.Today; // 00:00:00
				DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
				//var items = await repository.GetStoreSaleAsync(from_date, to_date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreSale_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				//Logger.Log($"EOD StoreSale downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
				//GenerateXml(items, filePath, generate_type);

				//MessageBox.Show($"RETAIL SALE synced.\nSaved to: {outboundDir}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreSaleModel> items, string filePath, string generate_type)
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

		private static void WriteXmlContent(List<StoreSaleModel> items, XmlWriter writer)
		{
			writer.WriteStartElement("Transaction", GlobalOutbound.NsIXRetail); // <Transaction>

			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "RETAIL_SALE");

			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.OrganizationID))
			{
				var itemStore = storeGroup.FirstOrDefault();
				if (itemStore == null) continue;

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, itemStore.OrganizationID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", itemStore.RetailStoreID ?? "");

				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationID))
				{
					var itemWs = wsGroup.FirstOrDefault();
					if (itemWs == null) continue;

					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", itemWs.WorkstationID ?? "");
					GlobalOutbound.WriteCDataElement(writer, "TillID", itemWs.TillID ?? "");

					foreach (var transGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
					{
						var transactionItems = transGroup.FirstOrDefault();
						if (transactionItems == null) continue;

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", transactionItems.SequenceNo ?? "");
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(transactionItems.BusinessDayDate));
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(transactionItems.BeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(transactionItems.EndDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "OperatorID", transactionItems.OperatorID ?? "");
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", transactionItems.CurrencyCode ?? "");

						GlobalOutbound.WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", transactionItems.ReceiptDeliveryMethod);
						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", transactionItems.InventoryMovementSuccess);
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", transactionItems.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", transactionItems.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", transactionItems.AlternateStoreID);
						GlobalOutbound.WritePosTransactionProperties(writer, "TRANSACTION_CODE", transactionItems.TransactionCode);
						GlobalOutbound.WritePosTransactionProperties(writer, "BARCODE", transactionItems.Barcode);

						writer.WriteStartElement("RetailTransaction");
						writer.WriteAttributeString("TransactionStatus", "Delivered");
						writer.WriteAttributeString("TypeCode", "Transaction");

						// === SALE LineItem(s) ===
						foreach (var itemGroup in GlobalOutbound.GroupBySafe(transGroup, i => i.LineItemSequenceNo))
						{
							var lineItems = itemGroup.FirstOrDefault();
							if (lineItems == null) continue;

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("EntryMethod", "Scanner"); // fixed value
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", lineItems.LineItemSequenceNo ?? "");
							GlobalOutbound.WriteCDataElement(writer, "LineNumber", lineItems.LineItemLineNumber ?? "");
							GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(lineItems.LineItemBeginDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(lineItems.LineItemEndDateTime, true));

							writer.WriteStartElement("Sale");
							writer.WriteAttributeString("ItemType", "Stock");

							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItems.SaleItemID ?? "");
							GlobalOutbound.WriteCDataElement(writer, "Description", lineItems.SaleDescription ?? "");
							GlobalOutbound.WriteCDataElement(writer, "RegularSalesUnitPrice", lineItems.SaleRegularSalesUnitPrice ?? "");
							GlobalOutbound.WriteCDataElement(writer, "ActualSalesUnitPrice", lineItems.SaleActualSalesUnitPrice ?? "");
							GlobalOutbound.WriteCDataElement(writer, "ExtendedAmount", lineItems.SaleExtendedAmount ?? "");
							GlobalOutbound.WriteCDataElement(writer, "Quantity", lineItems.SaleQuantity ?? "");

							GlobalOutbound.WriteMerchandiseHierarchy(writer, "DIVISION", lineItems.Division);
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "DEPARTMENT", lineItems.Department);
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "SUBDEPARTMENT", lineItems.SubDepartment);
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "CLASS", lineItems.Class);

							GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedItemID", GlobalOutbound.NsDtv, lineItems.ScannedItemID ?? "");
							GlobalOutbound.WriteCDataElement(writer, "GiftReceiptFlag", lineItems.GiftReceiptFlag ?? "");

							writer.WriteStartElement("Associate");
							GlobalOutbound.WriteCDataElement(writer, "AssociateID", lineItems.AssociateID ?? "");
							writer.WriteEndElement(); // </Associate>

							writer.WriteStartElement("dtv", "PercentageOfItem", GlobalOutbound.NsDtv);
							GlobalOutbound.WriteCDataElement(writer, "dtv", "AssociateID", GlobalOutbound.NsDtv, lineItems.AssociateID ?? "");
							GlobalOutbound.WriteCDataElement(writer, "dtv", "Percentage", GlobalOutbound.NsDtv, lineItems.Percentage ?? "");
							writer.WriteEndElement(); // </dtv:PercentageOfItem>

							writer.WriteStartElement("Tax");
							writer.WriteAttributeString("TaxType", "Sales");
							writer.WriteAttributeString("dtv", "VoidFlag", GlobalOutbound.NsDtv, "false");

							GlobalOutbound.WriteCDataElement(writer, "TaxAuthority", lineItems.TaxAuthority ?? "");
							GlobalOutbound.WriteCDataElement(writer, "TaxableAmount", lineItems.TaxableAmount ?? "");
							GlobalOutbound.WriteCDataElement(writer, "Amount", lineItems.Amount ?? "");
							GlobalOutbound.WriteCDataElement(writer, "Percent", lineItems.Percent ?? "");
							GlobalOutbound.WriteCDataElement(writer, "dtv", "RawTaxPercentage", GlobalOutbound.NsDtv, lineItems.RawTaxPercentage ?? "");

							// Write empty tag only if schema allows it
							writer.WriteElementString("dtv", "TaxLocationId", GlobalOutbound.NsDtv, "");

							GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxGroupId", GlobalOutbound.NsDtv, lineItems.TaxGroupID ?? "");
							writer.WriteEndElement(); // </Tax>

							GlobalOutbound.WriteLineItemProperty(writer, "DEAL_ITEM_PERCENT_OFF", "STRING", lineItems.DealItemPercentOff);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItems.PTDIM1);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItems.PTDIM2);
							GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItems.PTStyle);
							GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItems.PTEAN);

							writer.WriteEndElement(); // </Sale>
							writer.WriteEndElement(); // </LineItem>
						}

						// === TENDER LineItem ===
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", transactionItems.TenderSequenceNo ?? "");
						GlobalOutbound.WriteCDataElement(writer, "LineNumber", transactionItems.TenderLineNumber ?? "");
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(transactionItems.TenderBeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(transactionItems.TenderEndDateTime, true));

						writer.WriteStartElement("Tender");
						writer.WriteAttributeString("TenderType", transactionItems.TenderType ?? "");
						writer.WriteAttributeString("TypeCode", transactionItems.TypeCode ?? "");
						writer.WriteAttributeString("ChangeFlag", transactionItems.ChangeFlag ?? "");

						GlobalOutbound.WriteCDataElement(writer, "TenderID", transactionItems.TenderID ?? "");

						writer.WriteStartElement("Amount");
						writer.WriteAttributeString("Currency", transactionItems.AmountCurrency ?? "");
						writer.WriteCData(transactionItems.TenderAmount ?? "");
						writer.WriteEndElement(); // </Amount>

						writer.WriteEndElement(); // </Tender>
						writer.WriteEndElement(); // </LineItem>

						// Totals
						writer.WriteStartElement("Total");
						writer.WriteAttributeString("TotalType", "TransactionGrandAmount");
						writer.WriteCData(transactionItems.TransactionGrandAmount ?? "");
						writer.WriteEndElement(); // </Total>

						GlobalOutbound.WriteCDataElement(writer, "RoundedTotal", transactionItems.RoundedTotal ?? "");

						writer.WriteEndElement(); // </RetailTransaction>
					}
				}
			}

			writer.WriteEndElement(); // </Transaction>
			
		}

	}
}
