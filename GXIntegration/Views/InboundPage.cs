using Guna.UI2.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.InboundHandlers;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace GXIntegration_Levis.Views
{
	public partial class InboundPage : UserControl
	{
		static GXConfig config;
		private Guna2DataGridView guna1DataGridView1;

		private DateTimePicker datePickerFrom;
		private DateTimePicker datePickerTo;
		private Label lblFrom;
		private Label lblTo;
		private Guna2Button btnSaveToPrism;

		private PrismRepository _prismRepository;

		private readonly InboundEmployee inboundEmployee = new InboundEmployee();
		private readonly InboundItem inboundItem = new InboundItem();
		private readonly InboundHierarchy inboundHierarchy = new InboundHierarchy();
		private readonly InboundASN inboundAsn = new InboundASN();
		private readonly InboundPrice inboundPrice = new InboundPrice();

		private string configPath;
		public InboundPage()
		{
            InitializeComponent();

            GlobalInbound.Initialize();
			configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			_prismRepository = new PrismRepository(config.MainDbConnection);

			InitializeGrid();
			InitializeControls();
		}

		// ***************************************************
		// Initialization Methods
		// ***************************************************
		private void InitializeGrid()
		{
			guna1DataGridView1 = new Guna2DataGridView
			{
				Location = new Point(250, 90),
				Size = new Size(620, 180),
				AllowUserToAddRows = false,
				ScrollBars = ScrollBars.Both,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				GridColor = Color.LightGray,
				//Theme = Guna2DataGridViewPresetThemes.Guna
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

		// ***************************************************
		// Process Methods
		// ***************************************************
		public async Task TriggerSFTPAsync(int reprocessTime, string session)
		{
			await DownloadFromSftpAsync();
			await DownloadFromLocalAsync();

			await AutoProcessAsync(reprocessTime);
		}

		public async Task ManualProcessAsync()
		{
			ProgressForm progressForm = new ProgressForm();
			progressForm.Show();

			var isAuto = false;

			BackgroundWorker worker = new BackgroundWorker
			{
				WorkerReportsProgress = true,
				WorkerSupportsCancellation = false
			};

			worker.DoWork += (s, e) =>
			{
				try
				{
					var globalInbound = new GlobalInbound();

					// Block async so BackgroundWorker waits
					string session = globalInbound.AuthenticateFromConfigAsync()
												 .GetAwaiter()
												 .GetResult();

					if (session == null)
						return;

					var selectedModules = guna1DataGridView1.Rows
						.Cast<DataGridViewRow>()
						.Where(r => Convert.ToBoolean(r.Cells["Select"].Value) == true)
						.Select(r => r.Cells["Name"].Value.ToString())
						.ToList();

					if (!selectedModules.Any())
					{
						Logger.LogInbound("[INBOUND] No module selected.", isAuto);
						return;
					}

					int totalSteps = selectedModules.Count;
					int currentStep = 0;

					foreach (var moduleName in selectedModules)
					{
						currentStep++;

						//Logger.LogInbound($"[INBOUND] Processing module: {moduleName}", isAuto);

						worker.ReportProgress(currentStep * 100 / totalSteps, moduleName);

						switch (moduleName)
						{
							case "EMPLOYEE DETAILS":
								inboundEmployee.RunEmployeeSyncAsync(session, _prismRepository, isAuto)
									.GetAwaiter().GetResult();
								break;

							case "ITEM DETAILS":
								inboundItem.RunItemSyncAsync(session, _prismRepository, isAuto)
									.GetAwaiter().GetResult();
								break;

							case "HIERARCHY DETAILS":
								inboundHierarchy.RunHierarchySyncAsync(session, _prismRepository, isAuto)
									.GetAwaiter().GetResult();
								break;

							case "ASN DETAILS":
								inboundAsn.RunASNSyncAsync(session, _prismRepository, isAuto)
									.GetAwaiter().GetResult();
								break;

							case "PRICE DETAILS":
								inboundPrice.RunPriceSyncAsync(session, _prismRepository, isAuto)
									.GetAwaiter().GetResult();
								break;
						}
					}

					//Logger.LogInbound("[INBOUND-MANUAL] Process Completed!", isAuto);
				}
				catch (Exception ex)
				{
					Logger.LogError($"[INBOUND] Error: {ex}", isAuto);
				}
			};

			worker.ProgressChanged += (s, e) =>
			{
				progressForm.UpdateProgress(e.ProgressPercentage, 100);
			};

			worker.RunWorkerCompleted += (s, e) =>
			{
				//progressForm.AppendLog("Process Completed!");
				progressForm.AppendLog("Process Completed!");
				progressForm.EnableClose();

				// complete progress bar
				progressForm.UpdateProgress(100, 100);
			};

			worker.RunWorkerAsync();
		}

		public async Task AutoProcessAsync(int reprocessTime)
		{
			var isAuto = true;

			try
			{
				var globalInbound = new GlobalInbound();

				var (fromDate, toDate) = GlobalHelper.GetSystemTimeRange(reprocessTime);
				Logger.LogInbound($"-----------------------------------", isAuto);
				Logger.LogInbound($"[AUTO - SFTP] Process Time : {fromDate}", isAuto);

				// Block async so BackgroundWorker waits
				string session = globalInbound.AuthenticateFromConfigAsync()
												.GetAwaiter()
												.GetResult();


				if (session == null)
					return;

				// Auto process all modules
				var allModules = new List<string>
				{
					"EMPLOYEE DETAILS",
					"ITEM DETAILS",
					"HIERARCHY DETAILS",
					"ASN DETAILS",
					"PRICE DETAILS"
				};

				foreach (var moduleName in allModules)
				{

					// Logger.LogInbound($"[INBOUND] Processing module: {moduleName}", true);

					switch (moduleName)
					{
						case "EMPLOYEE DETAILS":
							inboundEmployee.RunEmployeeSyncAsync(session, _prismRepository, isAuto)
								.GetAwaiter().GetResult();
							break;

						case "ITEM DETAILS":
							inboundItem.RunItemSyncAsync(session, _prismRepository, isAuto)
								.GetAwaiter().GetResult();
							break;

						case "HIERARCHY DETAILS":
							inboundHierarchy.RunHierarchySyncAsync(session, _prismRepository, isAuto)
								.GetAwaiter().GetResult();
							break;

						case "ASN DETAILS":
							inboundAsn.RunASNSyncAsync(session, _prismRepository, isAuto)
								.GetAwaiter().GetResult();
							break;

						case "PRICE DETAILS":
							inboundPrice.RunPriceSyncAsync(session, _prismRepository, isAuto)
								.GetAwaiter().GetResult();
							break;
					}
				}

				// Logger.LogInbound("[INBOUND-AUTO] Process Completed!", true);
			}
			catch (Exception ex)
			{
				Logger.LogError($"[INBOUND] Error: {ex}", true);
			}
		}

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

			var directoryMap = GlobalHelper.LoadPathMap("InSFTPPath");

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
									Logger.LogInbound($"[INBOUND SFTP] Directory not found: {remotePath}", true);
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
										// Logger.LogInbound($"[INBOUND SFTP] Skipping '{fileName}' — already exists in database.");
										continue;
									}

									string localFilePath = Path.Combine(inboundBaseDir, fileName);

									// Download file
									using (var fileStream = new FileStream(localFilePath, FileMode.Create))
									{
										sftp.DownloadFile(file.FullName, fileStream);
									}

									Logger.LogInbound($"[INBOUND SFTP] Downloaded '{fileName}' from {remotePath}", true);

									// Insert new record into DB
									InsertDownloadedFile(dbPath, fileName, remotePath, inboundBaseDir);
								}
							}
							catch (Exception ex)
							{
								Logger.LogError($"[INBOUND SFTP] Error processing directory '{remotePath}': {ex}", true);
							}
						}

						sftp.Disconnect();
					}

					//Logger.LogInbound("[INBOUND SFTP] File download completed.", true);
				}
				catch (Exception ex)
				{
					Logger.LogError($"[INBOUND SFTP] Download failed: {ex}", true);
				}
			});
		}

		private async Task DownloadFromLocalAsync()
		{
			string inboundBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "INBOUND", "SENDING");
			string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "INBOUND", "inbound_db.db");

			// Read <InLocalPath> from config
			var directoryMap = GlobalHelper.LoadPathMap("InLocalPath");

			await Task.Run(() =>
			{
				try
				{
					InitializeInboundDb(dbPath);

					foreach (var entry in directoryMap)
					{
						string key = entry.Key;
						string remotePath = entry.Value;

						try
						{
							if (!Directory.Exists(remotePath))
							{
								Logger.LogInbound($"[AUTO - LOCAL] Directory not found: {remotePath}", true);
								continue;
							}

							var files = Directory.GetFiles(remotePath)
												 .Where(f => File.Exists(f))
												 .ToList();

							foreach (var filePath in files)
							{

								string fileName = Path.GetFileName(filePath);
								

								// Skip if already processed
								if (IsFileAlreadyDownloaded(dbPath, fileName))
								{
									Logger.LogInbound($"[AUTO - LOCAL] File {fileName} already downloaded. Moved to DUPLICATE folder.", true);
									var duplicatePath = Path.Combine(remotePath, "DUPLICATE");
									string dupliFilePath = Path.Combine(duplicatePath, fileName);

									if (!Directory.Exists(duplicatePath))
										Directory.CreateDirectory(duplicatePath);

									File.Copy(filePath, dupliFilePath, overwrite: true);
									File.Delete(filePath);
									continue;
								}

								string localFilePath = Path.Combine(inboundBaseDir, fileName);

								// Copy to INBOUND/SENDING
								File.Copy(filePath, localFilePath, overwrite: true);

								Logger.LogInbound($"[AUTO - LOCAL] Moved '{fileName}' from {remotePath}", true);

								// Add to DB
								InsertDownloadedFile(dbPath, fileName, remotePath, inboundBaseDir);
								File.Delete(filePath);
							}
						}
						catch (Exception ex)
						{
							Logger.LogError($"[AUTO - LOCAL] Error processing directory '{remotePath}': {ex}", true);
						}
					}

					//Logger.LogInbound("[INBOUND LOCAL] File copy completed.", true);
				}
				catch (Exception ex)
				{
					Logger.LogError($"[AUTO - LOCAL] Local download failed: {ex}", true);
				}
			});
		}

		// ***************************************************
		// Handlers/Helpers
		// ***************************************************
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
