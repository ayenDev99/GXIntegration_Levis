using Guna.UI.WinForms;
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

	}

}
