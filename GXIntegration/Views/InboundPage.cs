using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.InboundHandlers;
using GXIntegration_Levis.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GXIntegration_Levis.Helpers.GlobalHelper;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace GXIntegration_Levis.Views
{
	public partial class InboundPage : UserControl
	{
		static GXConfig config;
		private InventoryRepository _inventoryRepository;
		private GunaDataGridView guna1DataGridView1;
		private GunaButton btnSaveToPrism;

		private InboundHierarchyRepository _inboundHierarchyRepository;


		//private readonly InboundEmployee inboundEmployee = new InboundEmployee();
		private readonly InboundItem inboundItem = new InboundItem();
		private readonly InboundHierarchy inboundHierarchy = new InboundHierarchy();
		
		public InboundPage()
		{
			config = GXConfig.Load("config.xml");
			_inventoryRepository = new InventoryRepository(config.MainDbConnection);

			_inboundHierarchyRepository = new InboundHierarchyRepository(config.MainDbConnection);


			InitializeComponent();
			InitializeGrid();
			InitializeControls();
		}

		// ***************************************************
		// Initialization Methods
		// ***************************************************
		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(250, 50),
				Size = new Size(820, 180),
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

			guna1DataGridView1.ColumnCount = 5;
			guna1DataGridView1.Columns[0].Name = "ID";
			guna1DataGridView1.Columns[1].Name = "Name";
			guna1DataGridView1.Columns[2].Name = "File Name Format";
			guna1DataGridView1.Columns[3].Name = "File Type";
			guna1DataGridView1.Columns[4].Name = "Delimiter";

			guna1DataGridView1.Columns[0].Width = 30;
			guna1DataGridView1.Columns[1].Width = 200;
			guna1DataGridView1.Columns[2].Width = 300;
			guna1DataGridView1.Columns[3].Width = 85;
			guna1DataGridView1.Columns[4].Width = 85;

			var imageColumn = new DataGridViewImageColumn
			{
				Name = "Action",
				HeaderText = "Action",
				Image = Resources.icon_download,
				Width = 50,
				ImageLayout = DataGridViewImageCellLayout.Zoom
			};
			guna1DataGridView1.Columns.Add(imageColumn);

			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			void AddRow(string id, string name, string format, string type, string delimiter)
				=> guna1DataGridView1.Rows.Add(id, name, format, type, delimiter);

			AddRow("1", "EMPLOYEE DETAILS", "LSPI_WD_[yyyymmddhhmmss]", ".csv", "comma ( , )");
			AddRow("2", "ITEM DETAILS", "LSPI_ITEM_[yyyymmddhhmmss]", ".txt", "caret ( ^ )");
			AddRow("3", "HIERARCHY DETAILS", "LSPI_HIERARCHY_[yyyymmddhhmmss]", ".txt", "caret ( ^ )");
			AddRow("4", "ADVANCE SHIPPING NOTICE (ASN) DETAILS", "LSPI_PRTRDX_[yyyymmddhhmmss]", ".txt", "{^^}");
			AddRow("5", "PRICE DETAILS", "LSPI_PRTAR_[yyyymmddhhmmss]",".txt", "{^^}");

			this.Controls.Add(guna1DataGridView1);
		}

		private void InitializeControls()
		{
			btnSaveToPrism = GlobalHelper.CreateButton(
				text: "Save Data to Prism",
				location: new Point(250, 250),

				clickAction: async () =>
				{
					try
					{
						//await inboundEmployee.RunEmployeeSyncAsync();
						//await inboundItem.RunItemSyncAsync();
						await inboundHierarchy.RunHierarchySyncAsync(_inboundHierarchyRepository);
						
						MessageBox.Show("All sync operations completed successfully!");
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Error: {ex.Message}");
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
