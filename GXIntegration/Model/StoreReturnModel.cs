using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Model
{
	public class StoreReturnModel
	{
		public string DocSid { get; set; }
		public string StoreCode { get; set; }
		public string AlternateStoreId { get; set; }
		public string WorkstationNo { get; set; }
		public string DocNo { get; set; }
		public DateTimeOffset CreatedDateTime { get; set; }
		public DateTimeOffset? InvcPostDate { get; set; }
		public string CurrencyCode { get; set; }
		public string CashierLoginName { get; set; }
		public string ItemSequenceNumber { get; set; }
		public string TransactionCode { get; set; }
		public string Barcode { get; set; }
		public string SequenceNumber { get; set; }
		public DateTimeOffset? BeginDateTime { get; set; }
		public DateTimeOffset? EndDateTime { get; set; }
		public string ItemId { get; set; }
		public string Description { get; set; }
		public string RegularPrice { get; set; }
		public string ActualPrice { get; set; }
		public string ExtendedAmount { get; set; }
		public string Quantity { get; set; }
	}
}
