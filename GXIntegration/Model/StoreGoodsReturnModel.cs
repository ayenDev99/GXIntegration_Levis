using System;
using System.Collections.Generic;

namespace GXIntegration_Levis.Model
{
	public class StoreGoodsReturnModel
	{
		public string VouSid { get; set; }

		// Transaction Level
		public string TransOrganizationID { get; set; }
		public string TransRetailStoreID { get; set; }
		public string TransWorkstationID { get; set; }
		public string TransTillID { get; set; }
		public string TransSequenceNo { get; set; }
		public DateTimeOffset TransBusinessDayDate { get; set; }
		public DateTimeOffset? TransBeginDateTime { get; set; }
		public DateTimeOffset? TransEndDateTime { get; set; }
		public string TransCurrencyCode { get; set; }
		public string TransAlternateStoreID { get; set; }
		public string TransReasonCode { get; set; }
		public string TransOriginAlternateStoreID { get; set; }
		public string TransDocumentStatus { get; set; }
		public string TransDocumentID { get; set; }
		public string TransOriginatorName { get; set; }
		public DateTimeOffset? TransCreationTimestamp { get; set; }
		public DateTimeOffset? TransCompletionTimestamp { get; set; }
		public DateTimeOffset? TransLastActivityTimestamp { get; set; }

		// Shipment Level
		public string ShipmentSequence { get; set; }
		public DateTimeOffset? ActualDeliveryDate { get; set; }
		public DateTimeOffset? ActualShipDate { get; set; }
		public string DestinationPartyID { get; set; }
		public string DestinationRetailLocationID { get; set; }
		public string ShipmentStatusCode { get; set; }
		public string City { get; set; }
		public string PostalCode { get; set; }

		public List<SGRItems> SGRItems { get; set; } = new List<SGRItems>();
	}

	public class SGRItems
	{
		public string ItemID { get; set; }
		public string ScannedBarcodeID { get; set; }
		public string QuantityShipped { get; set; }
		public string LineNumber { get; set; }
		public string Description { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTEAN { get; set; }
	}
}
