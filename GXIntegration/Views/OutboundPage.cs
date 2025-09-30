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

		private TabControl tabControl;
		private TabPage tabEod, tabApi;

		private readonly OutboundRepositories repositories;

		public OutboundPage(OutboundRepositories repositories)
		{
			//Logger.Log("--------------------------------------------------------------------------");
			//Logger.Log("Starting OUTBOUND Process...");

			string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			InitializeComponent();
			InitializeTabs(repositories);
		}

		// ***************************************************
		// Initialization
		// ***************************************************

		public static async Task<bool> IsSidProcessedAsync(string sid)
		{
			string dbPath = Path.Combine(Application.StartupPath, "AppData", "TRANSACTION_PROCESS.db");
			string connectionString = $"Data Source={dbPath};Version=3;";

			using (SQLiteConnection conn = new SQLiteConnection(connectionString))
			{
				await conn.OpenAsync();

				string query = "SELECT COUNT(1) FROM TRANSACTION_PROCESS WHERE SID = @Sid";
				using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@Sid", sid);
					var result = await cmd.ExecuteScalarAsync();

					int count = Convert.ToInt32(result);
					return count > 0;
				}
			}
		}

		private void OutboundPage_Load(object sender, EventArgs e)
		{

		}

		private void InitializeTabs(OutboundRepositories repositories)
		{
			tabControl = new TabControl
			{
				Location = new Point(225, 10),
				Size = new Size(665, 440),
				Font = new Font("Segoe UI", 9)
			};

			tabEod = new TabPage("EOD");
			tabApi = new TabPage("API");

			tabEod.Controls.Add(new OutboundEODTab(config, repositories) { Dock = DockStyle.Fill });
			tabApi.Controls.Add(new OutboundAPITab(config, repositories) { Dock = DockStyle.Fill });

			this.Controls.Add(tabControl);
			tabControl.TabPages.Add(tabEod);
			tabControl.TabPages.Add(tabApi);
		}
		
	}
	public class OutboundRepositories
	{
		public PrismRepository PrismRepository { get; set; }
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
		public StoreInventoryCountRepository StoreInventoryCountRepository { get; set; }

		public OutboundRepositories(
			PrismRepository prismRepository,
			InventoryRepository inventoryRepository,
			InTransitRepository inTransitRepository,
			PriceRepository priceRepository,
			StoreGoodsRepository storeGoodsRepository,
			StoreGoodsReturnRepository storeGoodsReturnRepository,
			StoreSaleRepository storeSaleRepository,
			StoreReturnRepository storeReturnRepository,
			StoreInventoryAdjustmentRepository storeInventoryAdjustmentRepository,
			StoreShippingRepository storeShippingRepository,
			StoreReceivingRepository storeReceivingRepository,
			StoreInventoryCountRepository storeInventoryCountRepository)
		{
			PrismRepository = prismRepository;
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
			StoreInventoryCountRepository = storeInventoryCountRepository;
		}
	}
}
