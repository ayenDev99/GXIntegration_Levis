using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Net.Http;
using System.Threading.Tasks;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationAPITab : UserControl
	{
		private GunaTextBox txtApiUrl, txtApiUsername, txtApiPassword;
		private GunaButton btnEdit, btnSave, btnTestApi;
		private GunaLabel lblApiStatus;
		private GunaDataGridView dgvApiUrls;

		private Control[] ApiInputControls => new Control[]
		{
			txtApiUrl, txtApiUsername, txtApiPassword
		};

		public ConfigurationAPITab()
		{
			InitializeComponent();
			SetupApiTab();
			LoadApiSettings();
		}

		private void SetupApiTab()
		{
			AutoScroll = true;
			int labelX = 20;
			int inputX = 150;
			int currentY = 20;
			int spacingY = 40;

			// === API URL ===
			Controls.Add(GlobalHelper.CreateLabel("API URL", labelX, currentY));
			txtApiUrl = GlobalHelper.CreateTextBox(inputX, currentY, "400");
			Controls.Add(txtApiUrl);
			currentY += spacingY;

			// === Username ===
			Controls.Add(GlobalHelper.CreateLabel("Username", labelX, currentY));
			txtApiUsername = GlobalHelper.CreateTextBox(inputX, currentY, "200");
			Controls.Add(txtApiUsername);
			currentY += spacingY;

			// === Password ===
			Controls.Add(GlobalHelper.CreateLabel("Password", labelX, currentY));
			txtApiPassword = GlobalHelper.CreateTextBox(inputX, currentY, "200");
			txtApiPassword.UseSystemPasswordChar = true;
			Controls.Add(txtApiPassword);
			currentY += spacingY;

			// === Status Label ===
			lblApiStatus = new GunaLabel
			{
				Location = new Point(labelX, currentY),
				Width = 600,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			Controls.Add(lblApiStatus);
			currentY += spacingY;

			// === Buttons ===
			btnEdit = new GunaButton
			{
				Text = "Edit",
				Location = new Point(570, 20),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGunaButton(btnEdit, Color.FromArgb(33, 150, 243));
			btnEdit.Click += BtnEdit_Click;

			btnSave = new GunaButton
			{
				Text = "Save",
				Location = new Point(570, 60),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGunaButton(btnSave, Color.FromArgb(76, 175, 80));
			btnSave.Click += BtnSave_Click;

			btnTestApi = new GunaButton
			{
				Text = "Test API Connection",
				Location = new Point(inputX, currentY),
				Size = new Size(160, 25)
			};
			GlobalHelper.StyleGunaButton(btnTestApi, Color.FromArgb(138, 43, 226));
			btnTestApi.Click += async (s, e) => await BtnTestApi_Click(s, e);

			Controls.Add(btnEdit);
			Controls.Add(btnSave);
			Controls.Add(btnTestApi);

			// === DataGridView ===
			dgvApiUrls = new GunaDataGridView
			{
				Location = new Point(20, currentY + 40),
				Size = new Size(630, 200),
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
			};
			dgvApiUrls.Columns.Add("Url", "API URL");
			dgvApiUrls.Columns.Add("Username", "Username");
			dgvApiUrls.CellClick += DgvApiUrls_CellClick;
			Controls.Add(dgvApiUrls);

			// === Events ===
			txtApiUrl.TextChanged += DisableSaveOnEdit;
			txtApiUsername.TextChanged += DisableSaveOnEdit;
			txtApiPassword.TextChanged += DisableSaveOnEdit;
		}

		private void DisableSaveOnEdit(object sender, EventArgs e)
		{
			btnSave.Enabled = false;
			lblApiStatus.Text = "❗Please test connection again after editing.";
			lblApiStatus.ForeColor = Color.DarkOrange;
		}

		private void BtnEdit_Click(object sender, EventArgs e)
		{
			GlobalHelper.SetControlsEnabled(true, ApiInputControls);
			btnEdit.Enabled = false;
			btnSave.Enabled = false;

			lblApiStatus.Text = "Editing enabled.";
			lblApiStatus.ForeColor = Color.Blue;
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			if (!ValidateApiInputs()) return;

			SaveApiSettings();
		}

		private async Task BtnTestApi_Click(object sender, EventArgs e)
		{
			if (!ValidateApiInputs()) return;

			string apiUrl = txtApiUrl.Text.Trim();
			string username = txtApiUsername.Text.Trim();
			string password = txtApiPassword.Text.Trim();

			try
			{
				using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
				{
					if (!string.IsNullOrWhiteSpace(username))
					{
						var byteArray = System.Text.Encoding.ASCII.GetBytes($"{username}:{password}");
						client.DefaultRequestHeaders.Authorization =
							new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
					}

					var response = await client.GetAsync(apiUrl);

					if (response.IsSuccessStatusCode)
					{
						string content = await response.Content.ReadAsStringAsync();

						MessageBox.Show(
							"✅ API test successful!\n" +
							$"Response code: {(int)response.StatusCode}\n" +
							$"Content preview:\n{content.Substring(0, Math.Min(300, content.Length))}…",
							"Success",
							MessageBoxButtons.OK,
							MessageBoxIcon.Information
						);

						lblApiStatus.Text = "Connection test passed.";
						lblApiStatus.ForeColor = Color.Green;
						btnSave.Enabled = true;
					}
					else
					{
						string error = await response.Content.ReadAsStringAsync();
						throw new Exception($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}\n{error}");
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ API connection failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				lblApiStatus.Text = "Connection test failed.";
				lblApiStatus.ForeColor = Color.Red;
				btnSave.Enabled = false;
			}
		}

		private bool ValidateApiInputs()
		{
			string url = txtApiUrl.Text.Trim();

			if (string.IsNullOrWhiteSpace(url))
			{
				MessageBox.Show("API URL is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) ||
				!(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
			{
				MessageBox.Show("API URL is not valid or must start with http/https.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			return true;
		}

		private void LoadApiSettings()
		{
			dgvApiUrls.Rows.Clear();

			try
			{
				string filePath = "config.xml";
				if (!File.Exists(filePath))
				{
					lblApiStatus.Text = "config.xml not found.";
					return;
				}

				XmlDocument doc = new XmlDocument();
				doc.Load(filePath);

				var nodes = doc.SelectNodes("/Configuration/ApiConnections/ApiConnection");

				if (nodes == null || nodes.Count == 0)
				{
					lblApiStatus.Text = "No API URLs in config.xml.";
					return;
				}

				foreach (XmlNode node in nodes)
				{
					string url = node["Url"]?.InnerText ?? "";
					string username = node["Username"]?.InnerText ?? "";
					dgvApiUrls.Rows.Add(url, username);
				}

				lblApiStatus.Text = "Loaded API URLs.";
				lblApiStatus.ForeColor = Color.DarkGreen;
			}
			catch (Exception ex)
			{
				lblApiStatus.Text = "Error loading config: " + ex.Message;
				lblApiStatus.ForeColor = Color.Red;
			}
		}

		private void SaveApiSettings()
		{
			try
			{
				string filePath = "config.xml";
				XmlDocument doc = new XmlDocument();

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
				var apiConnectionsNode = root.SelectSingleNode("ApiConnections");

				if (apiConnectionsNode != null)
					root.RemoveChild(apiConnectionsNode);

				apiConnectionsNode = doc.CreateElement("ApiConnections");

				XmlElement apiNode = doc.CreateElement("ApiConnection");

				XmlElement urlElem = doc.CreateElement("Url");
				urlElem.InnerText = txtApiUrl.Text.Trim();
				apiNode.AppendChild(urlElem);

				XmlElement userElem = doc.CreateElement("Username");
				userElem.InnerText = txtApiUsername.Text.Trim();
				apiNode.AppendChild(userElem);

				XmlElement passElem = doc.CreateElement("Password");
				passElem.InnerText = txtApiPassword.Text.Trim();
				apiNode.AppendChild(passElem);

				apiConnectionsNode.AppendChild(apiNode);
				root.AppendChild(apiConnectionsNode);

				doc.Save(filePath);

				MessageBox.Show("API URL saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

				LoadApiSettings(); // Reload table
				GlobalHelper.SetControlsEnabled(false, ApiInputControls);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;
				lblApiStatus.Text = "Saved successfully.";
				lblApiStatus.ForeColor = Color.Green;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to save config:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				lblApiStatus.Text = "Failed to save.";
				lblApiStatus.ForeColor = Color.Red;
			}
		}

		private void DgvApiUrls_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0)
			{
				string selectedUrl = dgvApiUrls.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? "";
				string selectedUsername = dgvApiUrls.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";

				txtApiUrl.Text = selectedUrl;
				txtApiUsername.Text = selectedUsername;

				// Load password from config
				string filePath = "config.xml";
				XmlDocument doc = new XmlDocument();
				doc.Load(filePath);
				var nodes = doc.SelectNodes("/Configuration/ApiConnections/ApiConnection");

				foreach (XmlNode node in nodes)
				{
					if (node["Url"]?.InnerText == selectedUrl)
					{
						txtApiPassword.Text = node["Password"]?.InnerText ?? "";
						break;
					}
				}

				GlobalHelper.SetControlsEnabled(false, ApiInputControls);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;

				lblApiStatus.Text = "Loaded selected API URL.";
				lblApiStatus.ForeColor = Color.DarkGreen;
			}
		}
	}
}
