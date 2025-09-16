using Guna.UI.WinForms;
using GXIntegration.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.Helpers
{
	public class GlobalHelper
	{
		private static int _hoveredRowIndex = -1;
		private static readonly Dictionary<DataGridView, int> HoveredRowIndices = new Dictionary<DataGridView, int>();
		public static async Task HandleDownloadClick(DataGridView dataGrid, Dictionary<string, Func<Task>> downloadActions, int rowIndex, int columnIndex, string actionColumnName)
		{
			if (rowIndex < 0 || columnIndex != dataGrid.Columns[actionColumnName].Index) return;

			var name = dataGrid.Rows[rowIndex].Cells[1].Value.ToString();

			if (downloadActions.TryGetValue(name, out var action))
			{
				try
				{
					dataGrid.Enabled = false;
					Cursor.Current = Cursors.WaitCursor;

					await action();

					MessageBox.Show($"{name} downloaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error processing {name}:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				finally
				{
					dataGrid.Enabled = true;
					Cursor.Current = Cursors.Default;
				}
			}
		}

		public static void HandleCellMouseMove(DataGridView dataGridView, DataGridViewCellMouseEventArgs e, string actionColumnName = "Action")
		{
			if (dataGridView == null)
				return;

			if (e.RowIndex >= 0 && e.RowIndex != _hoveredRowIndex)
			{
				if (_hoveredRowIndex >= 0 && _hoveredRowIndex < dataGridView.Rows.Count)
					dataGridView.Rows[_hoveredRowIndex].DefaultCellStyle.BackColor = Color.White;

				dataGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
				_hoveredRowIndex = e.RowIndex;

				dataGridView.Cursor = dataGridView.Columns[e.ColumnIndex].Name == actionColumnName
					? Cursors.Hand
					: Cursors.Default;
			}
		}

		public static void HandleCellMouseLeave(DataGridView dataGridView)
		{
			if (dataGridView == null) return;

			if (!HoveredRowIndices.ContainsKey(dataGridView))
				HoveredRowIndices[dataGridView] = -1;

			int hoveredIndex = HoveredRowIndices[dataGridView];

			if (hoveredIndex >= 0 && hoveredIndex < dataGridView.Rows.Count)
			{
				dataGridView.Rows[hoveredIndex].DefaultCellStyle.BackColor = Color.White;
				HoveredRowIndices[dataGridView] = -1;
			}

			dataGridView.Cursor = Cursors.Default;
		}

		public static GunaButton CreateButton(string text, Point location, Func<Task> clickAction, int fixedHeight = 40, int paddingWidth = 30, int minWidth = 150)
		{
			var button = new GunaButton
			{
				Text = text,
				Location = location,
				ForeColor = Color.White,
				BaseColor = Color.FromArgb(100, 88, 255),
				OnHoverBaseColor = Color.FromArgb(72, 61, 255),
				Font = new Font("Segoe UI", 9F, FontStyle.Bold),
				Cursor = Cursors.Hand,
				Height = fixedHeight
			};

			using (var graphics = button.CreateGraphics())
			{
				SizeF textSize = graphics.MeasureString(text, button.Font);
				int calculatedWidth = (int)textSize.Width + paddingWidth;

				button.Width = Math.Max(calculatedWidth, minWidth);
			}

			button.Click += async (s, e) =>
			{
				if (clickAction != null)
					await clickAction();
			};

			return button;
		}

		// ***************************************************
		// Dates Methods
		// ***************************************************
		public static (DateTime from, DateTime to) GetProcessingTimeWindow(GXConfig config)
		{
			string timeStr = Program.CurrentTime;
			int processMinutes = config.ReprocessMinutes;

			// TO = selected time of day + 59s 999ms (end of that minute)
			DateTime to_date = DateTime.Today
				.Add(TimeSpan.Parse(timeStr))
				.AddSeconds(59)
				.AddMilliseconds(999);

			// FROM = start of N minutes ago (reset to 00.000)
			DateTime from_date = to_date
				.AddMinutes(-processMinutes)
				.AddSeconds(-59)
				.AddMilliseconds(-999);

			//Logger.Log($">>> Processing Time Window: FROM = {from_date:yyyy-MM-dd HH:mm}, TO = {to_date:yyyy-MM-dd HH:mm}");

			return (from_date, to_date);
		}

		public static string FormatDateToIso8601(string inputDate)
		{
			if (DateTime.TryParseExact(inputDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime datePart))
			{
				// Get current time (UTC) and combine with the input date
				DateTime now = DateTime.UtcNow;
				DateTime fullDateTime = new DateTime(
					datePart.Year, datePart.Month, datePart.Day,
					now.Hour, now.Minute, now.Second, now.Millisecond,
					DateTimeKind.Utc);

				return fullDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
			}

			return null;
		}

		public static string ToOracleTimestampTZLiteral(DateTime dt, string timezoneOffset = "+08:00")
		{
			// Format example: 20-AUG-25 09.59.20.123456 AM +08:00
			return dt.ToString("dd-MMM-yy hh.mm.ss.ffffff tt").ToUpper() + " " + timezoneOffset;
		}

		// ***************************************************
		// Parsing Methods
		// ***************************************************
		public static decimal? GetDecimalValue(IDictionary<string, string> item, string key, int decimalPlaces)
		{
			if (item != null &&
				item.TryGetValue(key, out var val) &&
				decimal.TryParse(val, out var parsed))
			{
				return Math.Round(parsed, decimalPlaces);
			}

			return null;
		}

		public static int? GetIntValue(IDictionary<string, string> item, string key)
		{
			if (item != null &&
				item.TryGetValue(key, out var val) &&
				int.TryParse(val?.ToString(), out var parsed))
			{
				return parsed;
			}
			return null;
		}

		public static string GetStringValue(IDictionary<string, string> item, string key)
		{
			if (item != null &&
				item.TryGetValue(key, out var val) &&
				val != null)
			{
				return val.ToString();
			}

			return null;
		}

		// ***************************************************
		// Buttons Methods
		// ***************************************************
		public static void StyleGunaButton(GunaButton button, Color baseColor)
		{
			// Derived colors
			Color hoverColor = Color.FromArgb(200, baseColor);   // More transparent
			Color pressedColor = ControlPaint.Dark(baseColor);   // Slightly darker
			Color borderColor = baseColor;

			button.BaseColor = baseColor;
			button.ForeColor = Color.White;
			button.BorderColor = borderColor;
			button.BorderSize = 1;
			button.Radius = 1;
			button.Font = new Font("Segoe UI", 10, FontStyle.Regular);
			button.TextAlign = HorizontalAlignment.Center;
			button.Image = null;

			button.OnHoverBaseColor = hoverColor;
			button.OnHoverForeColor = Color.White;
			button.OnHoverBorderColor = borderColor;
			button.OnPressedColor = pressedColor;

			button.MouseEnter += (s, e) => { button.Cursor = Cursors.Hand; };
			button.MouseLeave += (s, e) => { button.Cursor = Cursors.Default; };
		}

		public static void SetControlsEnabled(bool enabled, params Control[] controls)
		{
			foreach (var control in controls)
			{
				control.Enabled = enabled;
			}
		}

		public static GunaLabel CreateLabel(string text, int x, int y, int width = 120)
		{
			return new GunaLabel
			{
				Text = text,
				Location = new Point(x, y),
				Width = width
			};
		}

		public static GunaTextBox CreateTextBox(int x, int y, string defaultText = "", bool isPassword = false)
		{
			return new GunaTextBox
			{
				Location = new Point(x, y),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black,
				Text = defaultText,
				PasswordChar = isPassword ? '*' : '\0'
			};
		}

		// ***************************************************
		// Prism Error Log Methods
		// ***************************************************
		public class PrismErrorResponse
		{
			public List<PrismError> errors { get; set; }
		}

		public class PrismError
		{
			public string errorcode { get; set; }
			public string errormsg { get; set; }
		}

		// ***************************************************
		// Config.xml Methods
		// ***************************************************
		public static Dictionary<string, string> LoadOBPriceLevels()
		{
			var result = new Dictionary<string, string>();
			var doc = new System.Xml.XmlDocument();
			doc.Load("config.xml");

			foreach (System.Xml.XmlNode node in doc.SelectNodes("//OBPriceLevels/add"))
			{
				var key = node.Attributes["key"]?.Value;
				var value = node.Attributes["value"]?.Value;

				if (key != null && value != null)
					result[key] = value;
			}

			return result;
		}
	}

}
