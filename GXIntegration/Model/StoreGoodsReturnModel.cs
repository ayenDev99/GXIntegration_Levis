using System;

namespace GXIntegration_Levis.Model
{
	public class StoreGoodsReturnModel
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
		public string AlternateStoreID { get; set; }
		public string ReasonCode { get; set; }
		public string OriginAlternateStoreID { get; set; }
		public string DocumentStatus { get; set; }
		public string DocumentID { get; set; }
		public string OriginatorName { get; set; }
		public string DocumentTypeDescription { get; set; }
		public string DocumentType { get; set; }
		public string DocumentSubType { get; set; }
		public DateTimeOffset? CreationTimestamp { get; set; }
		public DateTimeOffset? CompletionTimestamp { get; set; }
		public string ShipmentSequence { get; set; }
		public DateTimeOffset? ActualDeliveryDate { get; set; }
		public DateTimeOffset? ActualShipDate { get; set; }
		public string DestinationPartyID { get; set; }
		public string DestinationRetailLocationID { get; set; }
		public string ShipmentStatusCode { get; set; }
		public string City { get; set; }
		public string PostalCode { get; set; }
		public string ItemID { get; set; }
		public string ScannedBarcodeID { get; set; }
		public string QuantityShipped { get; set; }
		public string LineNumber { get; set; }
		public string Description { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTControlNumber { get; set; }
		public string PTEAN { get; set; }

		public string VouSid { get; set; }

	}
}
