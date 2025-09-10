using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Views;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GXIntegration
{
	public partial class Form1 : Form
	{
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HTCAPTION = 0x2;

		static GXConfig config;
		private ConfigurationPage _configurationPage;
		//private InventoryRepository _inventoryRepository;
		//private InTransitRepository _inTransitRepository;
		//private PriceRepository _priceRepository;
		//private StoreGoodsRepository _storeGoodsRepository;
		//private StoreGoodsReturnRepository _storeGoodsReturnRepository;
		//private StoreSaleRepository _storeSaleRepository;
		//private StoreReturnRepository _storeReturnRepository;
		//private StoreInventoryAdjustmentRepository _storeInventoryAdjustmentRepository;
		//private StoreShippingRepository _storeShippingRepository;
		//private StoreReceivingRepository _storeReceivingRepository;

		bool sideBar_Expand = true;
		private Guna.UI.WinForms.GunaButton _activeButton = null;

		//public OutboundEODTab OutboundTab { get; private set; }

		public Form1()
		{
			InitializeComponent();
			InitialCreateDatabase();
			EnableDrag(SideBar);

			string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			MainContentPanel.Dock = DockStyle.Fill;

			//_inventoryRepository = new InventoryRepository(config.MainDbConnection);
			//_inTransitRepository = new InTransitRepository(config.MainDbConnection);
			//_priceRepository = new PriceRepository(config.MainDbConnection);
			//_storeGoodsRepository = new StoreGoodsRepository(config.MainDbConnection);
			//_storeGoodsReturnRepository = new StoreGoodsReturnRepository(config.MainDbConnection);
			//_storeSaleRepository = new StoreSaleRepository(config.MainDbConnection);
			//_storeReturnRepository = new StoreReturnRepository(config.MainDbConnection);
			//_storeInventoryAdjustmentRepository = new StoreInventoryAdjustmentRepository(config.MainDbConnection);
			//_storeShippingRepository = new StoreShippingRepository(config.MainDbConnection);
			//_storeReceivingRepository = new StoreReceivingRepository(config.MainDbConnection);

			//var repositories = new OutboundRepositories(
			//_inventoryRepository,
			//_inTransitRepository,
			//_priceRepository,
			//_storeGoodsRepository,
			//_storeGoodsReturnRepository,
			//_storeSaleRepository,
			//_storeReturnRepository,
			//_storeInventoryAdjustmentRepository,
			//_storeShippingRepository,
			//_storeReceivingRepository);


			////Logger.Log(">>> Start FORM Process...");
			//OutboundTab = new OutboundEODTab(config, repositories);
			//this.Controls.Add(OutboundTab);
		}

		private void EnableDrag(Control control)
		{
			control.MouseDown += (s, e) =>
			{
				if (e.Button == MouseButtons.Left)
				{
					ReleaseCapture();
					SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
				}
			};
		}

		private void Form1_MouseDown(object sender, MouseEventArgs e)
		{
			// Trigger dragging
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			this.WindowState = FormWindowState.Normal;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.ShowInTaskbar = true;
			this.Show();
			this.BringToFront();

			SetActiveSidebarButton(Home_Button);
			LoadPage(new HomePage());
			EnableDrag(this);
		}

		private void gunaPanel1_Paint(object sender, PaintEventArgs e)
		{

		}

		private void Close_Button_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void Timer_Sidebar_Menu_Tick(object sender, EventArgs e)
		{
			if (sideBar_Expand)
			{
				SideBar.Width -= 10;
				if (SideBar.Width == SideBar.MinimumSize.Width)
				{
					sideBar_Expand = false;
					Timer_Sidebar_Menu.Stop();
				}
			}
			else
			{
				SideBar.Width += 10;
				if (SideBar.Width == SideBar.MaximumSize.Width)
				{
					sideBar_Expand = true;
					Timer_Sidebar_Menu.Stop();
				}
			}
		}

		private void Menu_Button_Click(object sender, EventArgs e)
		{
			Timer_Sidebar_Menu.Start();
		}

		private void gunaImageButton1_Click(object sender, EventArgs e)
		{

		}

		private void Link_Github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{

		}

		// *********************************************************
		// Sidebar Buttons
		// *********************************************************
		private void Home_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
			LoadPage(new HomePage());
		}

		private void Configuration_Button_Click(object sender, EventArgs e)
		{
			if (_configurationPage == null)
				_configurationPage = new ConfigurationPage();

			LoadPage(_configurationPage);
			SetActiveSidebarButton(Configuration_Button);
		}
		private void Inbound_Button_Click(object sender, EventArgs e)
		{
			LoadPage(new InboundPage());
			SetActiveSidebarButton(Inbound_Button);
		}

		private void Outbound_Button_Click(object sender, EventArgs e)
		{
			LoadPage(new OutboundPage());
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
		}
		private void About_Button_Click(object sender, EventArgs e)
		{
			LoadPage(new AboutPage());
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
		}

		// *********************************************************
		// Helpers
		// *********************************************************
		private void SetActiveSidebarButton(Guna.UI.WinForms.GunaButton button)
		{
			// Reset previous active button
			if (_activeButton != null)
			{
				_activeButton.BaseColor = Color.Transparent;
				_activeButton.ForeColor = Color.White;
				_activeButton.OnHoverBaseColor = Color.FromArgb(40, 40, 100);
				_activeButton.OnHoverForeColor = Color.White;
			}

			// Set new active button
			_activeButton = button;
			_activeButton.BaseColor = Color.FromArgb(60, 60, 120); // Static active color
			_activeButton.ForeColor = Color.White;
			_activeButton.OnHoverBaseColor = _activeButton.BaseColor; // Lock hover color
			_activeButton.OnHoverForeColor = _activeButton.ForeColor;
		}

		private void LoadPage(UserControl page)
		{
			MainContentPanel.Controls.Clear();
			page.Dock = DockStyle.Fill;
			MainContentPanel.Controls.Add(page);
		}

		private void MainContentPanel_Paint(object sender, PaintEventArgs e)
		{

		}


		// ***************************************************
		// Initialization
		// ***************************************************
		private void InitialCreateDatabase()
		{
			string folderPath = Path.Combine(Application.StartupPath, "AppData");
			//Logger.Log("[INIT] Checking for AppData folder...");

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
				Logger.Log($"[INIT] Created AppData folder at: {folderPath}");
			}

			string dbPath = Path.Combine(folderPath, "TRANSACTION_PROCESS.db");
			if (!File.Exists(dbPath))
			{
				SQLiteConnection.CreateFile(dbPath);
			}

			string connectionString = $"Data Source={dbPath};Version=3;";
			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				conn.Open();

				string createTableQuery = @"
				CREATE TABLE IF NOT EXISTS TRANSACTION_PROCESS (
					ID					INTEGER PRIMARY KEY AUTOINCREMENT
					, SID				TEXT	NOT NULL
					, PROCCESS_TYPE		INT
					, TRANSACTION_TYPE	TEXT
					, PROCCESS_DATE		TEXT
					, POST_DATE			TEXT
					, STATUS			TEXT	NOT NULL
				);";
				// !NOTE : PROCESS_TYPE: [1] INBOUND, [2] OUTBOUND

				using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
				{
					cmd.ExecuteNonQuery();
					Logger.Log("[INIT] 'TRANSACTION_PROCESS' table created or already exists.");
				}

				string countQuery = "SELECT COUNT(*) FROM TRANSACTION_PROCESS;";
				using (SQLiteCommand countCmd = new SQLiteCommand(countQuery, conn))
				{
					long count = (long)countCmd.ExecuteScalar();
					Logger.Log($"[INIT] DB Record count: {count}");
				}

				Logger.Log("[INIT] Database initialization complete.");
			}
		}

	}
}
