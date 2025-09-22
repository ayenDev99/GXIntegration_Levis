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
	public static class OutboundStoreInventoryAdjustment
	{
		public static async Task Execute(StoreInventoryAdjustmentRepository repository, GXConfig config, string generate_type)
		{
			try
			{
				DateTime from_date = DateTime.Today; // 00:00:00
				DateTime to_date = from_date.AddDays(1).AddMilliseconds(-1); // 23:59:59.999
				//var items = await repository.GetStoreInventoryAdjustmentAsync(from_date, to_date);

				string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
				Directory.CreateDirectory(outboundDir);

				string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				string fileName = $"StoreInventoryAdjustment_{timestamp}.xml";
				string filePath = Path.Combine(outboundDir, fileName);

				//Logger.Log($"EOD StoreInventoryAdjustment downloaded successfully | Items Count: {items.Count} | File Name: {fileName}");
				//GenerateXml(items, filePath, generate_type);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Oracle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Logger.Log($"Error: {ex.Message}");
			}
		}

		public static string GenerateXml(List<StoreInventoryAdjustmentModel> items, string filePath, string generate_type)
		{
			if (!items.Any()) { return null; }

			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8,
				OmitXmlDeclaration = false
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

		public static void WriteXmlContent(List<StoreInventoryAdjustmentModel> items, XmlWriter writer)
		{
			// Transaction element
			writer.WriteStartElement("Transaction");
			writer.WriteAttributeString("CancelFlag", "false");
			writer.WriteAttributeString("OfflineFlag", "false");
			writer.WriteAttributeString("TrainingModeFlag", "false");
			writer.WriteAttributeString("dtv", "AppVersion", GlobalOutbound.NsDtv, "");
			writer.WriteAttributeString("dtv", "InventoryDocumentSubType", GlobalOutbound.NsDtv, "ADJUSTMENT");
			writer.WriteAttributeString("dtv", "InventoryDocumentType", GlobalOutbound.NsDtv, "ADJUSTMENT");
			writer.WriteAttributeString("dtv", "TransactionType", GlobalOutbound.NsDtv, "INVENTORY_CONTROL");

			// Grouping by store
			foreach (var storeGroup in GlobalOutbound.GroupBySafe(items, i => i.OrganizationID))
			{
				var storeItem = storeGroup.FirstOrDefault();
				if (storeItem == null) continue;

				GlobalOutbound.WriteCDataElement(writer, "dtv", "OrganizationID", GlobalOutbound.NsDtv, storeItem.OrganizationID);
				GlobalOutbound.WriteCDataElement(writer, "RetailStoreID", storeItem.RetailStoreID);

				// Group by Workstation
				foreach (var wsGroup in GlobalOutbound.GroupBySafe(storeGroup, i => i.WorkstationID))
				{
					var wsItem = wsGroup.FirstOrDefault();
					if (wsItem == null) continue;

					GlobalOutbound.WriteCDataElement(writer, "WorkstationID", wsItem.WorkstationID);
					GlobalOutbound.WriteCDataElement(writer, "TillID", wsItem.TillID);
					GlobalOutbound.WriteCDataElement(writer, "SequenceNumber", wsItem.SequenceNo);
					GlobalOutbound.WriteCDataElement(writer, "BusinessDayDate", GlobalOutbound.FormatDate(wsItem.BusinessDayDate));
					GlobalOutbound.WriteCDataElement(writer, "BeginDateTime", GlobalOutbound.FormatDate(wsItem.BeginDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "EndDateTime", GlobalOutbound.FormatDate(wsItem.EndDateTime, true));
					GlobalOutbound.WriteCDataElement(writer, "OperatorID", wsItem.OperatorID);
					GlobalOutbound.WriteCDataElement(writer, "CurrencyCode", wsItem.CurrencyCode);

					GlobalOutbound.WritePosTransactionProperties(writer, "INVENTORY_MOVEMENT_SUCCESS", wsItem.InventoryMovementSuccess);
					GlobalOutbound.WritePosTransactionProperties(writer, "REGION", wsItem.Region);
					GlobalOutbound.WritePosTransactionProperties(writer, "COUNTRY", wsItem.Country);
					GlobalOutbound.WritePosTransactionProperties(writer, "ALTERNATE_STOREID", wsItem.AlternateStoreID);

					// InventoryTransaction block grouped by SequenceNo
					foreach (var invTransGroup in GlobalOutbound.GroupBySafe(wsGroup, i => i.SequenceNo))
					{
						var invTransItem = invTransGroup.FirstOrDefault();
						if (invTransItem == null) continue;

						writer.WriteStartElement("InventoryTransaction");
						GlobalOutbound.WriteCDataElement(writer, "CountID", invTransItem.CountID);
						GlobalOutbound.WriteCDataElement(writer, "CountType", invTransItem.CountType);
						GlobalOutbound.WriteCDataElement(writer, "CountStatus", invTransItem.CountStatus);
						GlobalOutbound.WriteCDataElement(writer, "ReasonCode", invTransItem.ReasonCode ?? "");
						GlobalOutbound.WriteCDataElement(writer, "Comment", invTransItem.Comments ?? "");

						// ItemCount entries
						foreach (var lineItem in invTransGroup)
						{
							writer.WriteStartElement("ItemCount");
							writer.WriteAttributeString("VoidFlag", "false");

							GlobalOutbound.WriteCDataElement(writer, "ItemID", lineItem.ItemID);
							GlobalOutbound.WriteCDataElement(writer, "Quantity", lineItem.QuantityShipped);
							GlobalOutbound.WriteCDataElement(writer, "dtv", "InventoryBucketId", GlobalOutbound.NsDtv, lineItem.InventoryBucketID);

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

			writer.WriteEndElement(); // </Transaction>

		}

	}
}
