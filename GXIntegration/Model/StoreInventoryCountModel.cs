using System;

namespace GXIntegration_Levis.Model
{
	public class StoreInventoryCountModel
	{
		public string OrganizationID { get; set; }
		public string RetailStoreID { get; set; }
		public string WorkstationID { get; set; }
		public string TillID { get; set; }
		public string SequenceNo { get; set; }
		public DateTimeOffset? BusinessDayDate { get; set; }
		public DateTimeOffset? BeginDateTime { get; set; }
		public DateTimeOffset? EndDateTime { get; set; }
		public string OperatorID { get; set; }
		public string CurrencyCode { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public string AlternateStoreID { get; set; }
		public string CountID { get; set; }
		public DateTimeOffset? DueDate { get; set; }
		public string CountType { get; set; }
		public string CountStatus { get; set; }
		public string VariancesAdjusted { get; set; }
		public string ItemCountItemID { get; set; }
		public string ItemCountScannedBarcodeID { get; set; }
		public string ItemCountDIM1 { get; set; }
		public string ItemCountDIM2 { get; set; }
		public string ItemCountQuantity { get; set; }
		public string ItemCountSnapshotQuantity { get; set; }
		public string ItemCountUnitVariance { get; set; }
		public string ItemCountInventoryBucketID { get; set; }

	}
}
