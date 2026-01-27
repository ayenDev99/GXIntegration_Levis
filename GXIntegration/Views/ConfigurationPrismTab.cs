using Guna.UI2.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationPrismTab : UserControl
	{
		private Guna2TextBox txtAddress, txtUsername, txtPassword, txtWorkstationName;
		private Guna2Button btnEdit, btnSave, btnTestAuth;
		private Label lblStatus;

		public ConfigurationPrismTab(GXConfig config)
		{
			InitializeComponent();
			SetupPrismTab();
			LoadPrismConfig();
		}

		private Control[] PrismInputs => new Control[]
		{
			txtAddress,
			txtUsername,
			txtPassword,
			txtWorkstationName
		};

		private void SetupPrismTab()
		{
			AutoScroll = true;
			int inputStartX = 150;
			int labelStartX = 20;
			int currentY = 20;
			int spacingY = 40;

			// Local helpers
			Label CreateLabel(string text, int y) => new Label
			{
				Text = text,
				Location = new Point(labelStartX, y),
				Width = 120
			};

			Guna2TextBox CreateTextBox(int y, bool isPassword = false) => new Guna2TextBox
			{
				Location = new Point(inputStartX, y),
				Width = 250,
				//BaseColor = Color.White,
				ForeColor = Color.Black,
				PasswordChar = isPassword ? '*' : '\0'
			};

			// Inputs
			Controls.Add(CreateLabel("Address", currentY));
			txtAddress = CreateTextBox(currentY);
			Controls.Add(txtAddress);
			currentY += spacingY;

			Controls.Add(CreateLabel("Username", currentY));
			txtUsername = CreateTextBox(currentY);
			Controls.Add(txtUsername);
			currentY += spacingY;

			Controls.Add(CreateLabel("Password", currentY));
			txtPassword = CreateTextBox(currentY, true);
			Controls.Add(txtPassword);
			currentY += spacingY;

			Controls.Add(CreateLabel("Workstation", currentY));
			txtWorkstationName = CreateTextBox(currentY);
			Controls.Add(txtWorkstationName);
			currentY += spacingY;

			// Status
			lblStatus = new Label
			{
				Location = new Point(labelStartX, currentY),
				Width = 500,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			Controls.Add(lblStatus);
			currentY += spacingY;

			// Buttons
			btnEdit = new Guna2Button
			{
				Text = "Edit",
				Location = new Point(420, 20),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGuna2Button(btnEdit, Color.FromArgb(33, 150, 243));
			btnEdit.Click += BtnEdit_Click;

			btnSave = new Guna2Button
			{
				Text = "Save",
				Location = new Point(420, 60),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGuna2Button(btnSave, Color.FromArgb(76, 175, 80));
			btnSave.Click += BtnSave_Click;

			btnTestAuth = new Guna2Button
			{
				Text = "Test Authentication",
				Location = new Point(inputStartX, currentY),
				Size = new Size(180, 25),
				Enabled = true
			};
			GlobalHelper.StyleGuna2Button(btnTestAuth, Color.FromArgb(138, 43, 226));
			btnTestAuth.Click += async (s, e) => await BtnTestAuth_Click();

			Controls.AddRange(new Control[] { btnEdit, btnSave, btnTestAuth });

			// Events
			txtAddress.TextChanged += DisableSaveOnEdit;
			txtUsername.TextChanged += DisableSaveOnEdit;
			txtPassword.TextChanged += DisableSaveOnEdit;
			txtWorkstationName.TextChanged += DisableSaveOnEdit;
		}

		private void LoadPrismConfig()
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
				var prismNode = doc.Root.Element("PrismConfig");
				if (prismNode == null)
				{
					lblStatus.Text = "No PrismConfig section found in config.xml.";
					return;
				}

				txtAddress.Text = prismNode.Element("Address")?.Value ?? "";
				txtUsername.Text = prismNode.Element("Username")?.Value ?? "";
				txtPassword.Text = prismNode.Element("Password")?.Value ?? "";
				txtWorkstationName.Text = prismNode.Element("WorkstationName")?.Value ?? "";

				GlobalHelper.SetControlsEnabled(false, PrismInputs);
				btnEdit.Enabled = true;

				lblStatus.Text = "Loaded PrismConfig from config.xml";
				lblStatus.ForeColor = Color.DarkGreen;
			}
			catch (Exception ex)
			{
				lblStatus.Text = "Failed to load PrismConfig: " + ex.Message;
				lblStatus.ForeColor = Color.Red;
			}
		}

		private async Task BtnTestAuth_Click()
		{
			try
			{
				string addr = txtAddress.Text.Trim();
				string user = txtUsername.Text.Trim();
				string pass = txtPassword.Text;
				string ws = txtWorkstationName.Text.Trim();

				// Your existing authentication call
				string session = await Authenticate(addr, user, pass, ws);

				if (string.IsNullOrEmpty(session))
				{
					MessageBox.Show("❌ Authentication failed. Please check configuration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					lblStatus.Text = "Authentication failed.";
					lblStatus.ForeColor = Color.Red;
					btnSave.Enabled = false;
				}
				else
				{
					MessageBox.Show("✅ Authentication successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					lblStatus.Text = "Authentication passed.";
					lblStatus.ForeColor = Color.Green;
					btnSave.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Error: " + ex.Message, "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				lblStatus.Text = "Auth error.";
				lblStatus.ForeColor = Color.Red;
			}
		}

		private void BtnEdit_Click(object sender, EventArgs e)
		{
			GlobalHelper.SetControlsEnabled(true, PrismInputs);
			btnEdit.Enabled = false;
			btnSave.Enabled = false;
			lblStatus.Text = "Editing enabled.";
			lblStatus.ForeColor = Color.Blue;
		}

		private void BtnSave_Click(object sender, EventArgs e)
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

				var prismNode = root.Element("PrismConfig");
				if (prismNode != null)
					prismNode.Remove();

				prismNode = new XElement("PrismConfig",
					new XElement("Address", txtAddress.Text.Trim()),
					new XElement("Username", txtUsername.Text.Trim()),
					new XElement("Password", txtPassword.Text),
					new XElement("WorkstationName", txtWorkstationName.Text.Trim())
				);
				root.Add(prismNode);

				doc.Save(filePath);

				lblStatus.Text = "PrismConfig saved successfully.";
				lblStatus.ForeColor = Color.Green;

				GlobalHelper.SetControlsEnabled(false, PrismInputs);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error saving PrismConfig: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void DisableSaveOnEdit(object sender, EventArgs e)
		{
			btnSave.Enabled = false;
			lblStatus.Text = "❗Please test authentication again after editing.";
			lblStatus.ForeColor = Color.DarkOrange;
		}

		public async Task<string> Authenticate(string prismAddress, string prismUsername, string prismPassword, string workstationName)
		{
			try
			{
				// Step 1: Get Auth-Nonce
				var nonceRequest = WebRequest.CreateHttp($"{prismAddress}/v1/rest/auth");
				nonceRequest.Method = "GET";
				nonceRequest.Accept = "application/json";
				nonceRequest.ContentType = "application/json; charset=UTF-8";

				long nonce;
				using (var response = nonceRequest.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						Logger.LogError($"Failed to get Auth-Nonce. Status: {response.StatusCode}");
						return null;
					}

					nonce = long.Parse(response.Headers["Auth-Nonce"]);
				}

				// Step 2: Compute Nonce Response
				long nonceResponse = nonce / 13L % 99999L * 17L;

				// Step 3: Authenticate with credentials
				var authUrl = $"{prismAddress}/v1/rest/auth?usr={prismUsername}&pwd={prismPassword}";
				var loginRequest = WebRequest.CreateHttp(authUrl);
				loginRequest.Method = "GET";
				loginRequest.Accept = "application/json";
				loginRequest.ContentType = "application/json; charset=UTF-8";
				loginRequest.Headers.Add("Auth-Nonce", nonce.ToString());
				loginRequest.Headers.Add("Auth-Nonce-Response", nonceResponse.ToString());

				string authSession;
				using (var response = loginRequest.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						Logger.LogError($"Login failed. Status: {response.StatusCode}");
						return null;
					}

					authSession = response.Headers["Auth-Session"];
				}

				// Step 4: Bind session to workstation
				var sitUrl = $"{prismAddress}/v1/rest/sit?ws={workstationName}";
				var sitRequest = WebRequest.CreateHttp(sitUrl);
				sitRequest.Method = "GET";
				sitRequest.Accept = "application/json";
				sitRequest.ContentType = "application/json; charset=UTF-8";
				sitRequest.Headers.Add("Auth-Session", authSession);

				using (var response = sitRequest.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						Logger.LogError($"Workstation bind failed. Status: {response.StatusCode}");
						return null;
					}
				}

				return authSession;
			}
			catch (WebException ex)
			{
				string errorMessage = "WebException occurred.";
				if (ex.Response != null)
				{
					using (var reader = new StreamReader(ex.Response.GetResponseStream()))
					{
						var errorResponse = reader.ReadToEnd();
						errorMessage += $" Response: {errorResponse}";
						Logger.LogError(errorMessage + ex);
						return null;
					}
				}

				Logger.LogError($"{errorMessage} Exception: {ex.Message}" + ex);
				return null;
			}
			catch (Exception ex)
			{
				Logger.LogError($"Unexpected error: {ex.Message}" + ex);
				return null;
			}
		}

	}
}
