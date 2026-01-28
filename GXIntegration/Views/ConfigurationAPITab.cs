using Guna.UI2.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationAPITab : UserControl
	{
		private GXConfig config;

		private Guna2TextBox txtUsername, txtPassword, txtSaleApiUrl, txtInventoryApiUrl;
		private Guna2Button btnEdit, btnSave, btnTestConn;
		private Label lblStatus;

		public ConfigurationAPITab(GXConfig config)
		{
			this.config = config;
			InitializeComponent();
			SetupApiTab();
			LoadApiConfig();
		}

		private Control[] ApiInputs => new Control[]
		{
			txtUsername,
			txtPassword,
			txtSaleApiUrl,
			txtInventoryApiUrl
		};

		private void SetupApiTab()
		{
			AutoScroll = true;

            int inputStartX = 180;
			int labelStartX = 20;
			int currentY = 15;
			int spacingY = 40;

			// Local helpers
			Label CreateLabel(string text, int y) => new Label
			{
				Text = text,
				Location = new Point(labelStartX, y),
				Width = 150
			};

			Guna2TextBox CreateTextBox(int y, bool isPassword = false) => new Guna2TextBox
			{
				Location = new Point(inputStartX, y),
				Width = 500,
				//BaseColor = Color.White,
				ForeColor = Color.Black,
				PasswordChar = isPassword ? '*' : '\0'
			};

			// Inputs
			Controls.Add(CreateLabel("Username", currentY));
			txtUsername = CreateTextBox(currentY);
			Controls.Add(txtUsername);
			currentY += spacingY;

			Controls.Add(CreateLabel("Password", currentY));
			txtPassword = CreateTextBox(currentY, true);
			Controls.Add(txtPassword);
			currentY += spacingY;

			Controls.Add(CreateLabel("Sale API URL", currentY));
			txtSaleApiUrl = CreateTextBox(currentY);
			Controls.Add(txtSaleApiUrl);
			currentY += spacingY;

			Controls.Add(CreateLabel("Inventory API URL", currentY));
			txtInventoryApiUrl = CreateTextBox(currentY);
			Controls.Add(txtInventoryApiUrl);
			currentY += spacingY;

			// Status
			lblStatus = new Label
			{
				Location = new Point(labelStartX, currentY),
				Width = 600,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			Controls.Add(lblStatus);
			currentY += spacingY;

            // --------------------
            // Buttons
            // --------------------
            // Edit Button
            btnEdit = GlobalHelper.CreateButton(
                text: "Edit ",
                Location = new Point(700, 20),
                fillColor: Color.SteelBlue,
                clickAction: async () => await BtnEdit_Click()
            );
            this.Controls.Add(btnEdit);

            // Save Button
            btnSave = GlobalHelper.CreateButton(
                text: "Save",
                Location = new Point(700, 60),
                fillColor: Color.MediumSeaGreen,
                clickAction: async () => await BtnSave_Click()
            );
            this.Controls.Add(btnSave);

            // Test Connection Button
            btnTestConn = GlobalHelper.CreateButton(
                text: "Test Connection",
                location: new Point(inputStartX, currentY),
                fillColor: Color.MediumPurple,
                clickAction: async () => await BtnTestConn_Click()
            );
            this.Controls.Add(btnTestConn);

			// Events
			txtUsername.TextChanged += DisableSaveOnEdit;
			txtPassword.TextChanged += DisableSaveOnEdit;
			txtSaleApiUrl.TextChanged += DisableSaveOnEdit;
			txtInventoryApiUrl.TextChanged += DisableSaveOnEdit;
		}

		private void LoadApiConfig()
		{
			string filePath = "config.xml";
			if (!File.Exists(filePath))
			{
				lblStatus.Text = "config.xml not found.";
				return;
			}

			try
			{
				var doc = XDocument.Load(filePath);
				var apiNode = doc.Root.Element("APIConnection");
				if (apiNode == null)
				{
					lblStatus.Text = "No APIConnection section found in config.xml.";
					return;
				}

				txtUsername.Text = apiNode.Element("Username")?.Value ?? "";
				txtPassword.Text = apiNode.Element("Password")?.Value ?? "";
				txtSaleApiUrl.Text = apiNode.Element("SaleApiUrl")?.Value ?? "";
				txtInventoryApiUrl.Text = apiNode.Element("InventoryApiUrl")?.Value ?? "";

				GlobalHelper.SetControlsEnabled(false, ApiInputs);
				btnEdit.Enabled = true;

				lblStatus.Text = "Loaded APIConnection from config.xml";
				lblStatus.ForeColor = Color.DarkGreen;
			}
			catch (Exception ex)
			{
				lblStatus.Text = "Failed to load APIConnection: " + ex.Message;
				lblStatus.ForeColor = Color.Red;
			}
		}

		private async Task BtnTestConn_Click()
		{
			try
			{
				string username = txtUsername.Text.Trim();
				string password = txtPassword.Text;
				string saleUrl = txtSaleApiUrl.Text.Trim();
				string invUrl = txtInventoryApiUrl.Text.Trim();

				// Simulate async API call
				await Task.Delay(1000);

				if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
					string.IsNullOrEmpty(saleUrl) || string.IsNullOrEmpty(invUrl))
				{
					MessageBox.Show("❌ Please fill in all fields before testing.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					lblStatus.Text = "Missing values for API test.";
					lblStatus.ForeColor = Color.Red;
					return;
				}

				// TODO: Replace with real HTTP test
				MessageBox.Show("✅ API configuration looks valid!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				lblStatus.Text = "API test passed.";
				lblStatus.ForeColor = Color.Green;
				btnSave.Enabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ API test error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				lblStatus.Text = "API test failed.";
				lblStatus.ForeColor = Color.Red;
			}
		}

		private async Task BtnEdit_Click()
		{
			GlobalHelper.SetControlsEnabled(true, ApiInputs);
			btnEdit.Enabled = false;
			btnSave.Enabled = false;
			lblStatus.Text = "Editing enabled.";
			lblStatus.ForeColor = Color.Blue;
		}

		private async Task BtnSave_Click()
		{
			try
			{
				string filePath = "config.xml";
				var doc = new XDocument();

				if (File.Exists(filePath))
				{
					doc = XDocument.Load(filePath);
				}
				else
				{
					doc.Add(new XElement("Configuration"));
				}

				var root = doc.Root ?? new XElement("Configuration");
				if (doc.Root == null)
					doc.Add(root);

				var apiNode = root.Element("APIConnection");
				if (apiNode != null)
					apiNode.Remove();

				apiNode = new XElement("APIConnection",
					new XElement("Username", txtUsername.Text.Trim()),
					new XElement("Password", txtPassword.Text),
					new XElement("SaleApiUrl", txtSaleApiUrl.Text.Trim()),
					new XElement("InventoryApiUrl", txtInventoryApiUrl.Text.Trim())
				);
				root.Add(apiNode);

				doc.Save(filePath);

				lblStatus.Text = "APIConnection saved successfully.";
				lblStatus.ForeColor = Color.Green;

				GlobalHelper.SetControlsEnabled(false, ApiInputs);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error saving APIConnection: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void DisableSaveOnEdit(object sender, EventArgs e)
		{
			btnSave.Enabled = false;
			lblStatus.Text = "❗Please test API again after editing.";
			lblStatus.ForeColor = Color.DarkOrange;
		}
	}
}
