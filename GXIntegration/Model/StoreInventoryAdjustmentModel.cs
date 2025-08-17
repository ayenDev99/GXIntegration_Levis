using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Model
{
	public class StoreInventoryAdjustmentModel
	{
		public string AdjSid { get; set; }
		public string StoreCode { get; set; }
		public string WorkstationNo { get; set; }
		public string SequenceNo { get; set; }
		public DateTimeOffset BusinessDayDate { get; set; }
		public DateTimeOffset? BeginDateTime { get; set; }
		public DateTimeOffset? EndDateTime { get; set; }
		public string OperatorId { get; set; }
		public string CurrencyCode { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public string AlternateStoreId { get; set; }
		public string CountID { get; set; }
		public string CountType { get; set; }
		public string CountStatus { get; set; }
		public string ReasonCode { get; set; }

		public string Comments { get; set; }
		public string ItemId { get; set; }
		public string QuantityShipped { get; set; }
		public string InventoryBucketId { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTEAN { get; set; }

	}
}
