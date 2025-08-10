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
		private static readonly string NsIXRetail = "http://www.nrf-arts.org/IXRetail/namespace/";
		private static readonly string NsDtv = "http://www.datavantagecorp.com/xstore/";
		private static readonly string NsXsi = "http://www.w3.org/2001/XMLSchema-instance";

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
			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8
			};

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// <POSLog> with namespaces
				writer.WriteStartElement("POSLog", NsIXRetail);
				writer.WriteAttributeString("xmlns", "dtv", null, NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, NsXsi);
				writer.WriteAttributeString("dtv", NsDtv);
				writer.WriteAttributeString("xs", NsXsi);
				writer.WriteAttributeString("schemaLocation", NsIXRetail + " POSLog.xsd");

				// <Transaction>
				writer.WriteStartElement("Transaction", NsIXRetail);
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "TransactionType", NsDtv, "RETAIL_SALE");

				// <dtv:OrganizationID>
				WriteCDataElement(writer, "dtv", "OrganizationID", NsDtv, "1");

				// Group by store, workstation, transaction
				foreach (var storeGroup in GroupBySafe(items, i => i.StoreNo))
				{
					WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);

					foreach (var wsGroup in GroupBySafe(storeGroup, i => i.WorkstationNo))
					{
						WriteCDataElement(writer, "WorkstationID", wsGroup.Key);
						WriteCDataElement(writer, "TillID", storeGroup.Key + wsGroup.Key);

						foreach (var transGroup in GroupBySafe(wsGroup, i => i.DocNo))
						{
							WriteCDataElement(writer, "SequenceNumber", transGroup.Key);

							var transactionItems = transGroup.FirstOrDefault();
							WriteCDataElement(writer, "BusinessDayDate", FormatDate(transactionItems?.CreatedDateTime));
							WriteCDataElement(writer, "BeginDateTime", FormatDate(transactionItems?.CreatedDateTime, true));
							WriteCDataElement(writer, "EndDateTime", FormatDate(transactionItems?.InvcPostDate, true));
							WriteCDataElement(writer, "OperatorID", (transactionItems != null ? transactionItems.CashierLoginName : ""));
							WriteCDataElement(writer, "CurrencyCode", (transactionItems != null ? transactionItems.CurrencyCode : ""));

							WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", "PAPER");
							WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
							WritePosTransactionProperties(writer, "REGION", "AMA");
							WritePosTransactionProperties(writer, "COUNTRY", "PH");
							WritePosTransactionProperties(writer, "ALTERNATE_STOREID", (transactionItems != null ? transactionItems.AlternateStoreId : ""));
							WritePosTransactionProperties(writer, "TRANSACTION_CODE", (transactionItems != null ? transactionItems.TransactionCode : ""));
							WritePosTransactionProperties(writer, "BARCODE", (transactionItems != null ? transactionItems.Barcode : ""));

							writer.WriteStartElement("RetailTransaction");
							writer.WriteAttributeString("TransactionStatus", "Delivered");
							writer.WriteAttributeString("TypeCode", "Transaction");

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("EntryMethod", "dtv:ScannerScanner");
							writer.WriteAttributeString("VoidFlag", "false");

							foreach (var itemGroup in GroupBySafe(transGroup, i => i.ItemSequenceNumber))
							{
								var itemItems = itemGroup.FirstOrDefault(); // corrected: use itemGroup, not transGroup
								WriteCDataElement(writer, "SequenceNumber", (itemItems != null ? itemItems.SequenceNumber : ""));
								WriteCDataElement(writer, "LineNumber", (itemItems != null ? itemItems.SequenceNumber : ""));
								WriteCDataElement(writer, "BeginDateTime", FormatDate(transactionItems?.CreatedDateTime));
								WriteCDataElement(writer, "EndDateTime", FormatDate(transactionItems?.EndDateTime));

								writer.WriteStartElement("Sale");
								writer.WriteAttributeString("ItemType", "Stock");

								WriteCDataElement(writer, "ItemID", (itemItems != null ? itemItems.ItemId : ""));
								WriteCDataElement(writer, "Description", (itemItems != null ? itemItems.Description : ""));
								WriteCDataElement(writer, "RegularSalesUnitPrice", (itemItems != null ? itemItems.RegularPrice : ""));
								WriteCDataElement(writer, "ActualSalesUnitPrice", (itemItems != null ? itemItems.ActualPrice : ""));
								WriteCDataElement(writer, "ExtendedAmount", (itemItems != null ? itemItems.ExtendedAmount : ""));
								WriteCDataElement(writer, "Quantity", (itemItems != null ? itemItems.Quantity : ""));

								WriteMerchandiseHierarchy(writer, "DIVISION", "10");
								WriteMerchandiseHierarchy(writer, "DEPARTMENT", "00674");
								WriteMerchandiseHierarchy(writer, "SUBDEPARTMENT", "00054");
								WriteMerchandiseHierarchy(writer, "CLASS", "02");

								WriteCDataElement(writer, "dtv", "ScannedItemID", NsDtv, "5401157298299");
								WriteCDataElement(writer, "GiftReceiptFlag", "false");

								writer.WriteStartElement("Associate");
								WriteCDataElement(writer, "AssociateID", "6794");
								writer.WriteEndElement();

								writer.WriteStartElement("dtv", "PercentageOfItem", NsDtv);
								WriteCDataElement(writer, "dtv", "AssociateID", NsDtv, "6794");
								WriteCDataElement(writer, "dtv", "Percentage", NsDtv, "1");
								writer.WriteEndElement();

								writer.WriteStartElement("Tax");
								writer.WriteAttributeString("TaxType", "Sales");
								writer.WriteAttributeString("dtv", "VoidFlag", NsDtv, "false");

								WriteCDataElement(writer, "TaxAuthority", "TR_VAT");
								WriteCDataElement(writer, "TaxableAmount", "0.00");
								WriteCDataElement(writer, "Amount", "0.00");
								WriteCDataElement(writer, "Percent", "0.00");
								WriteCDataElement(writer, "dtv", "RawTaxPercentage", NsDtv, "0.00");

								writer.WriteStartElement("dtv", "TaxLocationId", NsDtv);
								writer.WriteEndElement();

								WriteCDataElement(writer, "dtv", "TaxGroupId", NsDtv, "3");

								writer.WriteEndElement(); // </Tax>

								WriteLineItemProperty(writer, "DEAL_ITEM_PERCENT_OFF", "STRING", "yes");
								WriteLineItemProperty(writer, "DIM1", "STRING", "32");
								WriteLineItemProperty(writer, "DIM2", "STRING", "30");
								WriteLineItemProperty(writer, "STYLE", "STRING", "005013603");
								WriteLineItemProperty(writer, "EAN", "STRING", "5401157298299");

							}

							writer.WriteEndElement(); // </Sale>
							writer.WriteEndElement(); // </LineItem>

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("VoidFlag", "false");

							WriteCDataElement(writer, "SequenceNumber", "2");
							WriteCDataElement(writer, "LineNumber", "0");
							WriteCDataElement(writer, "BeginDateTime", "2025-02-21T10:16:31.97");
							WriteCDataElement(writer, "EndDateTime", "2025-02-21T10:16:45.64");

							writer.WriteStartElement("Tender");
							writer.WriteAttributeString("TenderType", "CREDIT");
							writer.WriteAttributeString("TypeCode", "SALE");
							writer.WriteAttributeString("ChangeFlag", "false");

							WriteCDataElement(writer, "TenderID", "TR0111-1");

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

							WriteCDataElement(writer, "RoundedTotal", "0.0000");
						}
					}
				}

				writer.WriteEndElement(); // </RetailTransaction>
				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndDocument(); // </POSLog>
			}
		}

		private static void WriteCDataElement(XmlWriter writer, string elementName, string content)
		{
			writer.WriteStartElement(elementName);
			writer.WriteCData(content ?? "");
			writer.WriteEndElement();
		}

		private static void WriteCDataElement(XmlWriter writer, string prefix, string localName, string ns, string content)
		{
			writer.WriteStartElement(prefix, localName, ns);
			writer.WriteCData(content ?? "");
			writer.WriteEndElement();
		}

		private static void WritePosTransactionProperties(XmlWriter writer, string code, string value)
		{
			writer.WriteStartElement("dtv", "PosTransactionProperties", NsDtv);

			writer.WriteStartElement("dtv", "PosTransactionPropertyCode", NsDtv);
			writer.WriteCData(code);
			writer.WriteEndElement();

			writer.WriteStartElement("dtv", "PosTransactionPropertyValue", NsDtv);
			writer.WriteCData(value);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		private static void WriteMerchandiseHierarchy(XmlWriter writer, string level, string value)
		{
			writer.WriteStartElement("MerchandiseHierarchy");
			writer.WriteAttributeString("Level", level);
			writer.WriteCData(value ?? "");
			writer.WriteEndElement();
		}

		private static void WriteLineItemProperty(XmlWriter writer, string code, string type, string value)
		{
			writer.WriteStartElement("dtv", "LineItemProperty", NsDtv);

			WriteCDataElement(writer, "dtv", "LineItemPropertyCode", NsDtv, code);
			WriteCDataElement(writer, "dtv", "LineItemPropertyType", NsDtv, type);
			WriteCDataElement(writer, "dtv", "LineItemPropertyValue", NsDtv, value);

			writer.WriteEndElement(); // </dtv:LineItemProperty>
		}

		private static string FormatDate(DateTimeOffset? date, bool includeTime = false)
		{
			if (!date.HasValue) return "";

			if (includeTime)
			{
				return date.Value.ToString("yyyy-MM-ddTHH:mm:ss.ff");
			}
			else
			{
				return date.Value.ToString("yyyy-MM-dd");
			}
		}

		private static IEnumerable<IGrouping<string, SalesModel>> GroupBySafe(IEnumerable<SalesModel> source, Func<SalesModel, string> keySelector)
		{
			return source
				.GroupBy(i => keySelector(i) ?? "UNKNOWN")
				.OrderBy(g =>
				{
					int n;
					return int.TryParse(g.Key, out n) ? n : int.MaxValue;
				});
		}
	}
}
