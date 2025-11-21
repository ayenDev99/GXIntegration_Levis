using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundStoreReturn
	{
		public static string GenerateXml(List<StoreReturnModel> items, string filePath, string generate_type)
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

		public static void WriteXmlContent(List<StoreReturnModel> items, XmlWriter writer)
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
			GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, first.OrganizationID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", first.RetailStoreID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "WorkstationID", first.WorkstationID ?? "");
			GlobalOutbound.WriteCDataElement(writer, "TillID", first.TillID ?? "");

			GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", first.SequenceNo);
			GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(first.BusinessDayDate));
			GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(first.BeginDateTime, true));
			GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(first.EndDateTime, true));
			GlobalOutbound.WriteCDataElement(writer, "OperatorID", first.OperatorID);
			GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", first.CurrencyCode);

			// Transaction properties
			GlobalOutbound.WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", first.ReceiptDeliveryMethod);
			GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", first.InventoryMovementSuccess);
			GlobalOutbound.WritePosTransactionProperties(writer, "REGION", first.Region);
			GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", first.Country);
			GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", first.AlternateStoreID);
			GlobalOutbound.WritePosTransactionProperties(writer, "TRANSACTION_CODE", first.TransactionCode);
			GlobalOutbound.WritePosTransactionProperties(writer, "BARCODE", first.Barcode);
			GlobalOutbound.WritePosTransactionProperties(writer, "RETURN_ORIGINAL_ALT_STORE_ID", first.ReturnOriginalAltStoreID);

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
				if (item.ReturnItems?.Any() == true)
				{
					foreach (var itm in item.ReturnItems.OrderBy(d => d.LineItemSequenceNo))
					{
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", itm.LineItemSequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "LineNumber", itm.LineItemLineNumber);
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(itm.LineItemBeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(itm.LineItemEndDateTime, true));

						//---------------------
						// Return Section
						//---------------------
						writer.WriteStartElement("Return");
						writer.WriteAttributeString("ItemType", "Stock");

						GlobalOutbound.WriteCDataElement(writer, "ItemID", itm.SaleItemID);
						GlobalOutbound.WriteCDataElement(writer, "Description", itm.SaleDescription);
						GlobalOutbound.WriteCDataElement(writer, "RegularSalesUnitPrice", itm.SaleRegularSalesUnitPrice);
						GlobalOutbound.WriteCDataElement(writer, "ActualSalesUnitPrice", itm.SaleActualSalesUnitPrice);
						GlobalOutbound.WriteCDataElement(writer, "ExtendedAmount", itm.SaleExtendedAmount);
						GlobalOutbound.WriteCDataElement(writer, "Quantity", itm.SaleQuantity);
						GlobalOutbound.WriteCDataElement(writer, "Reason", itm.SaleReason);
						GlobalOutbound.WriteCDataElement(writer, "ReturnType", itm.SaleReturnType);

						//---------------------
						// Associate Section
						//---------------------
						writer.WriteStartElement("Associate");
						GlobalOutbound.WriteCDataElement(writer, "AssociateID", itm.AssociateID);
						writer.WriteEndElement();

						//---------------------
						// PercentageOfItem Section
						//---------------------
						writer.WriteStartElement("PercentageOfItem");
						GlobalOutbound.WriteCDataElement(writer, "dtv", "AssociateID", GlobalOutbound.NsDtv, itm.AssociateID);
						GlobalOutbound.WriteCDataElement(writer, "Percentage", itm.Percentage);
						writer.WriteEndElement();

						//---------------------
						// Discount Section (per item disc)
						//---------------------
						if (itm.ReturnDiscounts?.Any() == true)
						{
							foreach (var disc in itm.ReturnDiscounts.OrderBy(d => d.DiscSequenceNo))
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
						writer.WriteStartElement("Tax");
						writer.WriteAttributeString("TaxType", "dtv:VAT");
						writer.WriteAttributeString("VoidFlag", "false");
						GlobalOutbound.WriteCDataElement(writer, "TaxAuthority", itm.TaxAuthority);
						GlobalOutbound.WriteCDataElement(writer, "TaxableAmount", itm.TaxableAmount);
						GlobalOutbound.WriteCDataElement(writer, "Amount", itm.Amount);
						GlobalOutbound.WriteCDataElement(writer, "Percent", itm.Percent);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "RawTaxPercentage", GlobalOutbound.NsDtv, itm.RawTaxPercentage);
						GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxLocationId", GlobalOutbound.NsDtv, itm.TaxLocationID ?? "");
						GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxGroupId", GlobalOutbound.NsDtv, itm.TaxGroupID);
						writer.WriteEndElement(); // </Tax>

						//---------------------
						// TransactionLink Section
						//---------------------
						writer.WriteStartElement("TransactionLink");
						writer.WriteAttributeString("ReasonCode", "Return");
						GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", itm.TransLinkRetailStoreID);
						GlobalOutbound.WriteCDataElement(writer, "WorkstationID", itm.TransLinkWorkstationID);
						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", itm.TransLinkSequenceNumber);
						GlobalOutbound.WriteCDataElement(writer, "LineItemSequenceNumber", itm.TransLinkLineItemSequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(itm.TransLinkBusinessDayDate));
						writer.WriteEndElement(); // </TransactionLink>

						//---------------------
						// LineItem Properties Section
						//---------------------
						GlobalOutbound.WriteLineItemProperty(writer, "DEAL_ITEM_PERCENT_OFF", "STRING", itm.DealItemPercentOff);
						GlobalOutbound.WriteLineItemProperty(writer, "ORIGINAL_TLOG_SEQUENCE", "STRING", itm.LineItemOriginalTlogSequence);
						GlobalOutbound.WriteLineItemProperty(writer, "RETURN_ORIGIN_ALT_STORE_ID", "STRING", itm.LineItemReturnOrgAltStoreID);
						GlobalOutbound.WriteLineItemProperty(writer, "6209:41762:", "STRING", itm.LineItemNum);
						GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", itm.PTDIM1);
						GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", itm.PTDIM2);
						GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", itm.PTStyle);
						GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", itm.PTEAN);

						//---------------------
						// Disposal & Disposition Section
						//---------------------
						writer.WriteStartElement("Disposal");
						writer.WriteAttributeString("Method", "ReturnToStock");
						writer.WriteEndElement();

						writer.WriteStartElement("Disposition");
						writer.WriteAttributeString("LocationId", "DEFAULT");
						writer.WriteAttributeString("BucketId", "ON_HAND");
						writer.WriteEndElement();

						//---------------------
						// Merchandise Hierarchy Section
						//---------------------
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "DIVISION", itm.MerchHierarchyDivision);
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "DEPARTMENT", itm.MerchHierarchyDepartment);
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "SUBDEPARTMENT", itm.MerchHierarchySubDepartment);
						GlobalOutbound.WriteMerchandiseHierarchy(writer, "CLASS", itm.MerchHierarchyClass);

						writer.WriteEndElement(); // </Return>
						writer.WriteEndElement(); // </LineItem>
					}
				}

				//---------------------
				// Tender Section (per tender)
				//---------------------
				if (item.ReturnTenders?.Any() == true)
				{
					foreach (var tender in item.ReturnTenders)
					{
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", tender.TenderSequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "LineNumber", tender.TenderLineNumber);
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(tender.TenderBeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(tender.TenderEndDateTime, true));

						writer.WriteStartElement("Tender");
						writer.WriteAttributeString("TenderType", tender.TenderType);
						writer.WriteAttributeString("TypeCode", tender.TypeCode);
						writer.WriteAttributeString("ChangeFlag", tender.ChangeFlag);

						GlobalOutbound.WriteCDataElement(writer, "TenderID", tender.TenderID);

						writer.WriteStartElement("Amount");
						writer.WriteAttributeString("Currency", tender.AmountCurrency);
						writer.WriteCData(tender.TenderAmount);
						writer.WriteEndElement(); // </Amount>

						if (!string.IsNullOrEmpty(tender.TenderAuthorizationNumber))
						{
							GlobalOutbound.WriteLineItemProperty(writer, "AUTHORIZATION NUMBER", "STRING", tender.TenderAuthorizationNumber);
						}

						//---------------------
						// Voucher Section
						//---------------------
						writer.WriteStartElement("Voucher");
						writer.WriteAttributeString("TypeCode", "REFUND");
						writer.WriteElementString("Description", string.Empty);
						GlobalOutbound.WriteCDataElement(writer, "FaceValueAmount", "0");
						GlobalOutbound.WriteCDataElement(writer, "UnspentAmount", "0");
						writer.WriteElementString("CardNumber", string.Empty);
						writer.WriteEndElement(); // </Voucher>

						writer.WriteEndElement(); // </Tender>
						writer.WriteEndElement(); // </LineItem>
					}
				}

				//---------------------
				// Totals Section
				//---------------------
				writer.WriteStartElement("Total");
				writer.WriteAttributeString("TotalType", "TransactionGrandAmount");
				writer.WriteCData(item.TransactionGrandAmount);
				writer.WriteEndElement(); // </Total>

				GlobalOutbound.WriteCDataElement(writer, "RoundedTotal", item.RoundedTotal);
			}

			writer.WriteEndElement(); // </RetailTransaction>
			writer.WriteEndElement(); // </Transaction>
		}

	}
}
