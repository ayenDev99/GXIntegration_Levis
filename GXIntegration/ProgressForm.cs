using Guna.UI.WinForms;
using GXIntegration_Levis.Helpers;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace GXIntegration_Levis
{
	public partial class ProgressForm : Form
	{
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture();
		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

		private const int WM_NCLBUTTONDOWN = 0xA1;
		private const int HTCAPTION = 0x2;

		private Panel topBar;
		private Button closeButton;

		private GunaProgressBar progressBar;
		private RichTextBox rtbLog;

		public ProgressForm()
		{
			InitializeComponent();
			InitializeTopBar();
			InitializeControls();

			Logger.OnLogMessage += AppendLog;

			this.Load += (s, e) => SetRoundedRegion(20);
		}

		private void InitializeTopBar()
		{
			this.FormBorderStyle = FormBorderStyle.None;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Size = new Size(750, 400);
			this.BackColor = Color.FromArgb(227, 227, 225);

			topBar = new Panel
			{
				Dock = DockStyle.Top,
				Height = 40,
				BackColor = Color.FromArgb(60, 60, 60)
			};
			this.Controls.Add(topBar);

			// Top bar "Activity Log" label 
			Label titleLabel = new Label
			{
				Text = "Activity Log",
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 10),
				AutoSize = true,
				Location = new Point(10, 10)
			};
			topBar.Controls.Add(titleLabel);

			EnableDrag(topBar);

			// Close button
			closeButton = new Button
			{
				Text = "x",
				Font = new Font("Segoe UI", 10, FontStyle.Bold),
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				FlatStyle = FlatStyle.Flat,
				Size = new Size(40, 50),
				Location = new Point(this.Width - 40, -5),
				Anchor = AnchorStyles.Top | AnchorStyles.Right
			};
			closeButton.FlatAppearance.BorderSize = 0;
			closeButton.Click += (s, e) => this.Close();
			closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(60, 60, 60);
			closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.Transparent;
			topBar.Controls.Add(closeButton);
		}

		private void InitializeControls()
		{
			// Container panel for progress bar with padding
			Panel progressPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 35,
				Padding = new Padding(10, 5, 10, 10),
				BackColor = Color.Transparent
			};
			this.Controls.Add(progressPanel);

			// Progress bar inside the panel
			progressBar = new GunaProgressBar
			{
				Dock = DockStyle.Top,
				Maximum = 100,
				Value = 0,
				IdleColor = Color.LightGray
			};
			progressBar.Paint += ProgressBar_Paint;
			progressPanel.Controls.Add(progressBar);

			// Container panel for RichTextBox to add spacing
			Panel logPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(10, 50, 10, 40) 
			};
			this.Controls.Add(logPanel);

			// RichTextBox for logs
			rtbLog = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				BackColor = Color.Black,
				ForeColor = Color.SlateGray,
				Font = new Font("Arial", 10),
				ScrollBars = RichTextBoxScrollBars.Both,
				WordWrap = false
			};
			logPanel.Controls.Add(rtbLog);
		}

		private void EnableDrag(Control control)
		{
			control.MouseDown += (s, e) =>
			{
				if (e.Button == MouseButtons.Left)
				{
					ReleaseCapture();
					SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
				}
			};
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			base.OnFormClosed(e);
			Logger.OnLogMessage -= AppendLog;
		}

		public void UpdateProgress(int value, int maximum)
		{
			if (InvokeRequired)
			{
				Invoke(new Action(() => UpdateProgress(value, maximum)));
				return;
			}

			progressBar.Maximum = maximum;
			progressBar.Value = Math.Min(value, maximum);
		}

		public void AppendLog(string message)
		{
			if (InvokeRequired)
			{
				Invoke(new Action(() => AppendLog(message)));
				return;
			}

			rtbLog.AppendText($"{message}{Environment.NewLine}");
			rtbLog.ScrollToCaret();

			// Keep last 1000 lines only (compatible with .NET Framework)
			string[] lines = rtbLog.Lines;
			if (lines.Length > 1000)
			{
				string[] lastLines = new string[1000];
				Array.Copy(lines, lines.Length - 1000, lastLines, 0, 1000);
				rtbLog.Lines = lastLines;
			}
		}

		private void SetRoundedRegion(int radius)
		{
			GraphicsPath path = new GraphicsPath();
			path.StartFigure();
			path.AddArc(0, 0, radius, radius, 180, 90); // top-left
			path.AddArc(this.Width - radius, 0, radius, radius, 270, 90); // top-right
			path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90); // bottom-right
			path.AddArc(0, this.Height - radius, radius, radius, 90, 90); // bottom-left
			path.CloseFigure();
			this.Region = new Region(path);
		}

		private void ProgressBar_Paint(object sender, PaintEventArgs e)
		{
			GunaProgressBar pb = sender as GunaProgressBar;

			// Draw background (idle)
			using (SolidBrush bgBrush = new SolidBrush(pb.IdleColor))
			{
				e.Graphics.FillRectangle(bgBrush, 0, 0, pb.Width, pb.Height);
			}

			// Draw filled portion (green)
			int fillWidth = (int)((pb.Value / (float)pb.Maximum) * pb.Width);
			using (SolidBrush fillBrush = new SolidBrush(Color.Green))
			{
				e.Graphics.FillRectangle(fillBrush, 0, 0, fillWidth, pb.Height);
			}
		}

	}
}
