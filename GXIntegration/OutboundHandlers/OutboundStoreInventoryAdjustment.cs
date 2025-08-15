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

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class OutboundStoreInventoryAdjustment
	{
		public static async Task Execute(StoreInventoryAdjustmentRepository repository, GXConfig config)
		{
			try
			{
				DateTime date = DateTime.Today;
				var items = await repository.GetStoreInventoryAdjustmentAsync(date);

				Logger.Log($"Items count: {items.Count}");

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreInventoryAdjustment_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				GenerateXml(items, filePath);

				//MessageBox.Show($"Store Inventory Adjustment synced.\nFile Name: {fileName}. \nSaved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static void GenerateXml(List<StoreInventoryAdjustmentModel> items, string filePath)
		{
			var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

			using (XmlWriter writer = XmlWriter.Create(filePath, settings))
			{
				writer.WriteStartDocument();

				// Root element with namespaces
				writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
				writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
				writer.WriteAttributeString("dtv", GlobalOutbound.NsDtv);
				writer.WriteAttributeString("xs", GlobalOutbound.NsXsi);
				writer.WriteAttributeString("xs", "schemaLocation", GlobalOutbound.NsXsi, $"{GlobalOutbound.NsIXRetail} POSLog.xsd");

				// Transaction element
				writer.WriteStartElement("Transaction");
				writer.WriteAttributeString("CancelFlag", "false");
				writer.WriteAttributeString("OfflineFlag", "false");
				writer.WriteAttributeString("TrainingModeFlag", "false");
				writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
				writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ADJUSTMENT");
				writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "ADJUSTMENT");
				writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

				// Grouping by store and processing
				foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.StoreCode))
				{
					GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, "1");
					GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeGroup.Key);

					foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationNo))
					{
						var item = wsGroup.FirstOrDefault();
						if (item == null) continue;

						GlobalOutbound.WriteCDataElement(writer, "WorkstationID", "");
						GlobalOutbound.WriteCDataElement(writer, "TillID", storeGroup.Key);

						GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", item.SequenceNo);
						GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(item.BusinessDayDate));
						GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(item.BeginDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(item.EndDateTime, true));
						GlobalOutbound.WriteCDataElement(writer, "OperatorID", item.OperatorId);
						GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", item.CurrencyCode);

						GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", "true");
						GlobalOutbound.WritePosTransactionProperties(writer, "REGION", item.Region);
						GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", item.Country);
						GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", item.AlternateStoreId);

						// InventoryTransaction block
						foreach (var invTransGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
						{
							var invTransItem = invTransGroup.FirstOrDefault();

							foreach (var itemItem in invTransGroup)
							{
								writer.WriteStartElement("InventoryTransaction");
								GlobalOutbound.WriteCDataElement(writer, "CountID", itemItem.CountID);
								GlobalOutbound.WriteCDataElement(writer, "CountType", itemItem.CountType);
								GlobalOutbound.WriteCDataElement(writer, "CountStatus", itemItem.CountStatus);
								GlobalOutbound.WriteCDataElement(writer, "ReasonCode", itemItem.ReasonCode ?? "");
								GlobalOutbound.WriteCDataElement(writer, "Comment", itemItem.Comments ?? "");

								foreach (var lineItem in invTransGroup)
								{
									// ItemCount block
									writer.WriteStartElement("ItemCount");
									writer.WriteAttributeString("VoidFlag", "false");
									GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemId);
									GlobalOutbound.WriteCDataElement(writer, "Quantity", lineItem.QuantityShipped);  // Use appropriate property
									GlobalOutbound.WriteCDataElement(writer, "dtv", "InventoryBucketId", GlobalOutbound.NsDtv, lineItem.InventoryBucketId);

									// LineItem properties
									GlobalOutbound.WriteLineItemProperty(writer, "DIM1", "STRING", lineItem.PTDIM1);
									GlobalOutbound.WriteLineItemProperty(writer, "DIM2", "STRING", lineItem.PTDIM2);
									GlobalOutbound.WriteLineItemProperty(writer, "STYLE", "STRING", lineItem.PTStyle);
									GlobalOutbound.WriteLineItemProperty(writer, "EAN", "STRING", lineItem.PTEAN);

									writer.WriteEndElement(); // </ItemCount>
								}

								writer.WriteEndElement(); // </InventoryTransaction>
							}
						}
					}

				}

				writer.WriteEndElement(); // </POSLog>
				writer.WriteEndDocument();
			}
		}
	}
}
