using Oracle.ManagedDataAccess.Client;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace GXIntegration_Levis
{
	public partial class ConfigurationPage : UserControl
	{
		// Controls for database tab
		private ComboBox cmbDbType;
		private TextBox txtConnName, txtHost, txtPort, txtUser, txtPassword;
		private Label lblDbStatus;
		private Button btnEdit, btnSave;
		private TabPage databaseTab;
		private Button btnTestConnection;

		public ConfigurationPage()
		{
			InitializeComponent();

			// Create tabs
			databaseTab = new TabPage("Database");
			TabPage apiTab = new TabPage("API");
			TabPage sftpTab = new TabPage("SFTP");
			TabPage timingTab = new TabPage("Timing");

			// Setup database tab controls
			SetupDatabaseTab(databaseTab);

			// Add placeholders for other tabs
			apiTab.Controls.Add(new Label() { Text = "API Connection here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
			sftpTab.Controls.Add(new Label() { Text = "SFTP Connection here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
			timingTab.Controls.Add(new Label() { Text = "Timing of Generation here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

			// Add tabs to TabControl
			tabControl1.TabPages.Add(databaseTab);
			tabControl1.TabPages.Add(apiTab);
			tabControl1.TabPages.Add(sftpTab);
			tabControl1.TabPages.Add(timingTab);

			// Load config if exists
			LoadMainDbConnection();
		}

		private void SetupDatabaseTab(TabPage tab)
		{
			// Optional: Enable scrolling if contents exceed tab bounds
			tab.AutoScroll = true;

			// === Labels ===
			tab.Controls.Add(new Label { Text = "Connection Name", Location = new Point(20, 20), Width = 120 });
			tab.Controls.Add(new Label { Text = "Username", Location = new Point(20, 60), Width = 120 });
			tab.Controls.Add(new Label { Text = "Password", Location = new Point(20, 100), Width = 120 });
			tab.Controls.Add(new Label { Text = "Hostname", Location = new Point(20, 140), Width = 120 });
			tab.Controls.Add(new Label { Text = "Port", Location = new Point(20, 180), Width = 120 });
			tab.Controls.Add(new Label { Text = "Database Type", Location = new Point(20, 220), Width = 120 });

			// === Inputs ===
			txtConnName = new TextBox { Location = new Point(150, 20), Width = 200 };
			txtUser = new TextBox { Location = new Point(150, 60), Width = 200 };
			txtPassword = new TextBox { Location = new Point(150, 100), Width = 200, PasswordChar = '*' };
			txtHost = new TextBox { Location = new Point(150, 140), Width = 200 };
			txtPort = new TextBox { Location = new Point(150, 180), Width = 200 };
			cmbDbType = new ComboBox
			{
				Location = new Point(150, 220),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			cmbDbType.Items.AddRange(new string[] { "Oracle", "SQLite", "MySQL", "SQL Server" });
			cmbDbType.SelectedIndex = 0;

			tab.Controls.Add(txtConnName);
			tab.Controls.Add(txtUser);
			tab.Controls.Add(txtPassword);
			tab.Controls.Add(txtHost);
			tab.Controls.Add(txtPort);
			tab.Controls.Add(cmbDbType);

			// === Status label ===
			lblDbStatus = new Label
			{
				Location = new Point(20, 270),
				Width = 500,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			tab.Controls.Add(lblDbStatus);

			// === Buttons ===
			btnEdit = new Button
			{
				Text = "Edit",
				Location = new Point(370, 20),
				Size = new Size(80, 30),
				Enabled = false
			};
			btnEdit.Click += BtnEdit_Click;

			btnSave = new Button
			{
				Text = "Save",
				Location = new Point(370, 60),
				Size = new Size(80, 30),
				Enabled = false
			};
			btnSave.Click += BtnSave_Click;

			btnTestConnection = new Button
			{
				Text = "Test Connection",
				Location = new Point(150, 310),
				Size = new Size(150, 35)
			};
			btnTestConnection.Click += BtnTestConnection_Click;

			tab.Controls.Add(btnEdit);
			tab.Controls.Add(btnSave);
			tab.Controls.Add(btnTestConnection);
		}

		private void BtnEdit_Click(object sender, EventArgs e)
		{
			SetInputsEnabled(true);
			btnEdit.Enabled = false;
			btnSave.Enabled = false;
			lblDbStatus.Text = "Editing enabled.";
			lblDbStatus.ForeColor = Color.Blue;
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			try
			{
				string connName = txtConnName.Text.Trim();
				string user = txtUser.Text.Trim();
				string password = txtPassword.Text;
				string host = txtHost.Text.Trim();
				string port = txtPort.Text.Trim();
				string dbType = cmbDbType.SelectedItem?.ToString() ?? "Oracle";

				if (string.IsNullOrEmpty(connName))
				{
					MessageBox.Show("Connection Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

				SaveToConfig(connName, connString);

				lblDbStatus.Text = "Connection saved successfully.";
				lblDbStatus.ForeColor = Color.Green;

				SetInputsEnabled(false);
				btnEdit.Enabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error saving config: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async void BtnTestConnection_Click(object sender, EventArgs e)
		{
			try
			{
				string user = txtUser.Text.Trim();
				string password = txtPassword.Text;
				string host = txtHost.Text.Trim();
				string port = txtPort.Text.Trim();
				string dbType = cmbDbType.SelectedItem?.ToString();

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

					btnSave.Enabled = true; // ✅ Enable Save only after success
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Connection failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				lblDbStatus.Text = "Connection test failed.";
				lblDbStatus.ForeColor = Color.Red;

				btnSave.Enabled = false; // ❌ Disable Save if test fails
			}
		}

		private void SetInputsEnabled(bool enabled)
		{
			txtConnName.Enabled = enabled;
			txtUser.Enabled = enabled;
			txtPassword.Enabled = enabled;
			txtHost.Enabled = enabled;
			txtPort.Enabled = enabled;
			cmbDbType.Enabled = enabled;
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
				//XmlNode node = doc.SelectSingleNode("/Configuration/MainDbConnection");
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
						string dataSource = part.Substring("Data Source=".Length); // e.g. //localhost:1521/RPROODS
						string trimmed = dataSource.TrimStart('/');
						string[] hostParts = trimmed.Split(new char[] { ':', '/' }, StringSplitOptions.RemoveEmptyEntries);
						if (hostParts.Length >= 3)
						{
							host = hostParts[0];
							port = hostParts[1];
							// service name hostParts[2] ignored here
						}
					}
				}

				// Fill inputs
				cmbDbType.SelectedItem = "Oracle";
				txtConnName.Text = "MainDbConnection";
				txtUser.Text = user;
				txtPassword.Text = pass;
				txtHost.Text = host;
				txtPort.Text = port;

				SetInputsEnabled(false);
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

		private void DisableSaveOnEdit(object sender, EventArgs e)
		{
			btnSave.Enabled = false;
			lblDbStatus.Text = "❗Please test connection again after editing.";
			lblDbStatus.ForeColor = Color.DarkOrange;
		}

	}
}
