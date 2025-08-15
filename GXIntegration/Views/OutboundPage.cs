using GXIntegration_Levis.Data.Access;
using GXIntegration.Properties;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GXIntegration_Levis.Helpers;

namespace GXIntegration_Levis.Views
{
	public partial class OutboundPage : UserControl
	{
		private static GXConfig config;

		private InventoryRepository _inventoryRepository;
		private InTransitRepository _inTransitRepository;
		private PriceRepository _priceRepository;
		private ASNRepository _asnRepository;
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
			_asnRepository = new ASNRepository(config.MainDbConnection);
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
			string dbPath = "MyDatabase.sqlite";

			Logger.Log($"TEST");

			if (!File.Exists(dbPath))
			{
				SQLiteConnection.CreateFile(dbPath);
				Console.WriteLine("Database created successfully.");
			}

			string connectionString = $"Data Source={dbPath};Version=3;";

			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				conn.Open();

				string createTableQuery = @"
						CREATE TABLE IF NOT EXISTS Users (
							Id INTEGER PRIMARY KEY AUTOINCREMENT,
							Name TEXT NOT NULL,
							Email TEXT UNIQUE NOT NULL,
							Age INTEGER
						);
					";

				using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
				{
					cmd.ExecuteNonQuery();
					Console.WriteLine("Table created successfully.");
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
				_asnRepository,
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
		public ASNRepository ASNRepository { get; set; }
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
			ASNRepository asnRepository,
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
			ASNRepository = asnRepository;
			StoreGoodsReturnRepository = storeGoodsReturnRepository;
			StoreSaleRepository = storeSaleRepository;
			StoreReturnRepository = storeReturnRepository;
			StoreInventoryAdjustmentRepository = storeInventoryAdjustmentRepository;
			StoreShippingRepository = storeShippingRepository;
			StoreReceivingRepository = storeReceivingRepository;
		}
	}
}
