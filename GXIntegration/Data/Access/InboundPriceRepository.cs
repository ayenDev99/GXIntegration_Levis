using GXIntegration_Levis.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Data.Access
{
	public class InboundPriceRepository
	{
		private string GetConnectionString()
		{
			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string dbPath = Path.Combine(baseDir, "AppData", "TempInboundPriceData.db");
			return $"Data Source={dbPath};Version=3;";
		}

		public async Task<bool> IsDuplicatePriceRecordAsync(string productCode, string effectivityDate, double price)
		{
			using (var conn = new SQLiteConnection(GetConnectionString()))
			{
				await conn.OpenAsync();

				string query = @"
					SELECT 
						COUNT(*) 
					FROM 
						TempInboundPriceData
					WHERE 
						ProductCode = @ProductCode
						AND EffectivityDate = @EffectivityDate
						AND Price = @Price;
				";

				using (var cmd = new SQLiteCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@ProductCode", productCode);
					cmd.Parameters.AddWithValue("@EffectivityDate", effectivityDate);
					cmd.Parameters.AddWithValue("@Price", price);

					long count = (long)(await cmd.ExecuteScalarAsync());
					return count > 0;
				}
			}
		}

		public async Task InsertTempInboundPriceData(
			string createdDate
			, string countryCode
			, string storeCode
			, string productCode
			, string colorCode
			, string sizeCode
			, string sku
			, string priceType
			, string currency
			, double price
			, string effectivityDate
			, string productReference
			, string brand
			, string priceListCode
			, string serialNumber
			, string priceSource
			, double price2
			, string effectivePriceEndDate
			, string discountCode
			, string discountDesc
			, string reasonCode
			, string reasonDesc
			, string level1Code)
		{
			if (await IsDuplicatePriceRecordAsync(productCode, effectivityDate, price))
			{
				Logger.LogInbound($"[DB - TempInboundPriceData] [SKIP] Duplicate found for ProductCode: {productCode} | EffectivityDate: {effectivityDate} | Price: {price}");

				return;
			}

			using (var conn = new SQLiteConnection(GetConnectionString()))
			{
				await conn.OpenAsync();

				try
				{
					string insertQuery = @"
						INSERT INTO TempInboundPriceData (
							CreatedDate
							, CountryCode
							, StoreCode
							, ProductCode
							, ColorCode
							, SizeCode
							, SKU
							, PriceType
							, Currency
							, Price
							, EffectivityDate
							, ProductReference
							, Brand
							, PriceListCode
							, SerialNumber
							, PriceSource
							, Price2
							, EffectivePriceEndDate
							, DiscountCode
							, DiscountDesc
							, ReasonCode
							, ReasonDesc
							, Level1Code
						) VALUES (
							@CreatedDate
							, @CountryCode
							, @StoreCode
							, @ProductCode
							, @ColorCode
							, @SizeCode
							, @SKU
							, @PriceType
							, @Currency
							, @Price
							, @EffectivityDate
							, @ProductReference
							, @Brand
							, @PriceListCode
							, @SerialNumber
							, @PriceSource
							, @Price2
							, @EffectivePriceEndDate
							, @DiscountCode
							, @DiscountDesc
							, @ReasonCode
							, @ReasonDesc
							, @Level1Code
						);";

					using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
					{
						cmd.Parameters.AddWithValue("@CreatedDate", createdDate);
						cmd.Parameters.AddWithValue("@CountryCode", countryCode);
						cmd.Parameters.AddWithValue("@StoreCode", storeCode);
						cmd.Parameters.AddWithValue("@ProductCode", productCode);
						cmd.Parameters.AddWithValue("@ColorCode", colorCode);
						cmd.Parameters.AddWithValue("@SizeCode", sizeCode);
						cmd.Parameters.AddWithValue("@SKU", sku);
						cmd.Parameters.AddWithValue("@PriceType", priceType);
						cmd.Parameters.AddWithValue("@Currency", currency);
						cmd.Parameters.AddWithValue("@Price", price);
						cmd.Parameters.AddWithValue("@EffectivityDate", effectivityDate);
						cmd.Parameters.AddWithValue("@ProductReference", productReference);
						cmd.Parameters.AddWithValue("@Brand", brand);
						cmd.Parameters.AddWithValue("@PriceListCode", priceListCode);
						cmd.Parameters.AddWithValue("@SerialNumber", serialNumber);
						cmd.Parameters.AddWithValue("@PriceSource", priceSource);
						cmd.Parameters.AddWithValue("@Price2", price2);
						cmd.Parameters.AddWithValue("@EffectivePriceEndDate", effectivePriceEndDate);
						cmd.Parameters.AddWithValue("@DiscountCode", discountCode);
						cmd.Parameters.AddWithValue("@DiscountDesc", discountDesc);
						cmd.Parameters.AddWithValue("@ReasonCode", reasonCode);
						cmd.Parameters.AddWithValue("@ReasonDesc", reasonDesc);
						cmd.Parameters.AddWithValue("@Level1Code", level1Code);

						await cmd.ExecuteNonQueryAsync();
					}

					Logger.LogInbound($"[INBOUND - PRICE] Successfully inserted to TempInboundPriceData record for ProductCode: {productCode} | EffectivityDate: {effectivityDate}");
				
				}
				catch (Exception ex)
				{
					Logger.LogError($"[ERROR] Failed to insert TempInboundPriceData record. Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
					throw;
				}
			}
		}

		public async Task<List<Dictionary<string, string>>> GetEligibleTempPriceRowsAsync(DateTime currentDate)
		{
			var results = new List<Dictionary<string, string>>();

			using (var connection = new SQLiteConnection(GetConnectionString()))
			{
				await connection.OpenAsync();
				var command = connection.CreateCommand();
				command.CommandText = @"
						SELECT 
							* 
						FROM 
							TempInboundPriceData 
						WHERE 
							EffectivityDate <= @currentDate 
							AND DeletedDate IS NULL
					";

				command.Parameters.AddWithValue("@currentDate", currentDate.ToString("yyyyMMdd"));

				using (var reader = await command.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						var row = new Dictionary<string, string>();
						for (int i = 0; i < reader.FieldCount; i++)
						{
							row[reader.GetName(i)] = reader.GetValue(i)?.ToString() ?? "";
						}
						results.Add(row);
					}
				}
			}

			// Log the total count
			Logger.LogInbound($"[INBOUND - PRICE] Retrieved {results.Count} data from temp db with EffectivityDate <= {currentDate:yyyyMMdd}");

			// Optionally log details of each row (e.g., ProductCode and EffectivityDate)
			foreach (var row in results)
			{
				if (row.TryGetValue("ProductCode", out var productCode) &&
					row.TryGetValue("EffectivityDate", out var effectivityDate))
				{
					Logger.LogInbound($"[DB] Eligible Row - ProductCode: {productCode}, EffectivityDate: {effectivityDate}");
				}
			}

			return results;
		}

		public async Task MarkTempPriceRowAsProcessedAsync(Dictionary<string, string> row)
		{
			using (var connection = new SQLiteConnection(GetConnectionString()))
			{
				await connection.OpenAsync();
				var command = connection.CreateCommand();
				//Logger.LogInbound($"[DB] {row["ProductCode"]}");


				command.CommandText = @"
					UPDATE 
						TempInboundPriceData 
					SET 
						DeletedDate = @deletedDate 
					WHERE ProductCode = @productCode AND EffectivityDate = @effectivityDate";

				command.Parameters.AddWithValue("@deletedDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));
				command.Parameters.AddWithValue("@productCode", row["ProductCode"]);
				command.Parameters.AddWithValue("@effectivityDate", row["EffectivityDate"]);

				await command.ExecuteNonQueryAsync();
			}
		}


	}
}
