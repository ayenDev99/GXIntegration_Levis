using GXIntegration_Levis.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GXIntegration_Levis.Model
{
	public class PriceModel
	{
		private static readonly Dictionary<string, string> _obPriceLevels = GlobalHelper.LoadOBPriceLevels();
		public string SalesOrg { get; set; }
		public string PC9 { get; set; }
		public string PriceLevel { get; set; }
		public string ConditionType { get; set; }
		public DateTimeOffset? PriceStartDate { get; set; }
		public DateTimeOffset? PriceEndDate { get; set; }
		public string Price { get; set; }
		public string Flag { get; set; }

		// ***************************************************
		// Format Methods
		// ***************************************************
		public string FormattedPriceLevel =>
			_obPriceLevels.TryGetValue(PriceLevel, out var mapped) ? mapped : PriceLevel;
		
		public string FormattedPriceStartDate
		{
			get
			{
				return PriceStartDate.HasValue
					? PriceStartDate.Value.ToString("dd-MMM-yy hh.mm.ss.ffffff tt", CultureInfo.InvariantCulture).ToUpper()
					: null;
			}
		}

		public string FormattedPriceEndDate
		{
			get
			{
				return PriceEndDate.HasValue
					? PriceEndDate.Value.ToString("dd-MMM-yy hh.mm.ss.ffffff tt", CultureInfo.InvariantCulture).ToUpper()
					: null;
			}
		}
	}
}
