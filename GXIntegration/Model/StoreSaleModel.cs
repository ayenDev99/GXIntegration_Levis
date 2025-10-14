using System;
using System.Collections.Generic;

namespace GXIntegration_Levis.Model
{
	public class StoreSaleModel
	{
		// Transaction Level
		public string TransOrganizationID { get; set; }
		public string TransRetailStoreID { get; set; }
		public string TransWorkstationID { get; set; }
		public string TransTillID { get; set; }
		public string TransCashDrawerID { get; set; }
		public string TransSequenceNo { get; set; }
		public DateTimeOffset TransBusinessDayDate { get; set; }
		public DateTimeOffset TransBeginDateTime { get; set; }
		public DateTimeOffset TransEndDateTime { get; set; }
		public string TransOperatorID { get; set; }
		public string TransCurrencyCode { get; set; }
		public string TransAlternateStoreID { get; set; }
		public string TransTransactionCode { get; set; }
		public string TransBarcode { get; set; }

		public string TransGrandAmount { get; set; }
		public string TransRoundedTotal { get; set; }

		// Tax Level
		public string TaxAuthority { get; set; }
		public string TaxableAmount { get; set; }
		public string Amount { get; set; }
		public string Percent { get; set; }
		public string RawTaxPercentage { get; set; }
		public string TaxLocationID { get; set; }
		public string TaxGroupID { get; set; }

		public string DocSid { get; set; }

		public List<Items> Items { get; set; } = new List<Items>();
		public List<Tender> Tenders { get; set; } = new List<Tender>();
	}

	public class Items
	{
		public string ItemSequenceNo { get; set; }
		public string ItemLineNumber { get; set; }
		public DateTimeOffset ItemBeginDateTime { get; set; }
		public DateTimeOffset ItemEndDateTime { get; set; }
		public string SaleItemID { get; set; }
		public string SaleDescription { get; set; }
		public string SaleRegularSalesUnitPrice { get; set; }
		public string SaleActualSalesUnitPrice { get; set; }
		public string SaleExtendedAmount { get; set; }
		public string SaleQuantity { get; set; }
		public string SaleBrand { get; set; }
		public string SaleCategory { get; set; }
		public string SaleClass { get; set; }
		public string SaleSubClass { get; set; }
		public string SaleScannedItemID { get; set; }
		public string SaleGiftReceiptFlag { get; set; }

		public string PTDIM1 { get; set; }
		public string PTDIM2 { get; set; }
		public string PTStyle { get; set; }
		public string PTEAN { get; set; }

		public List<Discount> Discounts { get; set; } = new List<Discount>();
	}

	public class Discount
	{
		public string DiscSequenceNo { get; set; }
		public string DiscAmount { get; set; }
		public string DiscPromotionID { get; set; }
		public string DiscReasonCode { get; set; }
		public string DocItemSid { get; set; }
	}

	public class Tender
	{
		public string TenderSID { get; set; } 
		public string TenderSequenceNo { get; set; }
		public string TenderLineNumber { get; set; }
		public string TenderType { get; set; }
		public string TenderTypeCode { get; set; }
		public string TenderID { get; set; }
		public string AmountCurrency { get; set; }
		public string TenderAmount { get; set; }
		public string TenderAuthorizationNumber { get; set; }
		public DateTimeOffset TenderBeginDateTime { get; set; }
		public DateTimeOffset TenderEndDateTime { get; set; }
		public string ChangeFlag { get; set; }
	}

}
