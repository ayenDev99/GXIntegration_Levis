using System;

namespace GXIntegration_Levis.Model
{
	public class StoreInventoryCountModel
	{
		public string OrganizationID { get; set; }
		public string StoreID { get; set; }
		public string WorkstationID { get; set; }
		public string TillID { get; set; }
		public string SequenceNo { get; set; }
		public string BusinessDayDate { get; set; }
		public DateTimeOffset? BeginDateTime { get; set; }
		public DateTimeOffset? EndDateTime { get; set; }
		public string OperatorID { get; set; }
		public string CurrencyCode { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public string AlternateStoreID { get; set; }
		public string CountID { get; set; }
		public string DueDate { get; set; }
		public string CountType { get; set; }
		public string CountStatus { get; set; }
		public string VarianceAdj { get; set; }
		public string ItemID { get; set; }
		public string ScannedBarcodeID { get; set; }
		public string DIM1 { get; set; }
		public string DIM2 { get; set; }
		public string Quantity { get; set; }
		public string SnapshotQty { get; set; }
		public string UnitVariance { get; set; }
		public string InventoryBucketID { get; set; }

	}
}
