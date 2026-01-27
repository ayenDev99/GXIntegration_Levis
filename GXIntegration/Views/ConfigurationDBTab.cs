using Guna.UI2.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationDBTab : UserControl
	{
		private GXConfig config;

		private Guna2Button btnEdit, btnSave, btnTestConnection;
		private Guna2TextBox txtDbName, txtUser, txtPassword, txtHost, txtPort, txtDbType;
		//private GunaComboBox cmbDbType;
		private Label lblDbStatus;

		public ConfigurationDBTab(GXConfig config)
		{
			this.config = config;

			InitializeComponent();
			SetupDatabaseTab();
			LoadMainDbConnection();
		}

		private Control[] DbInputControls => new Control[]
		{
			txtDbName,
			txtUser,
			txtPassword,
			txtHost,
			txtPort,
			txtDbType
		};

		private void SetDbInputValues(string dbType, string dbName, string user, string password, string host, string port)
		{
			//cmbDbType.SelectedItem = dbType;
			txtDbName.Text = dbName;
			txtUser.Text = user;
			txtPassword.Text = password;
			txtHost.Text = host;
			txtPort.Text = port;
			txtDbType.Text = dbType;
		}

		// ***************************************************
		// Initialization Methods
		// ***************************************************
		private void SetupDatabaseTab()
		{
			AutoScroll = true;
			int inputStartX = 150;
			int labelStartX = 20;
			int currentY = 20;
			int spacingY = 40;

			// === Local Helper Methods ===
			Label CreateLabel(string text, int y) => new Label
			{
				Text = text,
				Location = new Point(labelStartX, y),
				Width = 120
			};

			Guna2TextBox CreateTextBox(int y, bool isPassword = false) => new Guna2TextBox
			{
				Location = new Point(inputStartX, y),
				Width = 200,
				//BaseColor = Color.White,
				ForeColor = Color.Black,
				PasswordChar = isPassword ? '*' : '\0'
			};

			// === Labels & Inputs ===
			Controls.Add(CreateLabel("Database Name", currentY));
			txtDbName = CreateTextBox(currentY);
			Controls.Add(txtDbName);
			currentY += spacingY;

			Controls.Add(CreateLabel("Username", currentY));
			txtUser = CreateTextBox(currentY);
			Controls.Add(txtUser);
			currentY += spacingY;

			Controls.Add(CreateLabel("Password", currentY));
			txtPassword = CreateTextBox(currentY, true);
			Controls.Add(txtPassword);
			currentY += spacingY;

			Controls.Add(CreateLabel("Hostname", currentY));
			txtHost = CreateTextBox(currentY);
			Controls.Add(txtHost);
			currentY += spacingY;

			Controls.Add(CreateLabel("Port", currentY));
			txtPort = CreateTextBox(currentY);
			Controls.Add(txtPort);
			currentY += spacingY;

			Controls.Add(CreateLabel("Database Type", currentY));
			txtDbType = CreateTextBox(currentY);
			Controls.Add(txtDbType);
			currentY += spacingY;

			// === Status Label ===
			lblDbStatus = new Label
			{
				Location = new Point(labelStartX, currentY),
				Width = 500,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			Controls.Add(lblDbStatus);
			currentY += spacingY;

			// === Buttons ===
			btnEdit = new Guna2Button
			{
				Text = "Edit",
				Location = new Point(370, 20),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGuna2Button(btnEdit, Color.FromArgb(33, 150, 243));
			btnEdit.Click += BtnEdit_Click;

			btnSave = new Guna2Button
			{
				Text = "Save",
				Location = new Point(370, 60),
				Size = new Size(80, 25),
				Enabled = false
			};
			GlobalHelper.StyleGuna2Button(btnSave, Color.FromArgb(76, 175, 80));
			btnSave.Click += BtnSave_Click;

			btnTestConnection = new Guna2Button
			{
				Text = "Test Connection",
				Location = new Point(inputStartX, currentY),
				Size = new Size(150, 25),
				Enabled = true
			};
			GlobalHelper.StyleGuna2Button(btnTestConnection, Color.FromArgb(138, 43, 226));
			btnTestConnection.Click += BtnTestConnection_Click;

			Controls.Add(btnEdit);
			Controls.Add(btnSave);
			Controls.Add(btnTestConnection);

			// === Events to Disable Save Button ===
			txtDbName.TextChanged += DisableSaveOnEdit;
			txtUser.TextChanged += DisableSaveOnEdit;
			txtPassword.TextChanged += DisableSaveOnEdit;
			txtHost.TextChanged += DisableSaveOnEdit;
			txtPort.TextChanged += DisableSaveOnEdit;
			txtDbType.TextChanged += DisableSaveOnEdit;
		}
		private void LoadMainDbConnection()
		{
			string filePath = "config.xml";
			if (!File.Exists(filePath))
			{
				lblDbStatus.Text = "config.xml not found.";
				return;
			}

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(filePath);
				XmlNode node = doc.DocumentElement.SelectSingleNode("MainDbConnection");

				if (node == null || string.IsNullOrWhiteSpace(node.InnerText))
				{
					lblDbStatus.Text = "No MainDbConnection found in config.xml.";
					return;
				}

				string connString = node.InnerText;

				string user = "", pass = "", host = "", port = "";

				string[] parts = connString.Split(';');
				foreach (var part in parts)
				{
					if (part.StartsWith("User Id=", StringComparison.OrdinalIgnoreCase))
						user = part.Substring("User Id=".Length);
					else if (part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
						pass = part.Substring("Password=".Length);
					else if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
					{
						string dataSource = part.Substring("Data Source=".Length);
						string trimmed = dataSource.TrimStart('/');
						string[] hostParts = trimmed.Split(new char[] { ':', '/' }, StringSplitOptions.RemoveEmptyEntries);
						if (hostParts.Length >= 3)
						{
							host = hostParts[0];
							port = hostParts[1];
						}
					}
				}

				// Fill inputs
				SetDbInputValues("Oracle", "MainDbConnection", user, pass, host, port);


				GlobalHelper.SetControlsEnabled(false, DbInputControls);
				btnEdit.Enabled = true;

				lblDbStatus.Text = "Loaded connection from config.xml";
				lblDbStatus.ForeColor = Color.DarkGreen;
			}
			catch (Exception ex)
			{
				lblDbStatus.Text = "Failed to load config.xml: " + ex.Message;
				lblDbStatus.ForeColor = Color.Red;
			}
		}
		private void SaveToConfig(string connectionName, string connectionString)
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
					var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
					doc.AppendChild(declaration);
					var root = doc.CreateElement("Configuration");
					doc.AppendChild(root);
				}

				var rootElement = doc.DocumentElement ?? doc.CreateElement("Configuration");
				if (doc.DocumentElement == null)
					doc.AppendChild(rootElement);

				var existingNode = rootElement.SelectSingleNode(connectionName);
				if (existingNode != null)
				{
					rootElement.RemoveChild(existingNode);
				}

				var newNode = doc.CreateElement(connectionName);
				newNode.InnerText = connectionString;
				rootElement.AppendChild(newNode);

				doc.Save(filePath);

				MessageBox.Show($"Saved '{connectionName}' to config.xml", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to save config.xml:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		// ***************************************************
		// BUtton Methods
		// ***************************************************
		private async void BtnTestConnection_Click(object sender, EventArgs e)
		{
			try
			{
				string user = txtUser.Text.Trim();
				string password = txtPassword.Text;
				string host = txtHost.Text.Trim();
				string port = txtPort.Text.Trim();
				string dbType = txtDbType.Text.Trim();

				if (dbType != "Oracle")
				{
					MessageBox.Show("Currently only Oracle is supported for testing.", "Unsupported", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				string connString = $"User Id={user};Password={password};Data Source=//{host}:{port}/RPROODS;";

				using (var conn = new OracleConnection(connString))
				{
					await conn.OpenAsync();
					MessageBox.Show("✅ Connection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

					lblDbStatus.Text = "Connection test passed.";
					lblDbStatus.ForeColor = Color.Green;

					btnSave.Enabled = true; // Enable Save only after success
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Connection failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				lblDbStatus.Text = "Connection test failed.";
				lblDbStatus.ForeColor = Color.Red;

				btnSave.Enabled = false; // Disable Save if test fails
			}
		}

		private void DisableSaveOnEdit(object sender, EventArgs e)
		{
			btnSave.Enabled = false;
			lblDbStatus.Text = "❗Please test connection again after editing.";
			lblDbStatus.ForeColor = Color.DarkOrange;
		}

		private void BtnEdit_Click(object sender, EventArgs e)
		{
			GlobalHelper.SetControlsEnabled(true, DbInputControls);
			btnEdit.Enabled = false;
			btnSave.Enabled = false;
			lblDbStatus.Text = "Editing enabled.";
			lblDbStatus.ForeColor = Color.Blue;
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			try
			{
				string connectionName = "MainDbConnection"; // fixed name
				string dbName = txtDbName.Text.Trim();
				string user = txtUser.Text.Trim();
				string password = txtPassword.Text;
				string host = txtHost.Text.Trim();
				string port = txtPort.Text.Trim();
				string dbType = txtDbType.Text.Trim();

				if (string.IsNullOrEmpty(dbName))
				{
					MessageBox.Show("Database Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				// Build connection string based on dbType
				string connString = "";
				if (dbType == "Oracle")
				{
					connString = $"User Id={user};Password={password};Data Source=//{host}:{port}/RPROODS;";
				}
				else if (dbType == "SQLite")
				{
					connString = $"Data Source={host};";  // Simplified example
				}
				else
				{
					// Add other db types here if needed
					connString = ""; // or show error
				}

				SaveToConfig(connectionName, connString);

				lblDbStatus.Text = "Connection saved successfully.";
				lblDbStatus.ForeColor = Color.Green;

				GlobalHelper.SetControlsEnabled(false, DbInputControls);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error saving config: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

	}
}
