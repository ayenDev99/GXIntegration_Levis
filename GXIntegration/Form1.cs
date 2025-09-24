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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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
		bool sideBar_Expand = true;
		private Guna.UI.WinForms.GunaButton _activeButton = null;

		private Panel topBar;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Button minimizeButton;

		//public OutboundEODTab OutboundTab { get; private set; }

		public Form1()
		{
			InitializeComponent();
			//InitialCreateDatabase();
			InitialInboundPriceDatabase();
			EnableDrag(SideBar);
			InitializeTopBar();

			string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			MainContentPanel.Dock = DockStyle.Fill;
		}

		private void InitializeTopBar()
		{
			topBar = new Panel();
			topBar.Height = 30;
			topBar.Dock = DockStyle.Top;
			topBar.BackColor = Color.FromArgb(35, 40, 45);
			this.Controls.Add(topBar);

			// Enable drag (so the user can still move the form by dragging top bar)
			EnableDrag(topBar);

			InitializeCustomButtons();
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

		private void InitializeCustomButtons()
		{
			// Close button
			closeButton = new System.Windows.Forms.Button();
			closeButton.Text = "x";
			closeButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
			closeButton.ForeColor = Color.White;
			closeButton.BackColor = Color.Transparent;
			closeButton.FlatStyle = FlatStyle.Flat;
			closeButton.FlatAppearance.BorderSize = 0;
			closeButton.Size = new Size(40, 40);
			closeButton.Location = new Point(this.Width - 40, -5);
			closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			closeButton.Click += (s, e) => this.Close();

			// Hover effects
			closeButton.MouseEnter += (s, e) =>
			{
				closeButton.BackColor = Color.FromArgb(60, 60, 60);
				closeButton.ForeColor = Color.White;
			};
			closeButton.MouseLeave += (s, e) =>
			{
				closeButton.BackColor = Color.Transparent;
				closeButton.ForeColor = Color.White;
			};

			// Minimize button
			minimizeButton = new System.Windows.Forms.Button();
			minimizeButton.Text = "–"; // en dash looks like a minus
			minimizeButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
			minimizeButton.ForeColor = Color.White;
			minimizeButton.BackColor = Color.Transparent;
			minimizeButton.FlatStyle = FlatStyle.Flat;
			minimizeButton.FlatAppearance.BorderSize = 0;
			minimizeButton.Size = new Size(40, 40);
			minimizeButton.Location = new Point(this.Width - 75, -5);
			minimizeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			minimizeButton.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

			// Hover effects
			minimizeButton.MouseEnter += (s, e) =>
			{
				minimizeButton.BackColor = Color.FromArgb(60, 60, 60);
				minimizeButton.ForeColor = Color.White;
			};
			minimizeButton.MouseLeave += (s, e) =>
			{
				minimizeButton.BackColor = Color.Transparent;
				minimizeButton.ForeColor = Color.White;
			};

			topBar.Controls.Add(closeButton);
			topBar.Controls.Add(minimizeButton);
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

		private void InitialInboundPriceDatabase()
		{
			string folderPath = Path.Combine(Application.StartupPath, "AppData");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
				Logger.Log($"[INIT] Created AppData folder at: {folderPath}");
			}

			string dbPath = Path.Combine(folderPath, "TempInboundPriceData.db");
			if (!File.Exists(dbPath))
			{
				SQLiteConnection.CreateFile(dbPath);
			}

			string connectionString = $"Data Source={dbPath};Version=3;";
			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				conn.Open();

				string createTableQuery = @"
				CREATE TABLE IF NOT EXISTS TempInboundPriceData (
					Id                      INTEGER PRIMARY KEY AUTOINCREMENT
					, CreatedDate				TEXT
					, CountryCode				TEXT
					, StoreCode					TEXT
					, ProductCode				TEXT
					, ColorCode					TEXT
					, SizeCode					TEXT
					, SKU						TEXT
					, PriceType					TEXT
					, Currency					TEXT
					, Price						REAL
					, EffectivityDate			TEXT
					, ProductReference			TEXT
					, Brand						TEXT
					, PriceListCode				TEXT
					, SerialNumber				TEXT
					, PriceSource				TEXT
					, Price2					REAL
					, EffectivePriceEndDate		TEXT
					, DiscountCode				TEXT
					, DiscountDesc				TEXT
					, ReasonCode				TEXT
					, ReasonDesc				TEXT
					, Level1Code				TEXT
					, DeletedDate				TEXT
				);";

				using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
				{
					cmd.ExecuteNonQuery();
					Logger.Log("[INIT] 'TempInboundPriceData' table created or already exists.");
				}

				string countQuery = "SELECT COUNT(*) FROM TempInboundPriceData;";
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
