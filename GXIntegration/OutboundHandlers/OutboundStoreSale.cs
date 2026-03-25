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
			var first = items.FirstOrDefault();
			if (first == null) return;

			//---------------------
			// Transaction Section
			//---------------------
			writer.WriteStartElement("Transaction", GlobalOutbound.NsIXRetail);
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "RETAIL_SALE");

			// Transaction Header Info
			GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, first.TransOrganizationID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.TransRetailStoreID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.TransWorkstationID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "TillID", first.TransTillID ?? "");

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

			//---------------------
			// RetailTransaction Section
			//---------------------
			writer.WriteStartElement("RetailTransaction");
			writer.WriteAttributeString("TransactionStatus", "Delivered");
			writer.WriteAttributeString("TypeCode", "Transaction");

			//---------------------
			// Line Items (Sales)
			//---------------------
			foreach (var item in items)
			{
				//---------------------
				// Invn Items Section (per item)
				//---------------------
				if (item.Items?.Any() == true)
				{
					foreach (var itm in item.Items.OrderBy(d => d.ItemSequenceNo))
					{
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("EntryMethod", "Scanner");
						writer.WriteAttributeString("VoidFlag", "false");

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", itm.ItemSequenceNo ?? "");
						GlobalOutbound.WriteCDataElement(writer, "LineNumber", itm.ItemLineNumber ?? "");
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(itm.ItemBeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(itm.ItemEndDateTime, true));

						//---------------------
						// Sale Section
						//---------------------
						writer.WriteStartElement("Sale");

						// indentify in item is non-merchandise or stock
						if (itm.NonInvnFlag1 == 1 || itm.NonInvnFlag2 == 7) {
							writer.WriteAttributeString("ItemType", "dtv:GiftCertificate");
						} else {
							writer.WriteAttributeString("ItemType", "Stock");
						}

						GlobalOutbound.WriteCDataElement(writer, "ItemID", itm.SaleItemID ?? "");
						GlobalOutbound.WriteCDataElement(writer, "Description", itm.SaleDescription ?? "");
						GlobalOutbound.WriteCDataElement(writer, "RegularSalesUnitPrice", itm.SaleRegularSalesUnitPrice ?? "");
						GlobalOutbound.WriteCDataElement(writer, "ActualSalesUnitPrice", itm.SaleActualSalesUnitPrice ?? "");
						GlobalOutbound.WriteCDataElement(writer, "ExtendedAmount", itm.SaleExtendedAmount ?? "");
						GlobalOutbound.WriteCDataElement(writer, "Quantity", itm.SaleQuantity ?? "");

						GlobalOutbound.WriteMerchandiseHierarchy(writer, "BRAND", itm.SaleBrand);
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "CATEGORY", itm.SaleCategory);
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "CLASS", itm.SaleClass);
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "SUBCLASS", itm.SaleSubClass);

						GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedItemID", GlobalOutbound.NsDtv, itm.SaleScannedItemID ?? "");
						GlobalOutbound.WriteCDataElement(writer, "GiftReceiptFlag", itm.SaleGiftReceiptFlag ?? "");

						//---------------------
						// Discount Section (per item disc)
						//---------------------
						if (itm.Discounts?.Any() == true)
						{
							foreach (var disc in itm.Discounts.OrderBy(d => d.DiscSequenceNo))
							{
								writer.WriteStartElement("RetailPriceModifier");
								writer.WriteAttributeString("MethodCode", "Promotion");
								writer.WriteAttributeString("VoidFlag", "false");

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
						if (itm.NonInvnFlag1 != 1 || itm.NonInvnFlag2 != 7)
						{
                            writer.WriteStartElement("Tax");
                            writer.WriteAttributeString("TaxType", "dtv:VAT");
                            writer.WriteAttributeString("dtv", "VoidFlag", GlobalOutbound.NsDtv, "false");

                            GlobalOutbound.WriteCDataElement(writer, "TaxAuthority", itm.TaxAuthority ?? "");
                            GlobalOutbound.WriteCDataElement(writer, "TaxableAmount", itm.TaxableAmount ?? "");
                            GlobalOutbound.WriteCDataElement(writer, "Amount", itm.Amount ?? "");
                            GlobalOutbound.WriteCDataElement(writer, "Percent", itm.Percent ?? "");
                            GlobalOutbound.WriteCDataElement(writer, "dtv", "RawTaxPercentage", GlobalOutbound.NsDtv, itm.RawTaxPercentage ?? "");
                            GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxLocationId", GlobalOutbound.NsDtv, itm.TaxLocationID ?? "");
                            GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxGroupId", GlobalOutbound.NsDtv, itm.TaxGroupID ?? "");

                            writer.WriteEndElement(); // </Tax>
                        }
                           
						GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", itm.PTDIM1);
						GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", itm.PTDIM2);
						GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", itm.PTStyle);
						GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", itm.PTEAN);
						writer.WriteEndElement(); // </LineItem>

						writer.WriteEndElement(); // </Sale>
					}
				}

				//---------------------
				// Tender Section (per tender)
				//---------------------
				if (item.Tenders?.Any() == true)
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

						if (!string.IsNullOrEmpty(tender.TenderAuthorizationNumber))
						{
							GlobalOutbound.WriteLineItemProperty(writer, "AUTHORIZATION NUMBER", "STRING", tender.TenderAuthorizationNumber);
						}

						writer.WriteEndElement(); // </Tender>
						writer.WriteEndElement(); // </LineItem>
					}
				}

				//---------------------
				// Totals Section
				//---------------------
				writer.WriteStartElement("Total");
				writer.WriteAttributeString("TotalType", "TransactionGrandAmount");
				writer.WriteCData(item.TransGrandAmount ?? "");
				writer.WriteEndElement(); // </Total>

				GlobalOutbound.WriteCDataElement(writer, "RoundedTotal", item.TransRoundedTotal ?? "");
			}

			writer.WriteEndElement(); // </RetailTransaction>
			writer.WriteEndElement(); // </Transaction>
		}

	}
}
