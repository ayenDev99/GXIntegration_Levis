using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using Modern_Sliding_Sidebar___C_Sharp_Winform;
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
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8
			};

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// <POSLog>
				writer.WriteStartElement("POSLog", "http://www.nrf-arts.org/IXRetail/namespace/");

				// Add namespaces as attributes
				writer.WriteAttributeString("xmlns", "dtv", null, "http://www.datavantagecorp.com/xstore/");
				writer.WriteAttributeString("xmlns", "xs", null, "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("dtv", "http://www.datavantagecorp.com/xstore/");
				writer.WriteAttributeString("xs", "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("schemaLocation", "http://www.nrf-arts.org/IXRetail/namespace/ POSLog.xsd");

				// <Transaction>
				writer.WriteStartElement("Transaction", "http://www.nrf-arts.org/IXRetail/namespace/");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");

				writer.WriteAttributeString("dtv", "TransactionType", "http://www.datavantagecorp.com/xstore/", "RETAIL_SALE");

				// <dtv:OrganizationID><![CDATA[1]]></dtv:OrganizationID>
				writer.WriteStartElement("dtv", "OrganizationID", "http://www.datavantagecorp.com/xstore/");
				writer.WriteCData("1");
				writer.WriteEndElement(); // </dtv:OrganizationID>

				var stores = items
					.GroupBy(i => i.StoreNo ?? "UNKNOWN")
					.OrderBy(g =>
					{
						int.TryParse(g.Key, out int storeNo);
						return storeNo;
					});

				foreach (var storeGroup in stores)
				{
					writer.WriteStartElement("RetailStoreID");
					writer.WriteCData(storeGroup.Key ?? "");
					writer.WriteEndElement();

					var workstations = storeGroup
						.GroupBy(i => i.WorkstationNo ?? "UNKNOWN")
						.OrderBy(g =>
						{
							int.TryParse(g.Key, out int workstationNo);
							return workstationNo;
						});

					foreach (var wsGroup in workstations)
					{
						writer.WriteStartElement("WorkstationID");
						writer.WriteCData(wsGroup.Key ?? "");
						writer.WriteEndElement();

						writer.WriteStartElement("TillID");
						writer.WriteCData((storeGroup.Key + wsGroup.Key) ?? "");
						writer.WriteEndElement();

						var transactions = wsGroup
							.GroupBy(i => i.DocNo ?? "UNKNOWN")
							.OrderBy(g =>
							{
								int.TryParse(g.Key, out int docNo);
								return docNo;
							});

						foreach (var transGroup in transactions)
						{
							writer.WriteStartElement("SequenceNumber");
							writer.WriteCData(transGroup.Key ?? "");
							writer.WriteEndElement();

							var firstItem = transGroup.FirstOrDefault();
							string formattedDate = FormatDate(firstItem?.InvcPostDate);
							writer.WriteStartElement("BusinessDayDate");
							writer.WriteCData(formattedDate);
							writer.WriteEndElement();

							writer.WriteStartElement("BeginDateTime");
							writer.WriteCData("");
							writer.WriteEndElement();

							writer.WriteStartElement("EndDateTime");
							writer.WriteCData("");
							writer.WriteEndElement();

							writer.WriteStartElement("OperatorID");
							writer.WriteCData("");
							writer.WriteEndElement();

							writer.WriteStartElement("CurrencyCode");
							writer.WriteCData(firstItem?.CurrencyCode ?? "");
							writer.WriteEndElement();

							// RECEIPT_DELIVERY_METHOD
							// INVENTORY_MOVEMENT_SUCCESS

							// REGION
							writer.WriteStartElement("dtv", "PosTransactionProperties", "http://www.datavantagecorp.com/xstore/");
								writer.WriteStartElement("dtv", "PosTransactionPropertyCode", "http://www.datavantagecorp.com/xstore/");
								writer.WriteCData("REGION");
								writer.WriteEndElement();

								writer.WriteStartElement("dtv", "PosTransactionPropertyValue", "http://www.datavantagecorp.com/xstore/");
								writer.WriteCData("AMA");
								writer.WriteEndElement();
							writer.WriteEndElement();

							// COUNTRY
							writer.WriteStartElement("dtv", "PosTransactionProperties", "http://www.datavantagecorp.com/xstore/");
							writer.WriteStartElement("dtv", "PosTransactionPropertyCode", "http://www.datavantagecorp.com/xstore/");
							writer.WriteCData("COUNTRY");
							writer.WriteEndElement();

							writer.WriteStartElement("dtv", "PosTransactionPropertyValue", "http://www.datavantagecorp.com/xstore/");
							writer.WriteCData("AMA");
							writer.WriteEndElement();
							writer.WriteEndElement();

						}
					}
				}

				writer.WriteEndElement(); // </Transaction>
				writer.WriteEndElement(); // </POSLog>

				writer.WriteEndDocument();
			}
		}

		private static string FormatDate(DateTimeOffset? date)
		{
			if (date.HasValue)
			{
				// Format as "yyyy-MM-dd" (date part only)
				return date.Value.ToString("yyyy-MM-dd");
			}
			return "";
		}

	}
}
