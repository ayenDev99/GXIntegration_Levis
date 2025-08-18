using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationSFTPTab : UserControl
	{
		private GXConfig config;

		private GunaTextBox txtHost, txtPort, txtUser, txtPassword;
		private GunaButton btnEdit, btnSave, btnTestSftp;
		private GunaLabel lblSftpStatus;

		private Control[] SftpInputControls => new Control[]
		{
			txtHost, txtPort, txtUser, txtPassword
		};

		public ConfigurationSFTPTab(GXConfig config)
		{
			this.config = config;
			InitializeComponent();
			SetupSftpTab();
			LoadSftpSettings();
		}

		private void SetupSftpTab()
		{
			AutoScroll = true;
			int labelX = 20;
			int inputX = 150;
			int currentY = 20;
			int spacingY = 40;

			// === Fields ===
			Controls.Add(GlobalHelper.CreateLabel("Hostname", labelX, currentY));
			txtHost = GlobalHelper.CreateTextBox(inputX, currentY);
			Controls.Add(txtHost);
			currentY += spacingY;

			Controls.Add(GlobalHelper.CreateLabel("Port", labelX, currentY));
			txtPort = GlobalHelper.CreateTextBox(inputX, currentY); // Blank by default
			Controls.Add(txtPort);
			currentY += spacingY;

			Controls.Add(GlobalHelper.CreateLabel("Username", labelX, currentY));
			txtUser = GlobalHelper.CreateTextBox(inputX, currentY);
			Controls.Add(txtUser);
			currentY += spacingY;

			Controls.Add(GlobalHelper.CreateLabel("Password", labelX, currentY));
			txtPassword = GlobalHelper.CreateTextBox(inputX, currentY, "", true);
			Controls.Add(txtPassword);
			currentY += spacingY;

			// === Status Label ===
			lblSftpStatus = new GunaLabel
			{
				Location = new Point(labelX, currentY),
				Width = 500,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			Controls.Add(lblSftpStatus);
			currentY += spacingY;

			// === Buttons ===
			btnEdit = new GunaButton
			{
				Text = "Edit",
				Location = new Point(370, 20),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGunaButton(btnEdit, Color.FromArgb(33, 150, 243));
			btnEdit.Click += BtnEdit_Click;

			btnSave = new GunaButton
			{
				Text = "Save",
				Location = new Point(370, 60),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGunaButton(btnSave, Color.FromArgb(76, 175, 80));
			btnSave.Click += BtnSave_Click;

			btnTestSftp = new GunaButton
			{
				Text = "Test SFTP Connection",
				Location = new Point(inputX, currentY),
				Size = new Size(160, 25)
			};
			GlobalHelper.StyleGunaButton(btnTestSftp, Color.FromArgb(138, 43, 226));
			btnTestSftp.Click += BtnTestSftp_Click;

			Controls.Add(btnEdit);
			Controls.Add(btnSave);
			Controls.Add(btnTestSftp);

			// === Change Events to Disable Save ===
			txtHost.TextChanged += DisableSaveOnEdit;
			txtPort.TextChanged += DisableSaveOnEdit;
			txtUser.TextChanged += DisableSaveOnEdit;
			txtPassword.TextChanged += DisableSaveOnEdit;
		}

		private void LoadSftpSettings()
		{
			string filePath = "config.xml";
			if (!File.Exists(filePath))
			{
				lblSftpStatus.Text = "config.xml not found.";
				return;
			}

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(filePath);

				var node = doc.DocumentElement.SelectSingleNode("SftpConnection");

				if (node == null)
				{
					lblSftpStatus.Text = "No SftpConnection node in config.xml.";
					return;
				}

				txtHost.Text = node["Host"]?.InnerText ?? "";
				txtPort.Text = node["Port"]?.InnerText ?? "";
				txtUser.Text = node["Username"]?.InnerText ?? "";
				txtPassword.Text = node["Password"]?.InnerText ?? "";

				GlobalHelper.SetControlsEnabled(false, SftpInputControls);
				btnEdit.Enabled = true;

				lblSftpStatus.Text = "Loaded SFTP configuration.";
				lblSftpStatus.ForeColor = Color.DarkGreen;
			}
			catch (Exception ex)
			{
				lblSftpStatus.Text = "Error loading config: " + ex.Message;
				lblSftpStatus.ForeColor = Color.Red;
			}
		}

		private void SaveSftpSettings()
		{
			try
			{
				string filePath = "config.xml";
				var doc = new XmlDocument();

				if (File.Exists(filePath))
				{
					doc.Load(filePath);
				}
				else
				{
					doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
					doc.AppendChild(doc.CreateElement("Configuration"));
				}

				var root = doc.DocumentElement;
				var existing = root.SelectSingleNode("SftpConnection");
				if (existing != null)
				{
					root.RemoveChild(existing);
				}

				var sftpNode = doc.CreateElement("SftpConnection");

				void AppendChild(string name, string value)
				{
					var element = doc.CreateElement(name);
					element.InnerText = value;
					sftpNode.AppendChild(element);
				}

				AppendChild("Host", txtHost.Text.Trim());
				AppendChild("Port", txtPort.Text.Trim());
				AppendChild("Username", txtUser.Text.Trim());
				AppendChild("Password", txtPassword.Text);

				root.AppendChild(sftpNode);

				doc.Save(filePath);

				MessageBox.Show("SFTP settings saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

				GlobalHelper.SetControlsEnabled(false, SftpInputControls);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;
				lblSftpStatus.Text = "Saved successfully.";
				lblSftpStatus.ForeColor = Color.Green;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to save config:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				lblSftpStatus.Text = "Failed to save.";
				lblSftpStatus.ForeColor = Color.Red;
			}
		}

		private void BtnTestSftp_Click(object sender, EventArgs e)
		{
			if (!ValidateSftpInputs()) return;

			string host = txtHost.Text.Trim();
			int port = int.Parse(txtPort.Text.Trim());
			string user = txtUser.Text.Trim();
			string pass = txtPassword.Text;

			try
			{
				using (var sftp = new Renci.SshNet.SftpClient(host, port, user, pass))
				{
					sftp.Connect();

					if (sftp.IsConnected)
					{
						MessageBox.Show("✅ SFTP connection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

						lblSftpStatus.Text = "Connection test passed.";
						lblSftpStatus.ForeColor = Color.Green;
						btnSave.Enabled = true;

						sftp.Disconnect();
					}
					else
					{
						throw new Exception("SFTP client could not connect.");
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ SFTP connection failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				lblSftpStatus.Text = "Connection test failed.";
				lblSftpStatus.ForeColor = Color.Red;
				btnSave.Enabled = false;
			}
		}

		private void BtnEdit_Click(object sender, EventArgs e)
		{
			GlobalHelper.SetControlsEnabled(true, SftpInputControls);
			btnEdit.Enabled = false;
			btnSave.Enabled = false;

			lblSftpStatus.Text = "Editing enabled.";
			lblSftpStatus.ForeColor = Color.Blue;
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			if (!ValidateSftpInputs()) return;

			SaveSftpSettings();
		}

		private void DisableSaveOnEdit(object sender, EventArgs e)
		{
			btnSave.Enabled = false;
			lblSftpStatus.Text = "❗Please test connection again after editing.";
			lblSftpStatus.ForeColor = Color.DarkOrange;
		}

		private bool ValidateSftpInputs()
		{
			string host = txtHost.Text.Trim();
			string port = txtPort.Text.Trim();
			string user = txtUser.Text.Trim();
			string password = txtPassword.Text;

			if (string.IsNullOrWhiteSpace(host) ||
				string.IsNullOrWhiteSpace(port) ||
				string.IsNullOrWhiteSpace(user) ||
				string.IsNullOrWhiteSpace(password))
			{
				MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			if (!int.TryParse(port, out _))
			{
				MessageBox.Show("Port must be a number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			return true;
		}
	}
}
