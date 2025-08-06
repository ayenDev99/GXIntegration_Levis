using Oracle.ManagedDataAccess.Client;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using Guna.UI.WinForms;



namespace GXIntegration_Levis
{
	public partial class ConfigurationPage : UserControl
	{
		// Guna UI controls for database tab
		private GunaComboBox cmbDbType;
		private GunaTextBox txtDbName, txtUser, txtPassword, txtHost, txtPort;
		private GunaLabel lblDbStatus;
		private GunaButton btnEdit, btnSave, btnTestConnection;

		public ConfigurationPage()
		{
			InitializeComponent();

			// Create tabs
			TabPage databaseTab = new TabPage("Database");
			TabPage sftpTab = new TabPage("SFTP");

			// Setup database tab controls
			SetupDatabaseTab(databaseTab);
			SetupSftpTab(sftpTab);

			// Add tabs to TabControl
			tabControl1.TabPages.Add(databaseTab);
			tabControl1.TabPages.Add(sftpTab);

			// Load config if exists
			LoadMainDbConnection();
		}

		private void SetupDatabaseTab(TabPage tab)
		{
			tab.AutoScroll = true;

			// === Labels ===
			var lblDbName = new GunaLabel { Text = "Database Name", Location = new Point(20, 20), Width = 120 };
			var lblUser = new GunaLabel { Text = "Username", Location = new Point(20, 60), Width = 120 };
			var lblPassword = new GunaLabel { Text = "Password", Location = new Point(20, 100), Width = 120 };
			var lblHost = new GunaLabel { Text = "Hostname", Location = new Point(20, 140), Width = 120 };
			var lblPort = new GunaLabel { Text = "Port", Location = new Point(20, 180), Width = 120 };
			var lblDbType = new GunaLabel { Text = "Database Type", Location = new Point(20, 220), Width = 120 };

			tab.Controls.Add(lblDbName);
			tab.Controls.Add(lblUser);
			tab.Controls.Add(lblPassword);
			tab.Controls.Add(lblHost);
			tab.Controls.Add(lblPort);
			tab.Controls.Add(lblDbType);

			// === Inputs ===
			txtDbName = new GunaTextBox
			{
				Location = new Point(150, 20),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black
			};
			txtUser = new GunaTextBox
			{
				Location = new Point(150, 60),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black
			};
			txtPassword = new GunaTextBox
			{
				Location = new Point(150, 100),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black,
				PasswordChar = '*'
			};
			txtHost = new GunaTextBox
			{
				Location = new Point(150, 140),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black
			};
			txtPort = new GunaTextBox
			{
				Location = new Point(150, 180),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black
			};

			cmbDbType = new GunaComboBox
			{
				Location = new Point(150, 220),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			cmbDbType.Items.AddRange(new string[] { "Oracle", "SQLite", "MySQL", "SQL Server" });
			cmbDbType.SelectedIndex = 0;

			tab.Controls.Add(txtDbName);
			tab.Controls.Add(txtUser);
			tab.Controls.Add(txtPassword);
			tab.Controls.Add(txtHost);
			tab.Controls.Add(txtPort);
			tab.Controls.Add(cmbDbType);

			// === Status label ===
			lblDbStatus = new GunaLabel
			{
				Location = new Point(20, 260),
				Width = 500,
				ForeColor = Color.Gray,
				Text = "Ready"
			};
			tab.Controls.Add(lblDbStatus);

			// === Buttons ===
			btnSave = new GunaButton
			{
				Text = "Save",
				Location = new Point(370, 60),
				Size = new Size(80, 25),
				Enabled = false
			};
			StyleGunaButton(
				btnSave,
				baseColor: Color.FromArgb(76, 175, 80),      // Green
				hoverColor: Color.FromArgb(76, 195, 80),     // Lighter green hover
				pressedColor: Color.FromArgb(56, 142, 60),   // Darker green pressed
				borderColor: Color.FromArgb(76, 175, 80)
			);
			btnSave.Click += BtnSave_Click;


			btnEdit = new GunaButton
			{
				Text = "Edit",
				Location = new Point(370, 20),
				Size = new Size(80, 30),
				Enabled = false
			};
			StyleGunaButton(
				btnEdit,
				baseColor: Color.FromArgb(33, 150, 243),      // Blue
				hoverColor: Color.FromArgb(53, 170, 243),     // Lighter blue hover
				pressedColor: Color.FromArgb(20, 110, 200),   // Darker blue pressed
				borderColor: Color.FromArgb(33, 150, 243)
			);
			btnEdit.Click += BtnEdit_Click;


			btnTestConnection = new GunaButton
			{
				Text = "Test Connection",
				Location = new Point(150, 290),
				Size = new Size(150, 35),
				Enabled = true
			};
			StyleGunaButton(
				btnTestConnection,
				baseColor: Color.FromArgb(138, 43, 226),      // BlueViolet (medium purple)
				hoverColor: Color.FromArgb(100, 32, 165),     // Darker BlueViolet for hover
				pressedColor: Color.FromArgb(75, 24, 123),    // Even darker pressed
				borderColor: Color.FromArgb(138, 43, 226)     // Same as base for border
			);
			btnTestConnection.Click += BtnTestConnection_Click;
			tab.Controls.Add(btnEdit);
			tab.Controls.Add(btnSave);
			tab.Controls.Add(btnTestConnection);

			// Disable Save when user edits any input
			txtDbName.TextChanged += DisableSaveOnEdit;
			txtUser.TextChanged += DisableSaveOnEdit;
			txtPassword.TextChanged += DisableSaveOnEdit;
			txtHost.TextChanged += DisableSaveOnEdit;
			txtPort.TextChanged += DisableSaveOnEdit;
			cmbDbType.SelectedIndexChanged += DisableSaveOnEdit;
		}

		private void SetupSftpTab(TabPage tab)
		{
			tab.AutoScroll = true;

			// Labels
			var lblHost = new GunaLabel { Text = "Hostname", Location = new Point(20, 20), Width = 120 };
			var lblPort = new GunaLabel { Text = "Port", Location = new Point(20, 60), Width = 120 };
			var lblUser = new GunaLabel { Text = "Username", Location = new Point(20, 100), Width = 120 };
			var lblPassword = new GunaLabel { Text = "Password", Location = new Point(20, 140), Width = 120 };

			tab.Controls.Add(lblHost);
			tab.Controls.Add(lblPort);
			tab.Controls.Add(lblUser);
			tab.Controls.Add(lblPassword);

			// Inputs
			var txtHost = new GunaTextBox
			{
				Location = new Point(150, 20),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black
			};

			var txtPort = new GunaTextBox
			{
				Location = new Point(150, 60),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black,
				Text = "22" // default SFTP port
			};

			var txtUser = new GunaTextBox
			{
				Location = new Point(150, 100),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black
			};

			var txtPassword = new GunaTextBox
			{
				Location = new Point(150, 140),
				Width = 200,
				BaseColor = Color.White,
				ForeColor = Color.Black,
				PasswordChar = '*'
			};

			tab.Controls.Add(txtHost);
			tab.Controls.Add(txtPort);
			tab.Controls.Add(txtUser);
			tab.Controls.Add(txtPassword);

			// Buttons
			var btnTestSftp = new GunaButton
			{
				Text = "Test SFTP Connection",
				Location = new Point(150, 190),
				Size = new Size(160, 35)
			};

			StyleGunaButton(
				btnTestSftp,
				baseColor: Color.FromArgb(0, 123, 255),        // Blue
				hoverColor: Color.FromArgb(30, 144, 255),
				pressedColor: Color.FromArgb(0, 102, 204),
				borderColor: Color.FromArgb(0, 123, 255)
			);

			btnTestSftp.Click += (s, e) =>
			{
				// Placeholder: Add your SFTP test logic here
				MessageBox.Show("SFTP connection test clicked!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
			};

			tab.Controls.Add(btnTestSftp);
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
				string connectionName = "MainDbConnection"; // fixed name
				string dbName = txtDbName.Text.Trim();
				string user = txtUser.Text.Trim();
				string password = txtPassword.Text;
				string host = txtHost.Text.Trim();
				string port = txtPort.Text.Trim();
				string dbType = cmbDbType.SelectedItem?.ToString() ?? "Oracle";

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

				SetInputsEnabled(false);
				btnEdit.Enabled = true;
				btnSave.Enabled = false;
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

		private void SetInputsEnabled(bool enabled)
		{
			txtDbName.Enabled = enabled;
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
						}
					}
				}

				// Fill inputs
				cmbDbType.SelectedItem = "Oracle";
				txtDbName.Text = "MainDbConnection"; // fixed name shown here as DB name (or you can leave blank)
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

		private void StyleGunaButton(GunaButton button, Color baseColor, Color hoverColor, Color pressedColor, Color borderColor)
		{
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

	}
}
