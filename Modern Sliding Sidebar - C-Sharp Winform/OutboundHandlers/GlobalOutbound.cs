using GXIntegration_Levis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GXIntegration_Levis.OutboundHandlers
{
	public static class GlobalOutbound
	{
		public static readonly string NsIXRetail = "http://www.nrf-arts.org/IXRetail/namespace/";
		public static readonly string NsDtv = "http://www.datavantagecorp.com/xstore/";
		public static readonly string NsXsi = "http://www.w3.org/2001/XMLSchema-instance";
		public static void WriteCDataElement(XmlWriter writer, string elementName, string content)
		{
			writer.WriteStartElement(elementName);
			writer.WriteCData(content ?? "");
			writer.WriteEndElement();
		}

		public static void WriteCDataElement(XmlWriter writer, string prefix, string localName, string ns, string content)
		{
			writer.WriteStartElement(prefix, localName, ns);
			writer.WriteCData(content ?? "");
			writer.WriteEndElement();
		}

		public static void WritePosTransactionProperties(XmlWriter writer, string code, string value)
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

		public static void WriteMerchandiseHierarchy(XmlWriter writer, string level, string value)
		{
			writer.WriteStartElement("MerchandiseHierarchy");
			writer.WriteAttributeString("Level", level);
			writer.WriteCData(value ?? "");
			writer.WriteEndElement();
		}

		public static void WriteLineItemProperty(XmlWriter writer, string code, string type, string value)
		{
			writer.WriteStartElement("dtv", "LineItemProperty", NsDtv);

			WriteCDataElement(writer, "dtv", "LineItemPropertyCode", NsDtv, code);
			WriteCDataElement(writer, "dtv", "LineItemPropertyType", NsDtv, type);
			WriteCDataElement(writer, "dtv", "LineItemPropertyValue", NsDtv, value);

			writer.WriteEndElement(); // </dtv:LineItemProperty>
		}

		public static string FormatDate(DateTimeOffset? date, bool includeTime = false)
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

		public static IEnumerable<IGrouping<string, T>> GroupBySafe<T>(IEnumerable<T> source, Func<T, string> keySelector)
		{
			return source
				.GroupBy(i => keySelector(i) ?? "UNKNOWN")
				.OrderBy(g =>
				{
					int n;
					return int.TryParse(g.Key, out n) ? n : int.MaxValue;
				});
		}

		internal static IEnumerable<IGrouping<string, SalesModel>> GroupBySafe(List<ASNModel> items, Func<SalesModel, string> value)
		{
			throw new NotImplementedException();
		}
	}
}
