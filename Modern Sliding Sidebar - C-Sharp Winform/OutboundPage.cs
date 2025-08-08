using Guna.UI.WinForms;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Model;
using GXIntegration_Levis.OutboundHandlers;
using GXIntegration_Levis.Properties;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis
{
	public partial class OutboundPage : UserControl
	{
		private static GXConfig config;

		private InventoryRepository _inventoryRepository;
		private InTransitRepository _inTransitRepository;
		private PriceRepository _priceRepository;

		private SalesRepository _salesRepository;
		
		private GunaDataGridView guna1DataGridView1;
		private int _hoveredRowIndex = -1;

		private Dictionary<string, Func<Task>> downloadActions;

		public OutboundPage()
		{
			config = GXConfig.Load("config.xml");
			_inventoryRepository = new InventoryRepository(config.MainDbConnection);
			_inTransitRepository = new InTransitRepository(config.MainDbConnection);
			_priceRepository = new PriceRepository(config.MainDbConnection);

			_salesRepository = new SalesRepository(config.MainDbConnection);

			InitializeComponent();
			InitializeTable();
			InitializeDownloadActions();
		}

		private void InitializeTable()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(220, 70),
				Size = new Size(900, 300),
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
			guna1DataGridView1.CellMouseMove += Guna1DataGridView1_CellMouseMove;
			guna1DataGridView1.CellMouseLeave += Guna1DataGridView1_CellMouseLeave;

			// Columns
			guna1DataGridView1.ColumnCount = 4;
			guna1DataGridView1.Columns[0].Name = "ID";
			guna1DataGridView1.Columns[1].Name = "Name";
			guna1DataGridView1.Columns[2].Name = "File Name Format";
			guna1DataGridView1.Columns[3].Name = "File Type";

			guna1DataGridView1.Columns[0].Width = 30;
			guna1DataGridView1.Columns[1].Width = 200;
			guna1DataGridView1.Columns[2].Width = 455;
			guna1DataGridView1.Columns[3].Width = 70;

			// Add action button column
			var imageColumn = new DataGridViewImageColumn
			{
				Name = "Action",
				HeaderText = "Action",
				Image = Resources.icon_download,
				Width = 50,
				ImageLayout = DataGridViewImageCellLayout.Zoom
			};
			guna1DataGridView1.Columns.Add(imageColumn);

			guna1DataGridView1.CellMouseMove += (s, e) =>
			{
				if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && guna1DataGridView1.Columns[e.ColumnIndex].Name == "Action")
					guna1DataGridView1.Cursor = Cursors.Hand;
				else
					guna1DataGridView1.Cursor = Cursors.Default;
			};

			// Rows
			AddRow("1", "ASN - RECEIVING", "StoreGoods_[yyyymmddhhmmss]", ".xml");
			AddRow("2", "RETURN_TO_DC", "StoreGoodsReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("3", "RETAIL_SALE", "StoreSale_[yyyymmddhhmmss]", ".xml");
			AddRow("4", "RETURN_SALE", "StoreReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("5", "ADJUSTMENT", "StoreInventoryAdjustment_[yyyymmddhhmmss]", ".xml");
			AddRow("6", "STORE_TRANSFER - SHIPPING ", "StoreShipping_[yyyymmddhhmmss]", ".xml");
			AddRow("7", "STORE_TRANSFER - RECEIVING", "StoreReceiving_[yyyymmddhhmmss]", ".xml");
			AddRow("8", "INVENTORY SNAPSHOTS", "LS[Country code]_AMA_PSSTKR_[yyyymmddhhmmss]", ".txt");
			AddRow("9", "INTRANSIT", "LS[Country Code]_[REGION Code]_INTRANSIT_[yyyymmddhhmmss]", ".txt");
			AddRow("10", "PRICE", "[REGION Code]_[Country code]_PRICING_[yyyymmddhhmmss]", ".txt");

			this.Controls.Add(guna1DataGridView1);

			guna1DataGridView1.CellContentClick += Guna1DataGridView1_CellContentClick;
		}

		private void AddRow(string id, string name, string format, string type)
		{
			guna1DataGridView1.Rows.Add(id, name, format, type);
		}

		private void InitializeDownloadActions()
		{
			downloadActions = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
			{
				["ASN - RECEIVING"] = () => OutboundASN.Execute(_inventoryRepository, config),
				["RETURN_TO_DC"] = () => OutboundReturnToDC.Execute(_inventoryRepository, config),
				["RETAIL_SALE"] = () => OutboundRetailSale.Execute(_salesRepository, config),
				["RETURN_SALE"] = () => OutboundReturnSale.Execute(_inventoryRepository, config),
				["ADJUSTMENT"] = () => OutboundAdjustment.Execute(_inventoryRepository, config),
				["STORE_TRANSFER - SHIPPING "] = () => OutboundStoreShipping.Execute(_inventoryRepository, config),
				["STORE_TRANSFER - RECEIVING"] = () => OutboundStoreReceiving.Execute(_inventoryRepository, config),
				["INVENTORY SNAPSHOTS"] = () => OutboundInventorySnapshots.Execute(_inventoryRepository, config),
				["INTRANSIT"] = () => OutboundInTransit.Execute(_inTransitRepository, config),
				["PRICE"] = () => OutboundPrice.Execute(_priceRepository, config)
			};
		}

		private async void Guna1DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || guna1DataGridView1.Columns[e.ColumnIndex].Name != "Action")
				return;

			string name = guna1DataGridView1.Rows[e.RowIndex].Cells["Name"].Value?.ToString();

			if (downloadActions.TryGetValue(name, out var handler))
			{
				await handler();
			}
			else
			{
				MessageBox.Show($"No action defined for: {name}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}


		private void Guna1DataGridView1_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex >= 0 && e.RowIndex != _hoveredRowIndex)
			{
				if (_hoveredRowIndex >= 0)
				{
					guna1DataGridView1.Rows[_hoveredRowIndex].DefaultCellStyle.BackColor = Color.White;
				}

				guna1DataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
				_hoveredRowIndex = e.RowIndex;

				if (guna1DataGridView1.Columns[e.ColumnIndex].Name == "Action")
					guna1DataGridView1.Cursor = Cursors.Hand;
				else
					guna1DataGridView1.Cursor = Cursors.Default;
			}
		}

		private void Guna1DataGridView1_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
		{
			if (_hoveredRowIndex >= 0)
			{
				guna1DataGridView1.Rows[_hoveredRowIndex].DefaultCellStyle.BackColor = Color.White;
				_hoveredRowIndex = -1;
			}
			guna1DataGridView1.Cursor = Cursors.Default;
		}
	}
}
