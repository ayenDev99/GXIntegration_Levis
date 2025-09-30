using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using GXIntegration_Levis.OutboundHandlers;
using GXIntegration_Levis.Properties;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class OutboundAPITab : UserControl
	{
		private GXConfig _config;
		private OutboundRepositories _repositories;
		private GunaDataGridView guna1DataGridView1;

		private GunaButton btnSendXml;

		public OutboundAPITab(GXConfig config, OutboundRepositories repositories)
		{
			_config = config;
			_repositories = repositories;

			InitializeComponent();
			InitializeControls();
			InitializeGrid();
		}

		// ***************************************************
		// Initialization Methods
		// ***************************************************
		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(20, 20),
				Size = new Size(615, 180),
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

			// Add checkbox column FIRST
			var checkBoxColumn = new DataGridViewCheckBoxColumn
			{
				Name = "Select",
				HeaderText = "",
				Width = 40,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None
			};
			guna1DataGridView1.Columns.Add(checkBoxColumn);

			guna1DataGridView1.Columns.Add("ID", "ID");
			guna1DataGridView1.Columns.Add("Name", "Name");
			guna1DataGridView1.Columns.Add("FileNameFormat", "File Name Format");
			guna1DataGridView1.Columns.Add("Type", "Type");

			guna1DataGridView1.Columns["ID"].Width = 20;
			guna1DataGridView1.Columns["Name"].Width = 200;
			guna1DataGridView1.Columns["FileNameFormat"].Width = 310;
			guna1DataGridView1.Columns["Type"].Width = 45;

			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			void AddRow(string id, string name, string format, string type)
				=> guna1DataGridView1.Rows.Add(false, id, name, format, type, Resources.icon_download);

			AddRow("1", "ASN - RECEIVING", "StoreGoods_[yyyymmddhhmmss]", ".xml");
			AddRow("2", "RETURN_TO_DC", "StoreGoodsReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("3", "RETAIL_SALE", "StoreSale_[yyyymmddhhmmss]", ".xml");
			AddRow("4", "RETURN_SALE", "StoreReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("5", "ADJUSTMENT", "StoreInventoryAdjustment_[yyyymmddhhmmss]", ".xml");
			AddRow("6", "STORE_TRANSFER - SHIPPING", "StoreShipping_[yyyymmddhhmmss]", ".xml");
			AddRow("7", "STORE_TRANSFER - RECEIVING", "StoreReceiving_[yyyymmddhhmmss]", ".xml");

			this.Controls.Add(guna1DataGridView1);

			guna1DataGridView1.CurrentCellDirtyStateChanged += (s, e) =>
			{
				if (guna1DataGridView1.CurrentCell is DataGridViewCheckBoxCell)
				{
					guna1DataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};

		}

		private void InitializeControls()
		{
			btnSendXml = GlobalHelper.CreateButton(
				text: "Send XML to API",
				location: new Point(20, 300),
				clickAction: async () => await ManualSendXmlFilesToApi()
			);

			this.Controls.Add(btnSendXml);
		}

		public async Task TriggerAPIAsync()
		{
			await AutoSendXmlFilesToApi();
		}

		// ***************************************************
		// Process Methods
		// ***************************************************

		public async Task ManualSendXmlFilesToApi()
		{
			Logger.Log("[OUTBOUND API-MANUAL] Start Manual Reprocess...");

			guna1DataGridView1.EndEdit();

			var apiConfig = GlobalHelper.LoadApiConnection();

			if (!apiConfig.TryGetValue("Username", out string username) ||
				!apiConfig.TryGetValue("Password", out string password) ||
				!apiConfig.TryGetValue("SaleApiUrl", out string saleApiUrl) ||
				!apiConfig.TryGetValue("InventoryApiUrl", out string inventoryApiUrl))
			{
				MessageBox.Show("[ERROR] API configuration is missing. Please navigate to the 'Configuration API' tab to set up the API connection.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var selectedDocTypes = guna1DataGridView1.Rows
								.Cast<DataGridViewRow>()
								.Where(r => r.Cells["Select"].Value is bool b && b)
								.Select(r =>
								{
									var name = r.Cells["Name"].Value?.ToString();
									return string.IsNullOrWhiteSpace(name) ? null :
										   _docTypeMap.TryGetValue(name, out var mapped) ? mapped : null;
								})
								.Where(s => !string.IsNullOrEmpty(s))
								.ToHashSet();

			Logger.Log("[OUTBOUND API-MANUAL]		Selected Transaction Types: " + string.Join(",", selectedDocTypes));

			if (!selectedDocTypes.Any())
			{
				MessageBox.Show("Please select at least one transaction.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			btnSendXml.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;

			try
			{
				var config = GXConfig.Load("config.xml");
				var reprocessMinutes = config.ReprocessMinutes;
				var (fromDate, toDate) = GlobalHelper.GetSystemTimeRange(reprocessMinutes);

				Logger.Log($"[OUTBOUND API-MANUAL]		Process DateRange From: {fromDate}, To: {toDate}");

				var prismStores = await _repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
				foreach (var store in prismStores)
				{
					var storeCode = ((IDictionary<string, object>)store)
						.TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";

					Logger.Log($"[OUTBOUND API-MANUAL]		StoreCode: {storeCode}");

					var (storeSaleItems,
						 storeShippingItems,
						 storeReceivingItems,
						 storeInventoryAdjustmentItems,
						 storeReturnItems,
						 storeGoodsReturnItems,
						 storeGoodsItems) = await GetOutboundItemsAsync(fromDate, toDate, storeCode);

					var outboundConfigs = BuildOutboundConfigs(
						storeSaleItems, storeShippingItems, storeReceivingItems,
						storeInventoryAdjustmentItems, storeReturnItems,
						storeGoodsReturnItems, storeGoodsItems,
						saleApiUrl, inventoryApiUrl);

					var filteredConfigs = outboundConfigs
						.Where(cfg => selectedDocTypes.Contains(cfg.DocType.ToLowerInvariant()));

					foreach (var cfg in filteredConfigs)
					{
						var itemsList = cfg.Items.ToList();
						if (!itemsList.Any())
						{
							Logger.Log($"[OUTBOUND API-MANUAL]			No records for {cfg.DocType} in store {storeCode}");
							continue;
						}

						foreach (var item in itemsList)
						{
							await SendOutboundDataAsync(
								new List<object> { item },
								cfg.GetSid,
								cfg.GetDocNo,
								cfg.GetDate,
								cfg.XmlGen,
								cfg.DocType,
								cfg.ApiUrl,
								username,
								password
							);
						}
					}
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
				btnSendXml.Enabled = true;
			}

		}

		public async Task AutoSendXmlFilesToApi()
		{
			Logger.Log("[OUTBOUND API-AUTO] Start Auto Process...");

			try
			{
				var apiConfig = GlobalHelper.LoadApiConnection();

				if (!apiConfig.TryGetValue("Username", out string username) ||
					!apiConfig.TryGetValue("Password", out string password) ||
					!apiConfig.TryGetValue("SaleApiUrl", out string saleApiUrl) ||
					!apiConfig.TryGetValue("InventoryApiUrl", out string inventoryApiUrl))
				{
					Logger.Log("[ERROR] API configuration is missing. Please configure API settings first.");
					return;
				}

				var config = GXConfig.Load("config.xml");
				var reprocessMinutes = config.ReprocessMinutes;
				var (fromDate, toDate) = GlobalHelper.GetSystemTimeRange(reprocessMinutes);

				Logger.Log($"[OUTBOUND API-AUTO]	Process DateRange From: {fromDate}, To: {toDate}");

				var prismStores = await _repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
				foreach (var store in prismStores)
				{
					var storeCode = ((IDictionary<string, object>)store)
						.TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";

					Logger.Log($"[OUTBOUND API-AUTO]	StoreCode: {storeCode}");

					var (storeSaleItems,
						 storeShippingItems,
						 storeReceivingItems,
						 storeInventoryAdjustmentItems,
						 storeReturnItems,
						 storeGoodsReturnItems,
						 storeGoodsItems) = await GetOutboundItemsAsync(fromDate, toDate, storeCode);

					var outboundConfigs = BuildOutboundConfigs(
						storeSaleItems, storeShippingItems, storeReceivingItems,
						storeInventoryAdjustmentItems, storeReturnItems,
						storeGoodsReturnItems, storeGoodsItems,
						saleApiUrl, inventoryApiUrl);

					foreach (var cfg in outboundConfigs)
					{
						var itemsList = cfg.Items.ToList();
						if (!itemsList.Any())
						{
							Logger.Log($"[OUTBOUND API-AUTO]		No records for {cfg.DocType} in store {storeCode}");
							continue;
						}

						foreach (var item in itemsList)
						{
							await SendOutboundDataAsync(
								new List<object> { item },
								cfg.GetSid,
								cfg.GetDocNo,
								cfg.GetDate,
								cfg.XmlGen,
								cfg.DocType,
								cfg.ApiUrl,
								username,
								password
							);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"[ERROR] AutoSendXmlFilesToApi failed: {ex}");
			}
		}

		private async Task<(IEnumerable<object> storeSaleItems,
		IEnumerable<object> storeShippingItems,
		IEnumerable<object> storeReceivingItems,
		IEnumerable<object> storeInventoryAdjustmentItems,
		IEnumerable<object> storeReturnItems,
		IEnumerable<object> storeGoodsReturnItems,
		IEnumerable<object> storeGoodsItems)>
		GetOutboundItemsAsync(DateTime fromDate, DateTime toDate, string storeCode)
		{
			var storeSaleItems = await _repositories.StoreSaleRepository
				.GetStoreSaleAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeShippingItems = await _repositories.StoreShippingRepository
				.GetStoreShippingAsync(fromDate, toDate, storeCode)
				?? Enumerable.Empty<object>();

			var storeReceivingItems = await _repositories.StoreReceivingRepository
				.GetStoreReceivingAsync(fromDate, toDate, storeCode)
				?? Enumerable.Empty<object>();

			var storeInventoryAdjustmentItems = await _repositories.StoreInventoryAdjustmentRepository
				.GetStoreInventoryAdjustmentAsync(fromDate, toDate, storeCode)
				?? Enumerable.Empty<object>();

			var storeReturnItems = await _repositories.StoreReturnRepository
				.GetStoreReturnAsync(fromDate, toDate, storeCode)
				?? Enumerable.Empty<object>();

			var storeGoodsReturnItems = await _repositories.StoreGoodsReturnRepository
				.GetStoreGoodsReturnAsync(fromDate, toDate, storeCode)
				?? Enumerable.Empty<object>();

			var storeGoodsItems = await _repositories.StoreGoodsRepository
				.GetStoreGoodsAsync(fromDate, toDate, storeCode)
				?? Enumerable.Empty<object>();

			return (storeSaleItems, storeShippingItems, storeReceivingItems,
					storeInventoryAdjustmentItems, storeReturnItems,
					storeGoodsReturnItems, storeGoodsItems);
		}

		private List<OutboundConfig> BuildOutboundConfigs(
		IEnumerable<object> storeSaleItems,
		IEnumerable<object> storeShippingItems,
		IEnumerable<object> storeReceivingItems,
		IEnumerable<object> storeInventoryAdjustmentItems,
		IEnumerable<object> storeReturnItems,
		IEnumerable<object> storeGoodsReturnItems,
		IEnumerable<object> storeGoodsItems,
		string saleApiUrl,
		string inventoryApiUrl)
		{
			var configs = new List<OutboundConfig>
			{
				//new OutboundConfig
				//{
				//	Items = storeGoodsItems,
				//	GetSid = i => ((StoreGoodsModel)i).VouSid.ToString(),
				//	GetDocNo = i => ((StoreGoodsModel)i).SequenceNo.ToString(),
				//	GetDate = i => ((StoreGoodsModel)i).BusinessDayDate.DateTime,
				//	XmlGen = list => OutboundStoreGoods.GenerateXml(list.Cast<StoreGoodsModel>().ToList(), null, "template"),
				//	DocType = "storegoods",
				//	ApiUrl = inventoryApiUrl
				//},
				//new OutboundConfig
				//{
				//	Items = storeGoodsReturnItems,
				//	GetSid = i => ((StoreGoodsReturnModel)i).VouSid.ToString(),
				//	GetDocNo = i => ((StoreGoodsReturnModel)i).SequenceNo.ToString(),
				//	GetDate = i => ((StoreGoodsReturnModel)i).BusinessDayDate.DateTime,
				//	XmlGen = list => OutboundStoreGoodsReturn.GenerateXml(list.Cast<StoreGoodsReturnModel>().ToList(), null, "template"),
				//	DocType = "storegoodsreturn",
				//	ApiUrl = inventoryApiUrl
				//},
				new OutboundConfig
				{
					Items = storeSaleItems,
					GetSid = i => ((StoreSaleModel)i).DocSid.ToString(),
					GetDocNo = i => ((StoreSaleModel)i).SequenceNo.ToString(),
					GetDate = i => ((StoreSaleModel)i).BusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreSale.GenerateXml(list.Cast<StoreSaleModel>().ToList(), null, "template"),
					DocType = "storesale",
					ApiUrl = saleApiUrl
				},
				//new OutboundConfig
				//{
				//	Items = storeReturnItems,
				//	GetSid = i => ((StoreReturnModel)i).DocSid.ToString(),
				//	GetDocNo = i => ((StoreReturnModel)i).SequenceNo.ToString(),
				//	GetDate = i => ((StoreReturnModel)i).BusinessDayDate.DateTime,
				//	XmlGen = list => OutboundStoreReturn.GenerateXml(list.Cast<StoreReturnModel>().ToList(), null, "template"),
				//	DocType = "storereturn",
				//	ApiUrl = saleApiUrl
				//},
				//new OutboundConfig
				//{
				//	Items = storeInventoryAdjustmentItems,
				//	GetSid = i => ((StoreInventoryAdjustmentModel)i).AdjSid.ToString(),
				//	GetDocNo = i => ((StoreInventoryAdjustmentModel)i).SequenceNo.ToString(),
				//	GetDate = i => ((StoreInventoryAdjustmentModel)i).BusinessDayDate.DateTime,
				//	XmlGen = list => OutboundStoreInventoryAdjustment.GenerateXml(list.Cast<StoreInventoryAdjustmentModel>().ToList(), null, "template"),
				//	DocType = "storeinventoryadjustment",
				//	ApiUrl = inventoryApiUrl
				//},
				//new OutboundConfig
				//{
				//	Items = storeShippingItems,
				//	GetSid = i => ((StoreShippingModel)i).VouSid.ToString(),
				//	GetDocNo = i => ((StoreShippingModel)i).SequenceNo.ToString(),
				//	GetDate = i => ((StoreShippingModel)i).BusinessDayDate.DateTime,
				//	XmlGen = list => OutboundStoreShipping.GenerateXml(list.Cast<StoreShippingModel>().ToList(), null, "template"),
				//	DocType = "storeshipping",
				//	ApiUrl = inventoryApiUrl
				//},
				//new OutboundConfig
				//{
				//	Items = storeReceivingItems,
				//	GetSid = i => ((StoreReceivingModel)i).VouSid.ToString(),
				//	GetDocNo = i => ((StoreReceivingModel)i).SequenceNo.ToString(),
				//	GetDate = i => ((StoreReceivingModel)i).BusinessDayDate.DateTime,
				//	XmlGen = list => OutboundStoreReceiving.GenerateXml(list.Cast<StoreReceivingModel>().ToList(), null, "template"),
				//	DocType = "storereceiving",
				//	ApiUrl = inventoryApiUrl
				//}
			};

			return configs;
		}

		private readonly Dictionary<string, string> _docTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "RETAIL_SALE", "storesale" },
			{ "RETURN_SALE", "storereturn" },
			{ "ADJUSTMENT", "storeinventoryadjustment" },
			{ "STORE_TRANSFER - SHIPPING", "storeshipping" },
			{ "STORE_TRANSFER - RECEIVING", "storereceiving" },
			{ "ASN - RECEIVING", "storegoods" },
			{ "RETURN_TO_DC", "storegoodsreturn" }
		};

		public class OutboundConfig
		{
			public IEnumerable<object> Items { get; set; }
			public Func<object, string> GetSid { get; set; }
			public Func<object, string> GetDocNo { get; set; }
			public Func<object, DateTime> GetDate { get; set; }
			public Func<List<object>, string> XmlGen { get; set; }
			public string DocType { get; set; }
			public string ApiUrl { get; set; }
		}

		private async Task SendOutboundDataAsync<T>(
		List<T> items,
		Func<T, string> getSid,
		Func<T, string> getDocNo,
		Func<T, DateTime> getCreatedDate,
		Func<List<T>, string> generateXmlFunc,
		string docType,
		string apiUrl,
		string username,
		string password)
		{
			if (items == null || !items.Any())
			{
				Logger.Log($"No {docType} data found to send.");
				MessageBox.Show($"No {docType} data found to send.", "API Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			using (var client = new System.Net.Http.HttpClient())
			{
				var byteArray = System.Text.Encoding.UTF8.GetBytes($"{username}:{password}");
				client.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

				foreach (var item in items)
				{
					try
					{
						string sid = getSid(item);
						string docNo = getDocNo(item);
						DateTime docDate = getCreatedDate(item);

						if (IsSidAlreadyProcessed(sid))
						{
							Logger.Log($"SID {sid} already processed. Skipping.");
							continue;
						}

						string xml = generateXmlFunc(new List<T> { item });

						var soapEnvelope = $@"<?xml version=""1.0"" ?>
							<S:Envelope xmlns:S=""http://schemas.xmlsoap.org/soap/envelope/"">
							  <S:Body>
								<ns2:postTransaction xmlns:ns2=""http://v1.ws.poslog.xcenter.dtv/"">
								  <rawPoslogString>{System.Security.SecurityElement.Escape(xml)}</rawPoslogString>
								</ns2:postTransaction>
							  </S:Body>
							</S:Envelope>";

						var content = new System.Net.Http.StringContent(soapEnvelope, System.Text.Encoding.UTF8, "application/xml");
						var response = await client.PostAsync(apiUrl, content);
						string responseContent = await response.Content.ReadAsStringAsync();

						if (response.IsSuccessStatusCode)
						{
							Logger.Log($"[OUTBOUND API]			XML sent to API Mulesoft SUCCESSFULLY | Type: {docType} | SID: {sid} | DocNo: {docNo} | Status: {response.StatusCode}");
							InsertProcessedTransaction(sid, docType, docDate.ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Success", responseContent);
						}
						else
						{
							Logger.Log($"[OUTBOUND API]			XML sent to API Mulesoft FAILED | Type: {docType} | SID: {sid} | DocNo: {docNo} | Status: {response.StatusCode} | Reason: {response.ReasonPhrase}");
							InsertProcessedTransaction(sid, docType, docDate.ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Failed", responseContent);
						}

					}
					catch (Exception ex)
					{
						string sid = getSid(item);
						Logger.Log($"[API ERROR] {docType} | SID {sid} | Exception: {ex}");
						InsertProcessedTransaction(sid, docType, getCreatedDate(item).ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Error");
					}
				}
			}

			//MessageBox.Show($"{docType.ToUpper()} data processed. Full API responses saved to AppData folder.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private bool IsSidAlreadyProcessed(string sid)
		{
			EnsureDatabase();

			string dbPath = Path.Combine(Application.StartupPath, "AppData", "ProcessedPrismTransactions.db");
			string connStr = $"Data Source={dbPath};Version=3;";

			using (var conn = new SQLiteConnection(connStr))
			{
				conn.Open();
				string query = "SELECT COUNT(*) FROM ProcessedPrismTransactions WHERE SID = @SID";
				using (var cmd = new SQLiteCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@SID", sid);
					long count = (long)cmd.ExecuteScalar();
					return count > 0;
				}
			}
		}

		private void InsertProcessedTransaction(string sid, string type, string date, string status, string response = null)
		{
			EnsureDatabase();

			string dbPath = Path.Combine(Application.StartupPath, "AppData", "ProcessedPrismTransactions.db");
			string connStr = $"Data Source={dbPath};Version=3;";

			using (var conn = new SQLiteConnection(connStr))
			{
				conn.Open();
				string insert = @"
					INSERT INTO ProcessedPrismTransactions (SID, TYPE, DATE, STATUS, RESPONSE)
					VALUES (@SID, @TYPE, @DATE, @STATUS, @RESPONSE)
					ON CONFLICT(SID, TYPE) DO UPDATE SET
						STATUS = excluded.STATUS,
						DATE = excluded.DATE,
						RESPONSE = excluded.RESPONSE
				";
				using (var cmd = new SQLiteCommand(insert, conn))
				{
					cmd.Parameters.AddWithValue("@SID", sid);
					cmd.Parameters.AddWithValue("@TYPE", type);
					cmd.Parameters.AddWithValue("@DATE", date);
					cmd.Parameters.AddWithValue("@STATUS", status);
					cmd.Parameters.AddWithValue("@RESPONSE", response ?? string.Empty);
					cmd.ExecuteNonQuery();
				}
			}
		}

		private void EnsureDatabase()
		{
			string dbPath = Path.Combine(Application.StartupPath, "AppData", "ProcessedPrismTransactions.db");
			Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

			string connStr = $"Data Source={dbPath};Version=3;";

			using (var conn = new SQLiteConnection(connStr))
			{
				conn.Open();

				string createTable = @"
					CREATE TABLE IF NOT EXISTS ProcessedPrismTransactions (
						SID TEXT,
						TYPE TEXT,
						DATE TEXT,
						STATUS TEXT,
						RESPONSE TEXT,
						PRIMARY KEY (SID, TYPE)
					)";

				using (var cmd = new SQLiteCommand(createTable, conn))
				{
					cmd.ExecuteNonQuery();
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
