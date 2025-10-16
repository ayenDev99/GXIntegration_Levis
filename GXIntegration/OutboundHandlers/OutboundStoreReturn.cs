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
	public static class OutboundStoreReturn
	{
		//public static async Task Execute(StoreReturnRepository repository, GXConfig config, string generate_type)
		//{
		//	try
		//	{
		//		DateTime from_date = DateTime.Today; // 00:00:00
		//		DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
		//		//var items = await repository.GetStoreReturnAsync(from_date, to_date);

		//		string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
		//		Directory.CreateDirectory(outboundDir);

		//		string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
		//		string fileName = $"StoreReturn_{timestamp}.xml";
		//		string filePath = Path.Combine(outboundDir, fileName);

		//		//Logger.Log($"EOD StoreReturn downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
		//		//GenerateXml(items, filePath, generate_type);
		//	}
		//	catch (Exception ex)
		//	{
		//		MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//		Logger.Log($"Error: {ex.Message}");
		//	}
		//}

		public static string GenerateXml(List<StoreReturnModel> items, string filePath, string generate_type)
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

		public static void WriteXmlContent(List<StoreReturnModel> items, XmlWriter writer)
		{
			writer.WriteStartElement("Transaction", GlobalOutbound.NsIXRetail); // Transaction
			
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "RETAIL_SALE");

			// Group by OrganizationID
			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.OrganizationID))
			{
				var itemStore = storeGroup.FirstOrDefault();
				if (itemStore == null) continue;

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, itemStore.OrganizationID);
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", itemStore.RetailStoreID);

				// Group by WorkstationID
				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationID))
				{
					var itemWs = wsGroup.FirstOrDefault();
					if (itemWs == null) continue;

					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", itemWs.WorkstationID);
					GlobalOutbound.WriteCDataElement(writer, "TillID", itemWs.TillID);

					// Group by SequenceNo (transactions)
					foreach (var transGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
					{
						var transactionItems = transGroup.FirstOrDefault();
						if (transactionItems == null) continue;

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", transactionItems.SequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(transactionItems.BusinessDayDate));
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(transactionItems.BeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(transactionItems.EndDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "OperatorID", transactionItems.OperatorID);
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", transactionItems.CurrencyCode);

						// Transaction properties
						GlobalOutbound.WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", transactionItems.ReceiptDeliveryMethod);
						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", transactionItems.InventoryMovementSuccess);
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", transactionItems.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", transactionItems.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", transactionItems.AlternateStoreID);
						GlobalOutbound.WritePosTransactionProperties(writer, "TRANSACTION_CODE", transactionItems.TransactionCode);
						GlobalOutbound.WritePosTransactionProperties(writer, "BARCODE", transactionItems.Barcode);
						GlobalOutbound.WritePosTransactionProperties(writer, "RETURN_ORIGINAL_ALT_STORE_ID", transactionItems.ReturnOriginalAltStoreID);

						// <RetailTransaction>
						writer.WriteStartElement("RetailTransaction");
						writer.WriteAttributeString("TransactionStatus", "Delivered");
						writer.WriteAttributeString("TypeCode", "Transaction");

						// Group by LineItemSequenceNo
						foreach (var itemGroup in GlobalOutbound.GroupBySafe(transGroup, i => i.LineItemSequenceNo))
						{
							var lineItems = itemGroup.FirstOrDefault();
							if (lineItems == null) continue;

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", lineItems.LineItemSequenceNo);
							GlobalOutbound.WriteCDataElement(writer, "LineNumber", lineItems.LineItemLineNumber);
							GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(lineItems.LineItemBeginDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(lineItems.LineItemEndDateTime, true));

							// <Return>
							writer.WriteStartElement("Return");
							writer.WriteAttributeString("ItemType", "Stock");

							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItems.SaleItemID);
							GlobalOutbound.WriteCDataElement(writer, "Description", lineItems.SaleDescription);
							GlobalOutbound.WriteCDataElement(writer, "RegularSalesUnitPrice", lineItems.SaleRegularSalesUnitPrice);
							GlobalOutbound.WriteCDataElement(writer, "ActualSalesUnitPrice", lineItems.SaleActualSalesUnitPrice);
							GlobalOutbound.WriteCDataElement(writer, "ExtendedAmount", lineItems.SaleExtendedAmount);
							GlobalOutbound.WriteCDataElement(writer, "Quantity", lineItems.SaleQuantity);
							GlobalOutbound.WriteCDataElement(writer, "Reason", lineItems.SaleReason);
							GlobalOutbound.WriteCDataElement(writer, "ReturnType", lineItems.SaleReturnType);

							// Associate
							writer.WriteStartElement("Associate");
							GlobalOutbound.WriteCDataElement(writer, "AssociateID", lineItems.AssociateID);
							writer.WriteEndElement();

							// PercentageOfItem
							writer.WriteStartElement("PercentageOfItem");
							GlobalOutbound.WriteCDataElement(writer, "dtv", "AssociateID", GlobalOutbound.NsDtv, lineItems.AssociateID);
							GlobalOutbound.WriteCDataElement(writer, "Percentage", lineItems.Percentage);
							writer.WriteEndElement();

							// Tax
							writer.WriteStartElement("Tax");
							writer.WriteAttributeString("TaxType", "Sales");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "TaxAuthority", lineItems.TaxAuthority);
							GlobalOutbound.WriteCDataElement(writer, "TaxableAmount", lineItems.TaxableAmount);
							GlobalOutbound.WriteCDataElement(writer, "Amount", lineItems.Amount);
							GlobalOutbound.WriteCDataElement(writer, "Percent", lineItems.Percent);
							GlobalOutbound.WriteCDataElement(writer, "dtv", "RawTaxPercentage", GlobalOutbound.NsDtv, lineItems.RawTaxPercentage);

							writer.WriteStartElement("dtv", "TaxLocationId", GlobalOutbound.NsDtv);
							writer.WriteEndElement();
							GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxGroupId", GlobalOutbound.NsDtv, lineItems.TaxGroupID);

							writer.WriteEndElement(); // </Tax>

							// TransactionLink
							writer.WriteStartElement("TransactionLink");
							writer.WriteAttributeString("ReasonCode", "Return");
							GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", transactionItems.TransLinkRetailStoreID);
							GlobalOutbound.WriteCDataElement(writer, "WorkstationID", transactionItems.TransLinkWorkstationID);
							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", transactionItems.TransLinkSequenceNumber);
							GlobalOutbound.WriteCDataElement(writer, "LineItemSequenceNumber", transactionItems.TransLinkLineItemSequenceNo);
							GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(transactionItems.TransLinkBusinessDayDate));
							writer.WriteEndElement(); // </TransactionLink>

							// LineItem Properties
							GlobalOutbound.WriteLineItemProperty(writer, "DEAL_ITEM_PERCENT_OFF", "STRING", lineItems.DealItemPercentOff);
							GlobalOutbound.WriteLineItemProperty(writer, "ORIGINAL_TLOG_SEQUENCE", "STRING", lineItems.LineItemOriginalTlogSequence);
							GlobalOutbound.WriteLineItemProperty(writer, "RETURN_ORIGIN_ALT_STORE_ID", "STRING", lineItems.LineItemReturnOrgAltStoreID);
							GlobalOutbound.WriteLineItemProperty(writer, "6209:41762:", "STRING", lineItems.LineItemNum);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItems.PTDIM1);
							GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItems.PTDIM2);
							GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItems.PTStyle);
							GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItems.PTEAN);

							// Disposal & Disposition
							writer.WriteStartElement("Disposal");
							writer.WriteAttributeString("Method", "ReturnToStock");
							writer.WriteEndElement();

							writer.WriteStartElement("Disposition");
							writer.WriteAttributeString("LocationId", "DEFAULT");
							writer.WriteAttributeString("BucketId", "ON_HAND");
							writer.WriteEndElement();

							// Merchandise Hierarchy
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "DIVISION", lineItems.MerchHierarchyDivision);
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "DEPARTMENT", lineItems.MerchHierarchyDepartment);
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "SUBDEPARTMENT", lineItems.MerchHierarchySubDepartment);
							GlobalOutbound.WriteMerchandiseHierarchy(writer, "CLASS", lineItems.MerchHierarchyClass);

							writer.WriteEndElement(); // </Return>
							writer.WriteEndElement(); // </LineItem>
						}

						// Tender
						writer.WriteStartElement("LineItem");
						writer.WriteAttributeString("VoidFlag", "false");

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", transactionItems.TenderSequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "LineNumber", transactionItems.TenderLineNumber);
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(transactionItems.TenderBeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(transactionItems.TenderEndDateTime, true));

						writer.WriteStartElement("Tender");
						writer.WriteAttributeString("TenderType", transactionItems.TenderType);
						writer.WriteAttributeString("TypeCode", transactionItems.TypeCode);
						writer.WriteAttributeString("ChangeFlag", transactionItems.ChangeFlag);

						GlobalOutbound.WriteCDataElement(writer, "TenderID", transactionItems.TenderID);

						writer.WriteStartElement("Amount");
						writer.WriteAttributeString("Currency", transactionItems.AmountCurrency);
						writer.WriteCData(transactionItems.TenderAmount);
						writer.WriteEndElement(); // </Amount>

						writer.WriteStartElement("Voucher");
						writer.WriteAttributeString("TypeCode", "REFUND");
						writer.WriteElementString("Description", string.Empty);
						GlobalOutbound.WriteCDataElement(writer, "FaceValueAmount", "0");
						GlobalOutbound.WriteCDataElement(writer, "UnspentAmount", "0");
						writer.WriteElementString("CardNumber", string.Empty);
						writer.WriteEndElement(); // </Voucher>

						writer.WriteEndElement(); // </Tender>
						writer.WriteEndElement(); // </LineItem>

						// Transaction totals
						writer.WriteStartElement("Total");
						writer.WriteAttributeString("TotalType", "TransactionGrandAmount");
						writer.WriteCData(transactionItems.TransactionGrandAmount);
						writer.WriteEndElement();

						GlobalOutbound.WriteCDataElement(writer, "RoundedTotal", transactionItems.RoundedTotal);

						writer.WriteEndElement(); // </RetailTransaction>
					}
				}
			}

			writer.WriteEndElement(); // </Transaction>

		}

	}
}
