using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
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

		private static void WriteXmlContent(IEnumerable<StoreSaleModel> items, XmlWriter writer)
		{
			//---------------------
			// Transaction Section
			//---------------------
			writer.WriteStartElement("Transaction");
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "RETAIL_SALE");

			var first = items.FirstOrDefault();
			if (first != null)
			{
				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, first.TransOrganizationID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.TransRetailStoreID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.TransWorkstationID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "TillID", first.TransTillID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "dtv", "CashDrawerID", GlobalOutbound.NsDtv, first.TransCashDrawerID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", first.TransSequenceNo ?? "");
				GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(first.TransBusinessDayDate));
				GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(first.TransBeginDateTime, true));
				GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(first.TransEndDateTime, true));
				GlobalOutbound.WriteCDataElement(writer, "OperatorID", first.TransOperatorID ?? "");
				GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", first.TransCurrencyCode ?? "");

				GlobalOutbound.WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", "PAPER");
				GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
				GlobalOutbound.WritePosTransactionProperties(writer, "REGION", "AMA");
				GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", "PH");
				GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", first.TransAlternateStoreID);
				GlobalOutbound.WritePosTransactionProperties(writer, "TRANSACTION_CODE", first.TransTransactionCode);
				GlobalOutbound.WritePosTransactionProperties(writer, "BARCODE", first.TransBarcode);

				writer.WriteStartElement("RetailTransaction");
				writer.WriteAttributeString("TransactionStatus", "Delivered");
				writer.WriteAttributeString("TypeCode", "Transaction");

				//---------------------
				// Item Section
				//---------------------
				foreach (var item in items)
				{
					writer.WriteStartElement("LineItem");
					writer.WriteAttributeString("EntryMethod", "Scanner");
					writer.WriteAttributeString("VoidFlag", "false");

					GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.ItemSequenceNo ?? "");
					GlobalOutbound.WriteCDataElement(writer, "LineNumber", item.ItemLineNumber ?? "");
					GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(item.ItemBeginDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(item.ItemEndDateTime, true));

					writer.WriteStartElement("Sale");
					writer.WriteAttributeString("ItemType", "Stock");

					GlobalOutbound.WriteCDataElement(writer, "ItemID", item.SaleItemID ?? "");
					GlobalOutbound.WriteCDataElement(writer, "Description", item.SaleDescription ?? "");
					GlobalOutbound.WriteCDataElement(writer, "RegularSalesUnitPrice", item.SaleRegularSalesUnitPrice ?? "");
					GlobalOutbound.WriteCDataElement(writer, "ActualSalesUnitPrice", item.SaleActualSalesUnitPrice ?? "");
					GlobalOutbound.WriteCDataElement(writer, "ExtendedAmount", item.SaleExtendedAmount ?? "");
					GlobalOutbound.WriteCDataElement(writer, "Quantity", item.SaleQuantity ?? "");

					GlobalOutbound.WriteMerchandiseHierarchy(writer, "BRAND", item.SaleBrand);
					GlobalOutbound.WriteMerchandiseHierarchy(writer, "CATEGORY", item.SaleCategory);
					GlobalOutbound.WriteMerchandiseHierarchy(writer, "CLASS", item.SaleClass);
					GlobalOutbound.WriteMerchandiseHierarchy(writer, "SUBCLASS", item.SaleSubClass);

					GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedItemID", GlobalOutbound.NsDtv, item.SaleScannedItemID ?? "");
					GlobalOutbound.WriteCDataElement(writer, "GiftReceiptFlag", item.SaleGiftReceiptFlag ?? "");

					//---------------------
					// Discount Section
					//---------------------
					if (item.Discounts != null && item.Discounts.Any())
					{
						foreach (var disc in item.Discounts.OrderBy(d => d.DiscSequenceNo))
						{
							writer.WriteStartElement("RetailPriceModifier");

							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", disc.DiscSequenceNo ?? "");

							writer.WriteStartElement("Amount");
							writer.WriteAttributeString("Action", "Subtract");
							writer.WriteCData(disc.DiscAmount ?? "");
							writer.WriteEndElement(); // </Amount>

							GlobalOutbound.WriteCDataElement(writer, "PromotionID", disc.DiscPromotionID ?? "");
							GlobalOutbound.WriteCDataElement(writer, "ReasonCode", disc.DiscReasonCode ?? "");

							writer.WriteEndElement(); // </RetailPriceModifier>
						}
					}

					//---------------------
					// Tax Section
					//---------------------
					writer.WriteStartElement("Tax");
					writer.WriteAttributeString("TaxType", "Sales");
					writer.WriteAttributeString("dtv", "VoidFlag", GlobalOutbound.NsDtv, "false");

					GlobalOutbound.WriteCDataElement(writer, "TaxAuthority", item.TaxAuthority ?? "");
					GlobalOutbound.WriteCDataElement(writer, "TaxableAmount", item.TaxableAmount ?? "");
					GlobalOutbound.WriteCDataElement(writer, "Amount", item.Amount ?? "");
					GlobalOutbound.WriteCDataElement(writer, "Percent", item.Percent ?? "");
					GlobalOutbound.WriteCDataElement(writer, "dtv", "RawTaxPercentage", GlobalOutbound.NsDtv, item.RawTaxPercentage ?? "");
					GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxLocationId", GlobalOutbound.NsDtv, item.TaxLocationID ?? "");
					GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxGroupId", GlobalOutbound.NsDtv, item.TaxGroupID ?? "");
					writer.WriteEndElement(); // </Tax>

					GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", item.PTDIM1);
					GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", item.PTDIM2);
					GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", item.PTStyle);
					GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", item.PTEAN);

					writer.WriteEndElement(); // </Sale>
					writer.WriteEndElement(); // </LineItem>

					//---------------------
					// Tender Section
					//---------------------
					if (item.Tenders != null && item.Tenders.Any())
					{
						foreach (var tender in item.Tenders)
						{
							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", tender.TenderSequenceNo ?? "");
							GlobalOutbound.WriteCDataElement(writer, "LineNumber", tender.TenderLineNumber ?? "");
							GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(tender.TenderBeginDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(tender.TenderEndDateTime, true));

							writer.WriteStartElement("Tender");
							writer.WriteAttributeString("TenderType", tender.TenderType ?? "");
							writer.WriteAttributeString("TypeCode", tender.TenderTypeCode ?? "");
							writer.WriteAttributeString("ChangeFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "TenderID", tender.TenderID ?? "");

							writer.WriteStartElement("Amount");
							writer.WriteAttributeString("Currency", tender.AmountCurrency ?? "");
							writer.WriteCData(tender.TenderAmount ?? "");
							writer.WriteEndElement(); // </Amount>

							if (tender.TenderAuthorizationNumber != null && tender.TenderAuthorizationNumber.Any())
							{
								GlobalOutbound.WriteLineItemProperty(writer, "AUTHORIZATION NUMBER", "STRING", tender.TenderAuthorizationNumber);
							}

							writer.WriteEndElement(); // </Tender>
							writer.WriteEndElement(); // </LineItem>
						}
					}

					writer.WriteStartElement("Total");
					writer.WriteAttributeString("TotalType", "TransactionGrandAmount");
					writer.WriteCData(item.TransGrandAmount ?? "");
					writer.WriteEndElement(); // </Total>

					GlobalOutbound.WriteCDataElement(writer, "RoundedTotal", item.TransRoundedTotal ?? "");
				}

				writer.WriteEndElement(); // </RetailTransaction>
			}

			writer.WriteEndElement(); // </Transaction>
		}

	}
}
