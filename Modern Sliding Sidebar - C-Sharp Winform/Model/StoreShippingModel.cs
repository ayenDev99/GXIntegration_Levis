using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Model
{
	public class StoreShippingModel
	{
		public string StoreCode { get; set; }
		public string WorkstationNo { get; set; }
		public string SequenceNo { get; set; }
		public DateTimeOffset? BusinessDayDate { get; set; }
		public DateTimeOffset? BeginDateTime { get; set; }
		public DateTimeOffset? EndDateTime { get; set; }
		public string OperatorId { get; set; }
		public string CurrencyCode { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public string AlternateStoreId { get; set; }
		public string DocumentStatus { get; set; }
		public string DocumentId { get; set; }
		public DateTimeOffset? CompletionTimestamp { get; set; }
		public DateTimeOffset? LastActivityTimestamp { get; set; }
		public string ShipmentSequence { get; set; }
		public string DestinationRetailLocationId { get; set; }
		public string ShipmentStatusCode { get; set; }
		public string CartonId { get; set; }
		public string CartonStatusCode { get; set; }
		public string LineNumber { get; set; }
		public string ItemId { get; set; }
		public string ActualCount { get; set; }
		public string ExpectedCount { get; set; }
		public string PostedCount { get; set; }
		public DateTimeOffset? SaleLineBusinessDayDate { get; set; }
		public string TransactionSequence { get; set; }
		public string LineItemSequence { get; set; }
		public string RecordCreationType { get; set; }
		public string LineItemStatusCode { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTControlNumber { get; set; }
		public string PTEAN { get; set; }
		public string QuantityOrdered { get; set; }
		public string QuantityReceived { get; set; }
		public string Description { get; set; }
		public string ReasonCode { get; set; }
		public string OriginatorName { get; set; }
		public DateTimeOffset? ActualDeliveryDate { get; set; }
		public DateTimeOffset? ActualShipDate { get; set; }
		public string DestinationPartyID { get; set; }
		public string ShipmentPostalCode { get; set; }
		public string ShipmentCountry { get; set; }
		public string QuantityShipped { get; set; }
	}
}
