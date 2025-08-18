using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class OutboundPage : UserControl
	{
		private static GXConfig config;

		private InventoryRepository _inventoryRepository;
		private InTransitRepository _inTransitRepository;
		private PriceRepository _priceRepository;
		private StoreGoodsRepository _storeGoodsRepository;
		private StoreGoodsReturnRepository _storeGoodsReturnRepository;
		private StoreSaleRepository _storeSaleRepository;
		private StoreReturnRepository _storeReturnRepository;
		private StoreInventoryAdjustmentRepository _storeInventoryAdjustmentRepository;
		private StoreShippingRepository _storeShippingRepository;
		private StoreReceivingRepository _storeReceivingRepository;

		private TabControl tabControl;
		private TabPage tabEod, tabApi;

		public OutboundPage()
		{
			config = GXConfig.Load("config.xml");

			_inventoryRepository = new InventoryRepository(config.MainDbConnection);
			_inTransitRepository = new InTransitRepository(config.MainDbConnection);
			_priceRepository = new PriceRepository(config.MainDbConnection);
			_storeGoodsRepository = new StoreGoodsRepository(config.MainDbConnection);
			_storeGoodsReturnRepository = new StoreGoodsReturnRepository(config.MainDbConnection);
			_storeSaleRepository = new StoreSaleRepository(config.MainDbConnection);
			_storeReturnRepository = new StoreReturnRepository(config.MainDbConnection);
			_storeInventoryAdjustmentRepository = new StoreInventoryAdjustmentRepository(config.MainDbConnection);
			_storeShippingRepository = new StoreShippingRepository(config.MainDbConnection);
			_storeReceivingRepository = new StoreReceivingRepository(config.MainDbConnection);

			InitializeComponent();
			InitialCreateDatabase();
			InitializeTabs();
		}

		// ***************************************************
		// Initialization
		// ***************************************************
		private void InitialCreateDatabase()
		{
			// 🔧 Define and create AppData folder
			string folderPath = Path.Combine(Application.StartupPath, "AppData");
			Logger.Log("Checking for AppData folder...");

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
				Logger.Log($"Created AppData folder at: {folderPath}");
			}
			else
			{
				Logger.Log($"AppData folder already exists at: {folderPath}");
			}

			// 🗃️ Define DB path
			string dbPath = Path.Combine(folderPath, "ProcessedPrismTransactions.db");
			Logger.Log($"Database path: {dbPath}");

			// Create database file if needed
			if (!File.Exists(dbPath))
			{
				SQLiteConnection.CreateFile(dbPath);
				Logger.Log($"SQLite database created at: {dbPath}");
			}
			else
			{
				Logger.Log("Database file already exists.");
			}

			// 🔌 Connect and create tables
			string connectionString = $"Data Source={dbPath};Version=3;";
			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				conn.Open();
				Logger.Log("SQLite connection opened.");

				// 🧱 Create ProcessedPrismTransactions table
				string createTableQuery = @"
				CREATE TABLE IF NOT EXISTS ProcessedPrismTransactions (
					ID INTEGER PRIMARY KEY AUTOINCREMENT,
					SID TEXT NOT NULL,
					TYPE TEXT,
					DATE TEXT,
					STATUS TEXT NOT NULL
				);";

				using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
				{
					cmd.ExecuteNonQuery();
					Logger.Log("'ProcessedPrismTransactions' table created or already exists.");
				}

				// 🔍 Check if table is empty
				string countQuery = "SELECT COUNT(*) FROM ProcessedPrismTransactions;";
				using (SQLiteCommand countCmd = new SQLiteCommand(countQuery, conn))
				{
					long count = (long)countCmd.ExecuteScalar();
					Logger.Log($"Record count in 'ProcessedPrismTransactions': {count}");

					// 🌱 Insert seed data if empty
					if (count == 0)
					{
						Logger.Log("Inserting sample data...");

						string insertQuery = @"
							INSERT INTO ProcessedPrismTransactions (SID, TYPE, DATE, STATUS) VALUES
							('SID001', 'storesale', '25-JUN-24 11.41.26.000000000 PM +08:00', 'Success'),
							('SID002', 'storeshipping', '25-JUN-24 11.41.26.000000000 PM +08:00', 'Failed');
						";

						using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, conn))
						{
							int rowsInserted = insertCmd.ExecuteNonQuery();
							Logger.Log($"Inserted {rowsInserted} records into 'ProcessedPrismTransactions'.");
						}
					}
					else
					{
						Logger.Log("Table already contains data. Skipping sample insert.");
					}
				}

				Logger.Log("Database initialization complete.");
			}
		}

		public static async Task<bool> IsSidProcessedAsync(string sid)
		{
			string dbPath = Path.Combine(Application.StartupPath, "AppData", "ProcessedPrismTransactions.db");
			string connectionString = $"Data Source={dbPath};Version=3;";

			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				await conn.OpenAsync();

				string query = "SELECT COUNT(1) FROM ProcessedPrismTransactions WHERE SID = @Sid";
				using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@Sid", sid);
					var result = await cmd.ExecuteScalarAsync();

					int count = Convert.ToInt32(result);
					return count > 0;
				}
			}
		}


		private void InitializeTabs()
		{
			tabControl = new TabControl
			{
				Location = new Point(225, 20),
				Size = new Size(870, 450),
				Font = new Font("Segoe UI", 9)
			};

			tabEod = new TabPage("EOD");
			tabApi = new TabPage("API");

			var repositories = new OutboundRepositories(
				_inventoryRepository,
				_inTransitRepository,
				_priceRepository,
				_storeGoodsRepository,
				_storeGoodsReturnRepository,
				_storeSaleRepository,
				_storeReturnRepository,
				_storeInventoryAdjustmentRepository,
				_storeShippingRepository,
				_storeReceivingRepository);

			tabEod.Controls.Add(new OutboundEODTab(config, repositories) { Dock = DockStyle.Fill });
			tabApi.Controls.Add(new OutboundAPITab(config, repositories) { Dock = DockStyle.Fill });

			this.Controls.Add(tabControl);
			tabControl.TabPages.Add(tabEod);
			tabControl.TabPages.Add(tabApi);
		}
		
	}
	public class OutboundRepositories
	{
		public InventoryRepository InventoryRepository { get; set; }
		public InTransitRepository InTransitRepository { get; set; }
		public PriceRepository PriceRepository { get; set; }
		public StoreGoodsRepository StoreGoodsRepository { get; set; }
		public StoreGoodsReturnRepository StoreGoodsReturnRepository { get; set; }
		public StoreSaleRepository StoreSaleRepository { get; set; }
		public StoreReturnRepository StoreReturnRepository { get; set; }
		public StoreInventoryAdjustmentRepository StoreInventoryAdjustmentRepository { get; set; }
		public StoreShippingRepository StoreShippingRepository { get; set; }
		public StoreReceivingRepository StoreReceivingRepository { get; set; }

		public OutboundRepositories(
			InventoryRepository inventoryRepository,
			InTransitRepository inTransitRepository,
			PriceRepository priceRepository,
			StoreGoodsRepository storeGoodsRepository,
			StoreGoodsReturnRepository storeGoodsReturnRepository,
			StoreSaleRepository storeSaleRepository,
			StoreReturnRepository storeReturnRepository,
			StoreInventoryAdjustmentRepository storeInventoryAdjustmentRepository,
			StoreShippingRepository storeShippingRepository,
			StoreReceivingRepository storeReceivingRepository)
		{
			InventoryRepository = inventoryRepository;
			InTransitRepository = inTransitRepository;
			PriceRepository = priceRepository;
			StoreGoodsRepository = storeGoodsRepository;
			StoreGoodsReturnRepository = storeGoodsReturnRepository;
			StoreSaleRepository = storeSaleRepository;
			StoreReturnRepository = storeReturnRepository;
			StoreInventoryAdjustmentRepository = storeInventoryAdjustmentRepository;
			StoreShippingRepository = storeShippingRepository;
			StoreReceivingRepository = storeReceivingRepository;
		}
	}
}
