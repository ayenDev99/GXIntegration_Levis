using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Model
{
	public class SalesModel
	{
		public string StoreNo { get; set; }
		public string WorkstationNo { get; set; }
		public string DocNo { get; set; }
		public DateTimeOffset? CreatedDateTime { get; set; }
		public DateTimeOffset? InvcPostDate { get; set; }
		public string CurrencyCode { get; set; }
		public string CashierLoginName { get; set; }

		public string ItemSequenceNumber { get; set; }



	}
}
