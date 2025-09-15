using System;

namespace GXIntegration_Levis.Model
{
	public class StoreReceivingModel
	{
		public string OrganizationID { get; set; }
		public string RetailStoreID { get; set; }
		public string WorkstationID { get; set; }
		public string TillID { get; set; }
		public string SequenceNo { get; set; }
		public DateTimeOffset BusinessDayDate { get; set; }
		public DateTimeOffset? BeginDateTime { get; set; }
		public DateTimeOffset? EndDateTime { get; set; }
		public string OperatorID { get; set; }
		public string CurrencyCode { get; set; }
		public string InventoryMovementSuccess { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public string AlternateStoreId { get; set; }
		public string DestinationAlternateStoreId { get; set; }
		public string OriginAlternateStoreId { get; set; }
		public string DocumentStatus { get; set; }
		public string DocumentID { get; set; }
		public string OriginatorID { get; set; }
		public string OriginatorName { get; set; }
		public string DocumentTypeDescription { get; set; }
		public string DocumentType { get; set; }
		public string DocumentSubType { get; set; }
		public string RecordCreationType { get; set; }
		public DateTimeOffset? CreationTimestamp { get; set; }
		public DateTimeOffset? CompletionTimestamp { get; set; }
		public DateTimeOffset? LastActivityTimestamp { get; set; }
		public string ShipmentSequence { get; set; }
		public string DestinationRetailLocationId { get; set; }
		public string ShippingCarrier { get; set; }
		public string TrackingNumber { get; set; }
		public string StatusCode { get; set; }
		public string LineNumber { get; set; }
		public string ItemID { get; set; }
		public string ActualCount { get; set; }
		public string ExpectedCount { get; set; }
		public string PostedCount { get; set; }
		public string QuantityOrdered { get; set; }
		public string QuantityReceived { get; set; }
		public string Description { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTControlNumber { get; set; }
		public string PTEAN { get; set; }

		public string CartonNumber { get; set; }

	}
}
