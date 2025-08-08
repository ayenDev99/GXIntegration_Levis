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
		private static readonly string NsXs = "http://www.w3.org/2001/XMLSchema-instance";
		public static async Task Execute(SalesRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetSalesAsync(date);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreSale_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath);

				MessageBox.Show($"✅ Inventory synced.\nSaved to: {outboundDir}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"❌ Error: {ex.Message}");
			}
		}

		private static void GenerateXml(List<SalesModel> items, string filePath)
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
				writer.WriteAttributeString("xmlns", "xs", null, NsXs);
				writer.WriteAttributeString("dtv", NsDtv);
				writer.WriteAttributeString("xs", NsXs);
				writer.WriteAttributeString("schemaLocation", NsIXRetail + " POSLog.xsd");

				// <Transaction>
				writer.WriteStartElement("Transaction", NsIXRetail);
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "TransactionType", NsDtv, "RETAIL_SALE");

				// <dtv:OrganizationID>
				writer.WriteStartElement("dtv", "OrganizationID", NsDtv);
				writer.WriteCData("1");
				writer.WriteEndElement();

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

							var firstItem = transGroup.FirstOrDefault();
							WriteCDataElement(writer, "BusinessDayDate", FormatDate(firstItem?.CreatedDateTime));
							WriteCDataElement(writer, "BeginDateTime", FormatDate(firstItem?.CreatedDateTime, true));
							WriteCDataElement(writer, "EndDateTime", FormatDate(firstItem?.InvcPostDate, true));
							WriteCDataElement(writer, "OperatorID", firstItem != null ? firstItem.CashierLoginName : "");
							WriteCDataElement(writer, "CurrencyCode", firstItem != null ? firstItem.CurrencyCode : "");

							WritePosTransactionProperties(writer, "RECEIPT_DELIVERY_METHOD", "PAPER");
							WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
							WritePosTransactionProperties(writer, "REGION", "AMA");
							WritePosTransactionProperties(writer, "COUNTRY", "PH");
							WritePosTransactionProperties(writer, "ALTERNATE_STOREID", "PH");
							// TRANSACTION_CODE
							// BARCODE

							writer.WriteStartElement("RetailTransaction");
							writer.WriteAttributeString("TransactionStatus", "Delivered");
							writer.WriteAttributeString("TypeCode", "Transaction");

							writer.WriteStartElement("LineItem");
							writer.WriteAttributeString("EntryMethod", "dtv:ScannerScanner");
							writer.WriteAttributeString("VoidFlag", "false");

							foreach (var itemGroup in GroupBySafe(transGroup, i => i.ItemSequenceNumber))
							{
								WriteCDataElement(writer, "SequenceNumber", itemGroup.Key);
								WriteCDataElement(writer, "LineNumber", itemGroup.Key);
								WriteCDataElement(writer, "BeginDateTime", itemGroup.Key);
								WriteCDataElement(writer, "EndDateTime", itemGroup.Key);
							}

							writer.WriteEndElement(); // </LineItem>
							writer.WriteEndElement(); // </RetailTransaction>
						}
					}
				}

				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndElement(); // </POSLog>
				writer.WriteEndDocument();
			}
		}

		private static void WriteCDataElement(XmlWriter writer, string name, string content)
		{
			writer.WriteStartElement(name);
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

		private static string FormatDate(DateTimeOffset? date, bool includeTime = false)
		{
			if (!date.HasValue)
				return "";

			if (includeTime) {
				return date.Value.ToString("yyyy-MM-ddTHH:mm:ss.ff");
			} else {
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
