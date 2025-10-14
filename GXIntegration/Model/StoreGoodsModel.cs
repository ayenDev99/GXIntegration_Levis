using System;
using System.Collections.Generic;

namespace GXIntegration_Levis.Model
{
	public class StoreGoodsModel
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
		public string TransOperatorID { get; set; }
		public string TransCurrencyCode { get; set; }
		public string AlternateStoreID { get; set; }

		// ReceiveInventory Header Level
		public string DocumentID { get; set; }
		public DateTimeOffset? CompletionTimestamp { get; set; }
		public DateTimeOffset? LastActivityTimestamp { get; set; }

		// Shipment Level
		public string ShipmentSequence { get; set; }
		public string DestinationRetailLocationID { get; set; }
		public string ShipmentStatusCode { get; set; }

		// Carton Level
		public string CartonID { get; set; }
		public string CartonStatusCode { get; set; }
		public List<SGCarton> SGCarton { get; set; } = new List<SGCarton>();

		// LineItem Level outside Carton
		public List<SGItems> SGItems { get; set; } = new List<SGItems>();
	}

	public class SGCarton
	{
		public string LineNumber { get; set; }
		public string ItemID { get; set; }
		public string ActualCount { get; set; }
		public string ExpectedCount { get; set; }
		public string PostedCount { get; set; }
		public DateTimeOffset? SaleLineBusinessDayDate { get; set; }
		public string TransactionSequence { get; set; }
		public string LineItemSequence { get; set; }
		public string RecordCreationType { get; set; }
		public string LineItemStatusCode { get; set; }
	}

	public class SGItems
	{
		public string ALU { get; set; }
		public string ItemLineNumber { get; set; }
		public string ItemID { get; set; }
		public string CartonID { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTControlNumber { get; set; }
		public string PTEAN { get; set; }
		public string QuantityOrdered { get; set; }
		public string QuantityReceived { get; set; }
		public string CartonNumber { get; set; }
		public string Description { get; set; }
	}

}
