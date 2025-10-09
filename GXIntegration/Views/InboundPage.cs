using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.InboundHandlers;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class InboundPage : UserControl
	{
		static GXConfig config;
		private GunaDataGridView guna1DataGridView1;
		private GunaButton btnSaveToPrism;

		private PrismRepository _prismRepository;

		private readonly InboundEmployee inboundEmployee = new InboundEmployee();
		private readonly InboundItem inboundItem = new InboundItem();
		private readonly InboundHierarchy inboundHierarchy = new InboundHierarchy();
		private readonly InboundASN inboundAsn = new InboundASN();
		private readonly InboundPrice inboundPrice = new InboundPrice();

		public InboundPage()
		{
			string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			_prismRepository = new PrismRepository(config.MainDbConnection);

			InitializeComponent();
			InitializeGrid();
			ProcessedInboundFilesDatabase();
			InitializeControls();
		}

		// ***************************************************
		// Initialization
		// ***************************************************
		private void ProcessedInboundFilesDatabase()
		{
			string folderPath = Path.Combine(Application.StartupPath, "AppData");
			Directory.CreateDirectory(folderPath);

			string dbPath = Path.Combine(folderPath, "TempProcessedInboundFiles.db");

			if (!File.Exists(dbPath))
				SQLiteConnection.CreateFile(dbPath);

			string connectionString = $"Data Source={dbPath};Version=3;";
			string createTableQuery = @"
				CREATE TABLE IF NOT EXISTS TempProcessedInboundFiles (
					Id              INTEGER PRIMARY KEY AUTOINCREMENT,
					CreatedDate     TEXT,
					ModuleType      TEXT,
					FileName        TEXT,
					Status          TEXT,
					DeletedDate     TEXT
				);
			";

			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				conn.Open();
				using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
				{
					cmd.ExecuteNonQuery();
				}
			}

			Logger.Log($"[INBOUND] 'TempProcessedInboundFiles' table created or already exists. Path : {folderPath}");
		}

		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(250, 50),
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
				new DataGridViewTextBoxColumn { Name = "Name", Width = 140 },
				new DataGridViewTextBoxColumn { Name = "File Name Format", Width = 250 },
				new DataGridViewTextBoxColumn { Name = "File", Width = 45 },
				new DataGridViewTextBoxColumn { Name = "Delimiter", Width = 65 }
			);

			void AddRow(string id, string name, string format, string type, string delimiter)
				=> guna1DataGridView1.Rows.Add(false, id, name, format, type, delimiter);

			AddRow("1", "EMPLOYEE DETAILS", "LSPI_WD_[yyyymmddhhmmss]", ".csv", "( , )");
			AddRow("2", "ITEM DETAILS", "LSPI_ITEM_[yyyymmddhhmmss]", ".txt", "( ^ )");
			AddRow("3", "HIERARCHY DETAILS", "LSPI_HIERARCHY_[yyyymmddhhmmss]", ".txt", "( ^ )");
			AddRow("4", "ASN DETAILS", "LSPI_PRTRDX_[yyyymmddhhmmss]", ".txt", "{^^}");
			AddRow("5", "PRICE DETAILS", "LSPI_PRTAR_[yyyymmddhhmmss]", ".txt", "{^^}");

			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			this.Controls.Add(guna1DataGridView1);

			this.Load += (s, e) => PositionSelectAll();
		}

		private void InitializeControls()
		{
			btnSaveToPrism = GlobalHelper.CreateButton(
				text: "Save Data to Prism",
				location: new Point(250, 270),
				clickAction: async () =>
				{
					try
					{
						var globalInbound = new GlobalInbound();

						string session = await globalInbound.AuthenticateFromConfigAsync();
						if (session == null)
							return;

						string inboundDir = globalInbound.EnsureInboundDirectory();

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
									await inboundEmployee.RunEmployeeSyncAsync(session, inboundDir, _prismRepository);
									break;
								case "ITEM DETAILS":
									await inboundItem.RunItemSyncAsync(session, inboundDir, _prismRepository);
									break;
								case "HIERARCHY DETAILS":
									await inboundHierarchy.RunHierarchySyncAsync(session, inboundDir, _prismRepository);
									break;
								case "ASN DETAILS":
									await inboundAsn.RunASNSyncAsync(session, inboundDir, _prismRepository);
									break;
								case "PRICE DETAILS":
									await inboundPrice.RunPriceSyncAsync(session, inboundDir, _prismRepository);
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
			);

			this.Controls.Add(btnSaveToPrism);
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
