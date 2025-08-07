using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Model
{
	public class InventoryModel
	{
		public string CurrencyId { get; set; }
		public string StoreId { get; set; }
		// BIN_TYPE
		public string ProductCode { get; set; }
		public string Sku { get; set; }
		public string Waist { get; set; }
		public string Inseam { get; set; }
		// STOCK_FETCH_DATE
		public string LastMovementDate { get; set; }
		// QUANTITY_SIGN
		public string Quantity { get; set; }
		public string RetailPrice { get; set; }
		public string CountryCode { get; set; }
		public string ManufactureUpc { get; set; }
		public string Division { get; set; }
		// UNITCOUNT_SIGN
		// UNITCOUNT
	}
}
