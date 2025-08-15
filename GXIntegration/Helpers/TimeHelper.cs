using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Helpers
{
	public class TimeHelper
	{
		/// <summary>
		/// Gets a time range based on the real current UTC time and a dynamic offset in minutes.
		/// </summary>
		public static (DateTime from_date, DateTime to_date) GetPhilippineTimeRange(int minutesAgo)
		{
			DateTime utcNow = DateTime.UtcNow;

			// Windows timezone ID for PH (UTC+8)
			TimeZoneInfo phZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

			DateTime to_datePh = TimeZoneInfo.ConvertTimeFromUtc(utcNow, phZone);
			DateTime from_datePh = to_datePh.AddMinutes(-minutesAgo);

			return (from_datePh, to_datePh);
		}



	}
}
