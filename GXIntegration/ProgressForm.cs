using Guna.UI.WinForms;
using GXIntegration_Levis.Helpers;
using System;
using System.Windows.Forms;

namespace GXIntegration_Levis
{
	public partial class ProgressForm : Form
	{
		private GunaProgressBar progressBar;
		private RichTextBox rtbLog;


		public ProgressForm()
		{
			InitializeComponent();

			// Initialize ProgressBar
			progressBar = new GunaProgressBar
			{
				Dock = DockStyle.Top,
				Height = 30,
				Maximum = 100,
				Value = 0
			};
			this.Controls.Add(progressBar);

			// Initialize RichTextBox for logs
			rtbLog = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true
			};
			this.Controls.Add(rtbLog);

			// Subscribe to Logger events
			Logger.OnLogMessage += AppendLog;
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			base.OnFormClosed(e);
			// Unsubscribe to avoid memory leaks
			Logger.OnLogMessage -= AppendLog;
		}

		/// <summary>
		/// Update the progress bar safely
		/// </summary>
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

		/// <summary>
		/// Append log messages safely
		/// </summary>
		public void AppendLog(string message)
		{
			if (InvokeRequired)
			{
				Invoke(new Action(() => AppendLog(message)));
				return;
			}

			rtbLog.AppendText(message + Environment.NewLine);
			rtbLog.ScrollToCaret();
		}
	
	}
}
