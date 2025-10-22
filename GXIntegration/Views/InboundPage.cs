using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.InboundHandlers;
using Renci.SshNet;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class InboundPage : UserControl
	{
		static GXConfig config;
		private GunaDataGridView guna1DataGridView1;

		private DateTimePicker datePickerFrom;
		private DateTimePicker datePickerTo;
		private Label lblFrom;
		private Label lblTo;
		private GunaButton btnSaveToPrism;

		private PrismRepository _prismRepository;

		private readonly InboundEmployee inboundEmployee = new InboundEmployee();
		private readonly InboundItem inboundItem = new InboundItem();
		private readonly InboundHierarchy inboundHierarchy = new InboundHierarchy();
		private readonly InboundASN inboundAsn = new InboundASN();
		private readonly InboundPrice inboundPrice = new InboundPrice();

		private string configPath;
		public InboundPage()
		{
			GlobalInbound.Initialize();
			configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			_prismRepository = new PrismRepository(config.MainDbConnection);

			InitializeComponent();
			InitializeGrid();
			InitializeControls();
		}

		// ***************************************************
		// Initialization
		// ***************************************************
		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(250, 90),
				Size = new Size(620, 180),
				AllowUserToAddRows = false,
				ScrollBars = ScrollBars.Both,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				GridColor = Color.LightGray,
				Theme = GunaDataGridViewPresetThemes.Guna
			};

			guna1DataGridView1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
			guna1DataGridView1.ThemeStyle.HeaderStyle.ForeColor = Color.White;
			guna1DataGridView1.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
			guna1DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

			var checkboxColumn = new DataGridViewCheckBoxColumn
			{
				Name = "Select",
				HeaderText = "",
				Width = 45
			};
			guna1DataGridView1.Columns.Add(checkboxColumn);

			CheckBox selectAllCheckbox = new CheckBox
			{
				Size = new Size(15, 15),
				BackColor = Color.Transparent
			};
			guna1DataGridView1.Controls.Add(selectAllCheckbox);

			void PositionSelectAll()
			{
				if (guna1DataGridView1.Columns["Select"] == null) return;
				Rectangle rect = guna1DataGridView1.GetCellDisplayRectangle(
					guna1DataGridView1.Columns["Select"].Index, -1, true);
				selectAllCheckbox.Location = new Point(
					rect.Left + (rect.Width - selectAllCheckbox.Width) / 2,
					rect.Top + (rect.Height - selectAllCheckbox.Height) / 2
				);
			}

			guna1DataGridView1.ColumnWidthChanged += (s, e) => PositionSelectAll();
			guna1DataGridView1.Scroll += (s, e) => PositionSelectAll();
			guna1DataGridView1.SizeChanged += (s, e) => PositionSelectAll();
			guna1DataGridView1.DataBindingComplete += (s, e) => PositionSelectAll();
			guna1DataGridView1.CellPainting += (s, e) =>
			{
				if (e.RowIndex == -1 && e.ColumnIndex == guna1DataGridView1.Columns["Select"].Index)
					PositionSelectAll();
			};

			selectAllCheckbox.CheckedChanged += (s, e) =>
			{
				guna1DataGridView1.EndEdit();
				foreach (DataGridViewRow row in guna1DataGridView1.Rows)
				{
					row.Cells["Select"].Value = selectAllCheckbox.Checked;
				}
			};

			guna1DataGridView1.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "ID", Width = 30 },
				new DataGridViewTextBoxColumn { Name = "Name", Width = 200 },
				new DataGridViewTextBoxColumn { Name = "File Name Format", Width = 300 },
				new DataGridViewTextBoxColumn { Name = "File", Width = 45 }
			);

			void AddRow(string id, string name, string format, string type)
				=> guna1DataGridView1.Rows.Add(false, id, name, format, type);

			AddRow("1", "EMPLOYEE DETAILS", "LSPI_WD_[yyyymmddhhmmss]", ".csv");
			AddRow("2", "ITEM DETAILS", "LSPI_ITEM_[yyyymmddhhmmss]", ".txt");
			AddRow("3", "HIERARCHY DETAILS", "LSPI_HIERARCHY_[yyyymmddhhmmss]", ".txt");
			AddRow("4", "PRICE DETAILS", "LSPI_PRTAR_[yyyymmddhhmmss]", ".txt");
			AddRow("5", "ASN DETAILS", "LSPI_PRTRDX_[yyyymmddhhmmss]", ".txt");

			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			this.Controls.Add(guna1DataGridView1);

			this.Load += (s, e) => PositionSelectAll();
		}

		private void InitializeControls()
		{
			// --------------------
			// Date Range Controls
			// --------------------
			lblFrom = new Label
			{
				Text = "From:",
				Location = new Point(250, 54),
				AutoSize = true
			};

			datePickerFrom = new DateTimePicker
			{
				Location = new Point(300, 50),
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy-MM-dd hh:mm tt",
				Width = 160,
				ShowUpDown = false,
				Value = DateTime.Today
			};

			lblTo = new Label
			{
				Text = "To:",
				Location = new Point(480, 54),
				AutoSize = true
			};

			datePickerTo = new DateTimePicker
			{
				Location = new Point(520, 50),
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy-MM-dd hh:mm tt",
				Width = 160,
				ShowUpDown = false,
				Value = DateTime.Today.AddDays(1).AddSeconds(-1)
			};

			// --------------------
			// Add to Control
			// --------------------
			this.Controls.Add(lblFrom);
			this.Controls.Add(datePickerFrom);
			this.Controls.Add(lblTo);
			this.Controls.Add(datePickerTo);

			// --------------------
			// Send Button
			// --------------------
			btnSaveToPrism = GlobalHelper.CreateButton(
				text: "Save Data to Prism",
				location: new Point(250, 270),
				clickAction: async () => await ManualProcessAsync()
			);

			this.Controls.Add(btnSaveToPrism);
		}

		public async Task ManualProcessAsync()
		{
			Logger.Log("[INBOUND-MANUAL] Start Manual Process...");

			try
			{
				var globalInbound = new GlobalInbound();

				string session = await globalInbound.AuthenticateFromConfigAsync();
				if (session == null)
					return;

				var selectedModules = guna1DataGridView1.Rows
					.Cast<DataGridViewRow>()
					.Where(r => Convert.ToBoolean(r.Cells["Select"].Value) == true)
					.Select(r => r.Cells["Name"].Value.ToString())
					.ToList();

				if (!selectedModules.Any())
				{
					MessageBox.Show("Please select at least one module to process.", "No Selection",
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				foreach (var moduleName in selectedModules)
				{
					Logger.Log($"[INBOUND] Processing module: {moduleName}");

					switch (moduleName)
					{
						case "EMPLOYEE DETAILS":
							await inboundEmployee.RunEmployeeSyncAsync(session, _prismRepository);
							break;
						case "ITEM DETAILS":
							await inboundItem.RunItemSyncAsync(session, _prismRepository);
							break;
						case "HIERARCHY DETAILS":
							await inboundHierarchy.RunHierarchySyncAsync(session, _prismRepository);
							break;
						case "ASN DETAILS":
							await inboundAsn.RunASNSyncAsync(session, _prismRepository);
							break;
						case "PRICE DETAILS":
							await inboundPrice.RunPriceSyncAsync(session, _prismRepository);
							break;
					}
				}

				MessageBox.Show("Selected sync operations completed successfully!", "Success",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				Logger.Log($"[INBOUND] Error: {ex}");
				MessageBox.Show("An error occurred during synchronization. Check logs for details.",
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public async Task TriggerSFTPAsync()
		{
			await DownloadFromSftpAsync();
		}

		// ***************************************************
		// Methods
		// ***************************************************
		private async Task DownloadFromSftpAsync()
		{
			var sftpConfig = GlobalHelper.LoadSftpConnection();

			if (!sftpConfig.TryGetValue("Host", out string host) ||
				!sftpConfig.TryGetValue("Port", out string port) ||
				!sftpConfig.TryGetValue("Username", out string username) ||
				!sftpConfig.TryGetValue("Password", out string password))
			{
				MessageBox.Show("[ERROR] SFTP configuration is missing. Please navigate to the 'Configuration SFTP' tab to set up the SFTP connection.",
								"Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string inboundBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "INBOUND", "SENDING");
			string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "INBOUND", "inbound_db.db");

			var directoryMap = GlobalHelper.LoadSftpPathMap("InSFTPPath");

			await Task.Run(() =>
			{
				try
				{
					int portNumber = Convert.ToInt32(port);

					using (var sftp = new SftpClient(host, portNumber, username, password))
					{
						sftp.Connect();

						InitializeInboundDb(dbPath);

						foreach (var entry in directoryMap)
						{
							string key = entry.Key;
							string remotePath = entry.Value;

							try
							{
								if (!sftp.Exists(remotePath))
								{
									Logger.Log($"[INBOUND SFTP] Directory not found: {remotePath}");
									continue;
								}

								var files = sftp.ListDirectory(remotePath)
												.Where(f => !f.IsDirectory && !f.Name.StartsWith("."))
												.ToList();

								foreach (var file in files)
								{
									string fileName = file.Name;

									// Check if already exists in DB
									if (IsFileAlreadyDownloaded(dbPath, fileName))
									{
										Logger.Log($"[INBOUND SFTP] Skipping '{fileName}' — already exists in database.");
										continue;
									}

									string localFilePath = Path.Combine(inboundBaseDir, fileName);

									// Download file
									using (var fileStream = new FileStream(localFilePath, FileMode.Create))
									{
										sftp.DownloadFile(file.FullName, fileStream);
									}

									Logger.Log($"[INBOUND SFTP] Downloaded '{fileName}' from {remotePath}");

									// Insert new record into DB
									InsertDownloadedFile(dbPath, fileName, remotePath, inboundBaseDir);
								}
							}
							catch (Exception ex)
							{
								Logger.Log($"[INBOUND SFTP] Error processing directory '{remotePath}': {ex}");
							}
						}

						sftp.Disconnect();
					}

					Logger.Log("[INBOUND SFTP] File download completed.");
				}
				catch (Exception ex)
				{
					Logger.Log($"[INBOUND SFTP] Download failed: {ex}");
				}
			});
		}

		/// <summary>
		/// Creates the inbound_db.db database if it doesn't exist, with a temporary table.
		/// </summary>
		private void InitializeInboundDb(string dbPath)
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				string createTableSql = @"
            CREATE TABLE IF NOT EXISTS DownloadedFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL UNIQUE,
                RemotePath TEXT,
                LocalPath TEXT,
                DownloadDate TEXT DEFAULT CURRENT_TIMESTAMP
            );
        ";
				using (var command = new SQLiteCommand(createTableSql, connection))
				{
					command.ExecuteNonQuery();
				}
			}
		}

		/// <summary>
		/// Inserts a record of a downloaded file.
		/// </summary>
		private void InsertDownloadedFile(string dbPath, string fileName, string remotePath, string localPath)
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				string insertSql = "INSERT OR IGNORE INTO DownloadedFiles (FileName, RemotePath, LocalPath) VALUES (@FileName, @RemotePath, @LocalPath)";
				using (var command = new SQLiteCommand(insertSql, connection))
				{
					command.Parameters.AddWithValue("@FileName", fileName);
					command.Parameters.AddWithValue("@RemotePath", remotePath);
					command.Parameters.AddWithValue("@LocalPath", localPath);
					command.ExecuteNonQuery();
				}
			}
		}

		/// <summary>
		/// Checks if the file already exists in the DownloadedFiles table.
		/// </summary>
		private bool IsFileAlreadyDownloaded(string dbPath, string fileName)
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				string checkSql = "SELECT COUNT(1) FROM DownloadedFiles WHERE FileName = @FileName";
				using (var command = new SQLiteCommand(checkSql, connection))
				{
					command.Parameters.AddWithValue("@FileName", fileName);
					long count = (long)command.ExecuteScalar();
					return count > 0;
				}
			}
		}

		// ***************************************************
		// Handlers/Helpers
		// ***************************************************
		private void CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
		{
			GlobalHelper.HandleCellMouseMove(guna1DataGridView1, e);
		}

		private void CellMouseLeave(object sender, DataGridViewCellEventArgs e)
		{
			GlobalHelper.HandleCellMouseLeave(guna1DataGridView1);
		}
	}
}
