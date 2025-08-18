using Guna.UI.WinForms;
using GXIntegration.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationSFTPTab : UserControl
	{
		private GXConfig config;
		public ConfigurationSFTPTab(GXConfig config)
		{
			this.config = config;

			InitializeComponent();
			SetupSftpTab();
		}

		private void SetupSftpTab()
		{
			AutoScroll = true;

			int labelX = 20;
			int inputX = 150;
			int currentY = 20;
			int spacingY = 40;

			// Hostname
			Controls.Add(GlobalHelper.CreateLabel("Hostname", labelX, currentY));
			var txtHost = GlobalHelper.CreateTextBox(inputX, currentY);
			Controls.Add(txtHost);
			currentY += spacingY;

			// Port
			Controls.Add(GlobalHelper.CreateLabel("Port", labelX, currentY));
			var txtPort = GlobalHelper.CreateTextBox(inputX, currentY, "22");
			Controls.Add(txtPort);
			currentY += spacingY;

			// Username
			Controls.Add(GlobalHelper.CreateLabel("Username", labelX, currentY));
			var txtUser = GlobalHelper.CreateTextBox(inputX, currentY);
			Controls.Add(txtUser);
			currentY += spacingY;

			// Password
			Controls.Add(GlobalHelper.CreateLabel("Password", labelX, currentY));
			var txtPassword = GlobalHelper.CreateTextBox(inputX, currentY, "", true);
			Controls.Add(txtPassword);
			currentY += spacingY;

			// Test button
			var btnTestSftp = new GunaButton
			{
				Text = "Test SFTP Connection",
				Location = new Point(inputX, currentY),
				Size = new Size(160, 35)
			};
			GlobalHelper.StyleGunaButton(btnTestSftp, Color.FromArgb(33, 150, 243));

			btnTestSftp.Click += (s, e) =>
			{
				MessageBox.Show($"Testing SFTP connection to {txtHost.Text}:{txtPort.Text} with user {txtUser.Text}.",
								"Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
			};

			Controls.Add(btnTestSftp);
		}

	}
}
