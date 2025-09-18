using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Model
{
	public class StoreSaleModel
	{
		public string OrganizationID { get; set; }
		public string RetailStoreID { get; set; }
		public string WorkstationID { get; set; }
		public string TillID { get; set; }
		public string SequenceNo { get; set; }
		public DateTimeOffset BusinessDayDate { get; set; }
		public DateTimeOffset BeginDateTime { get; set; }
		public DateTimeOffset EndDateTime { get; set; }
		public string OperatorID { get; set; }
		public string CurrencyCode { get; set; }
		public string ReceiptDeliveryMethod { get; set; }
		public string InventoryMovementSuccess { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public string AlternateStoreID { get; set; }
		public string TransactionCode { get; set; }
		public string Barcode { get; set; }
		public string LineItemSequenceNo { get; set; }
		public string LineItemLineNumber { get; set; }
		public DateTimeOffset LineItemBeginDateTime { get; set; }
		public DateTimeOffset LineItemEndDateTime { get; set; }
		public string SaleItemID { get; set; }
		public string SaleDescription { get; set; }
		public string SaleRegularSalesUnitPrice { get; set; }
		public string SaleActualSalesUnitPrice { get; set; }
		public string SaleExtendedAmount { get; set; }
		public string SaleQuantity { get; set; }
		public string Division { get; set; }
		public string Department { get; set; }
		public string SubDepartment { get; set; }
		public string Class { get; set; }
		public string ScannedItemID { get; set; }
		public string GiftReceiptFlag { get; set; }
		public string AssociateID { get; set; }
		public string Percentage { get; set; }
		public string TaxAuthority { get; set; }
		public string TaxableAmount { get; set; }
		public string Amount { get; set; }
		public string Percent { get; set; }
		public string RawTaxPercentage { get; set; }
		public string TaxGroupID { get; set; }
		public string DealItemPercentOff { get; set; }
		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTEAN { get; set; }
		public string TenderSequenceNo { get; set; }
		public string TenderLineNumber { get; set; }
		public DateTimeOffset TenderBeginDateTime { get; set; }
		public DateTimeOffset TenderEndDateTime { get; set; }
		public string TenderType { get; set; }
		public string TypeCode { get; set; }
		public string ChangeFlag { get; set; }
		public string TenderID { get; set; }
		public string AmountCurrency { get; set; }
		public string TenderAmount { get; set; }
		public string TransactionGrandAmount { get; set; }
		public string RoundedTotal { get; set; }
		public string DocSid { get; set; }

	}

}
