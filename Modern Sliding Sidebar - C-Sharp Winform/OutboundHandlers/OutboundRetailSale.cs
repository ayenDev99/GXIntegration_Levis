using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
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
	public static class OutboundRetailSale
	{
		public static async Task Execute(SalesRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var receipt_type = new List<int> { 0, 2 };
				var items = await repository.GetSalesAsync(date, receipt_type);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreSale_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath);

				MessageBox.Show($"Retail Sale synced.\nSaved to: {outboundDir}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}

		public static void GenerateXml(List<SalesModel> items, string filePath)
		{
			var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// <POSLog> with namespaces
				writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
				writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
				writer.WriteAttributeString("dtv", GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xs", GlobalOutbound.NsXsi);
				writer.WriteAttributeString("schemaLocation", GlobalOutbound.NsIXRetail + " POSLog.xsd");

				// <Transaction>
				writer.WriteStartElement("Transaction", GlobalOutbound.NsIXRetail);
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "RETAIL_SALE");

				// <dtv:OrganizationID>
				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");

				// Group by store, workstation, transaction
				foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.StoreNo))
				{
					GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);

					foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationNo))
					{
						GlobalOutbound.WriteCDataElement(writer, "WorkstationID", wsGroup.Key);
						GlobalOutbound.WriteCDataElement(writer, "TillID", storeGroup.Key + wsGroup.Key);

						foreach (var transGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.DocNo))
						{
							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", transGroup.Key);

							var transactionItems = transGroup.FirstOrDefault();
							GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(transactionItems?.CreatedDateTime));
							GlobalOutbound.WriteCDataElement(writer, "BeginDateTime",	GlobalOutbound.FormatDate(transactionItems?.CreatedDateTime, true));
							GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(transactionItems?.InvcPostDate, true));
							GlobalOutbound.WriteCDataElement(writer, "OperatorID", (transactionItems != null ? transactionItems.CashierLoginName : ""));
							GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", (transactionItems != null ? transactionItems.CurrencyCode : ""));

							GlobalOutbound.WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", "PAPER");
							GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
							GlobalOutbound.WritePosTransactionProperties(writer, "REGION", "AMA");
							GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", "PH");
							GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", (transactionItems != null ? transactionItems.AlternateStoreId : ""));
							GlobalOutbound.WritePosTransactionProperties(writer, "TRANSACTION_CODE", (transactionItems != null ? transactionItems.TransactionCode : ""));
							GlobalOutbound.WritePosTransactionProperties(writer, "BARCODE", (transactionItems != null ? transactionItems.Barcode : ""));

							writer.WriteStartElement("RetailTransaction");
							writer.WriteAttributeString("TransactionStatus", "Delivered");
							writer.WriteAttributeString("TypeCode", "Transaction");

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("EntryMethod", "dtv:ScannerScanner");
							writer.WriteAttributeString("VoidFlag", "false");

							foreach (var itemGroup in GlobalOutbound.GroupBySafe(transGroup, i => i.ItemSequenceNumber))
							{
								var itemItems = itemGroup.FirstOrDefault(); // corrected: use itemGroup, not transGroup
								GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", (itemItems != null ? itemItems.SequenceNumber : ""));
								GlobalOutbound.WriteCDataElement(writer, "LineNumber", (itemItems != null ? itemItems.SequenceNumber : ""));
								GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(transactionItems?.CreatedDateTime));
								GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(transactionItems?.EndDateTime));

								writer.WriteStartElement("Sale");
								writer.WriteAttributeString("ItemType", "Stock");

								GlobalOutbound.WriteCDataElement(writer, "ItemID", (itemItems != null ? itemItems.ItemId : ""));
								GlobalOutbound.WriteCDataElement(writer, "Description", (itemItems != null ? itemItems.Description : ""));
								GlobalOutbound.WriteCDataElement(writer, "RegularSalesUnitPrice", (itemItems != null ? itemItems.RegularPrice : ""));
								GlobalOutbound.WriteCDataElement(writer, "ActualSalesUnitPrice", (itemItems != null ? itemItems.ActualPrice : ""));
								GlobalOutbound.	WriteCDataElement(writer, "ExtendedAmount", (itemItems != null ? itemItems.ExtendedAmount : ""));
								GlobalOutbound.WriteCDataElement(writer, "Quantity", (itemItems != null ? itemItems.Quantity : ""));

								GlobalOutbound.WriteMerchandiseHierarchy(writer, "DIVISION", "10");
								GlobalOutbound.WriteMerchandiseHierarchy(writer, "DEPARTMENT", "00674");
								GlobalOutbound.WriteMerchandiseHierarchy(writer, "SUBDEPARTMENT", "00054");
								GlobalOutbound.	WriteMerchandiseHierarchy(writer, "CLASS", "02");

								GlobalOutbound.WriteCDataElement(writer, "dtv", "ScannedItemID", GlobalOutbound.NsDtv, "5401157298299");
								GlobalOutbound.WriteCDataElement(writer, "GiftReceiptFlag", "false");

								writer.WriteStartElement("Associate");
								GlobalOutbound.WriteCDataElement(writer, "AssociateID", "6794");
								writer.WriteEndElement();

								writer.WriteStartElement("dtv", "PercentageOfItem", GlobalOutbound.NsDtv);
								GlobalOutbound.WriteCDataElement(writer, "dtv", "AssociateID", GlobalOutbound.NsDtv, "6794");
								GlobalOutbound.WriteCDataElement(writer, "dtv", "Percentage", GlobalOutbound.NsDtv, "1");
								writer.WriteEndElement();

								writer.WriteStartElement("Tax");
								writer.WriteAttributeString("TaxType", "Sales");
								writer.WriteAttributeString("dtv", "VoidFlag", GlobalOutbound.NsDtv, "false");

								GlobalOutbound.WriteCDataElement(writer, "TaxAuthority", "TR_VAT");
								GlobalOutbound.WriteCDataElement(writer, "TaxableAmount", "0.00");
								GlobalOutbound.WriteCDataElement(writer, "Amount", "0.00");
								GlobalOutbound.WriteCDataElement(writer, "Percent", "0.00");
								GlobalOutbound.WriteCDataElement(writer, "dtv", "RawTaxPercentage", GlobalOutbound.NsDtv, "0.00");

								writer.WriteStartElement("dtv", "TaxLocationId", GlobalOutbound.NsDtv);
								writer.WriteEndElement();

								GlobalOutbound.WriteCDataElement(writer, "dtv", "TaxGroupId", GlobalOutbound.NsDtv, "3");

								writer.WriteEndElement(); // </Tax>

								GlobalOutbound.WriteLineItemProperty(writer, "DEAL_ITEM_PERCENT_OFF", "STRING", "yes");
								GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", "32");
								GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", "30");
								GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", "005013603");
								GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", "5401157298299");

							}

							writer.WriteEndElement(); // </Sale>
							writer.WriteEndElement(); // </LineItem>

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", "2");
							GlobalOutbound.WriteCDataElement(writer, "LineNumber", "0");
							GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", "2025-02-21T10:16:31.97");
							GlobalOutbound.WriteCDataElement(writer, "EndDateTime", "2025-02-21T10:16:45.64");

							writer.WriteStartElement("Tender");
							writer.WriteAttributeString("TenderType", "CREDIT");
							writer.WriteAttributeString("TypeCode", "SALE");
							writer.WriteAttributeString("ChangeFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "TenderID", "TR0111-1");

							writer.WriteStartElement("Amount");
							writer.WriteAttributeString("Currency", "TRY");
							writer.WriteCData("2449.9000");
							writer.WriteEndElement(); // </Amount>

							writer.WriteEndElement(); // </Tender>
							writer.WriteEndElement(); // </LineItem>

							writer.WriteStartElement("Total");
							writer.WriteAttributeString("TotalType", "TransactionGrandAmount");
							writer.WriteCData("2449.9000");
							writer.WriteEndElement();

							GlobalOutbound.WriteCDataElement(writer, "RoundedTotal", "0.0000");
						}
					}
				}

				writer.WriteEndElement(); // </RetailTransaction>
				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndDocument(); // </POSLog>
			}
		}

	}
}
