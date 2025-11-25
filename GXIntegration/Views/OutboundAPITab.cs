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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class OutboundAPITab : UserControl
	{
		private OutboundRepositories _repositories;
		private GunaDataGridView guna1DataGridView1;

		private DateTimePicker datePickerFrom;
		private DateTimePicker datePickerTo;
		private Label lblFrom;
		private Label lblTo;
		private GunaButton btnSendXml;

		public OutboundAPITab(GXConfig config, OutboundRepositories repositories)
		{
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
				Location = new Point(20, 50),
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

			AddRow("1", "RETAIL_SALE", "StoreSale_[yyyymmddhhmmss]", ".xml");
			AddRow("2", "RETURN_SALE", "StoreReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("3", "ASN - RECEIVING", "StoreGoods_[yyyymmddhhmmss]", ".xml");
			AddRow("4", "RETURN_TO_DC", "StoreGoodsReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("5", "STORE_TRANSFER - SHIPPING", "StoreShipping_[yyyymmddhhmmss]", ".xml");
			AddRow("6", "STORE_TRANSFER - RECEIVING", "StoreReceiving_[yyyymmddhhmmss]", ".xml");
			AddRow("7", "ADJUSTMENT", "StoreInventoryAdjustment_[yyyymmddhhmmss]", ".xml");

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
			// --------------------
			// Date Range Controls
			// --------------------
			lblFrom = new Label
			{
				Text = "From:",
				Location = new Point(20, 24),
				AutoSize = true
			};

			datePickerFrom = new DateTimePicker
			{
				Location = new Point(70, 20),
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy-MM-dd hh:mm tt",
				Width = 160,
				ShowUpDown = false,
				Value = DateTime.Today
			};

			lblTo = new Label
			{
				Text = "To:",
				Location = new Point(250,24),
				AutoSize = true
			};

			datePickerTo = new DateTimePicker
			{
				Location = new Point(290, 20),
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
			btnSendXml = GlobalHelper.CreateButton(
				text: "Send XML to API",
				location: new Point(20, 240),
				clickAction: async () => await ManualSendXmlFilesToApi()
			);
			this.Controls.Add(btnSendXml);
		}
		
		public async Task TriggerAPIAsync(int reprocessTime)
		{
			await AutoSendXmlFilesToApi(reprocessTime);
		}

		// ***************************************************
		// Process Methods
		// ***************************************************
		public async Task ManualSendXmlFilesToApi()
		{
			bool isAuto = false;
			Logger.LogOutbound("[MANUAL - API] Start Manual Process...", isAuto);

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

			var selectedRows = guna1DataGridView1.Rows
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

			Logger.LogOutbound("[MANUAL - API]		Selected Transaction Types: " + string.Join(",", selectedRows), isAuto);

			if (!selectedRows.Any())
			{
				MessageBox.Show("Please select at least one transaction.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			btnSendXml.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;

			try
			{
				var fromDate = datePickerFrom.Value;
				var toDate = datePickerTo.Value;

				Logger.LogOutbound($"[MANUAL - API] Process DateRange From: {fromDate}, To: {toDate}", isAuto);

				var prismStores = await _repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
				foreach (var store in prismStores)
				{
					var storeCode = ((IDictionary<string, object>)store)
						.TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";

					Logger.LogOutbound($"[MANUAL - API]		StoreCode: {storeCode}", isAuto);

					var (storeSaleItems
						, storeReturnItems
						, storeGoodsItems
						, storeGoodsReturnItems
						, storeShippingItems
						, storeReceivingItems
						, storeInventoryAdjustmentItems
						) = await GetOutboundItemsAsync(fromDate, toDate, storeCode);

					var outboundConfigs = BuildOutboundConfigs(
						storeSaleItems
						, storeReturnItems
						, storeGoodsItems
						, storeGoodsReturnItems
						, storeShippingItems
						, storeReceivingItems
						, storeInventoryAdjustmentItems
						, saleApiUrl
						, inventoryApiUrl);

					var filteredConfigs = outboundConfigs
						.Where(cfg => selectedRows.Contains(cfg.DocType.ToLowerInvariant()));

					foreach (var cfg in filteredConfigs)
					{
						var itemsList = cfg.Items.ToList();
						if (!itemsList.Any())
						{
							Logger.LogOutbound($"[MANUAL - API]			No records for {cfg.DocType} in store {storeCode}", isAuto);
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
								password,
								isAuto
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

			MessageBox.Show($"API processed successfully. Full API responses saved to AppData folder ProcessedPrismTransactions.db.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public async Task AutoSendXmlFilesToApi(int reprocessTime)
		{
			bool isAuto = true;

			try
			{
				var apiConfig = GlobalHelper.LoadApiConnection();

				if (!apiConfig.TryGetValue("Username", out string username) ||
					!apiConfig.TryGetValue("Password", out string password) ||
					!apiConfig.TryGetValue("SaleApiUrl", out string saleApiUrl) ||
					!apiConfig.TryGetValue("InventoryApiUrl", out string inventoryApiUrl))
				{
					Logger.LogError("[ERROR] API configuration is missing. Please configure API settings first.", isAuto);
					return;
				}

				var (fromDate, toDate) = GlobalHelper.GetSystemTimeRange(reprocessTime);
				Logger.LogOutbound($"[AUTO - API] Process DateRange From: {fromDate}, To: {toDate}", isAuto);

				var prismStores = await _repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
				foreach (var store in prismStores)
				{
					var storeCode = ((IDictionary<string, object>)store)
						.TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";

					// logger.log($"[AUTO - API]	StoreCode: {storeCode}");

					var (storeSaleItems
						, storeReturnItems
						, storeGoodsItems
						, storeGoodsReturnItems
						, storeShippingItems
						, storeReceivingItems
						, storeInventoryAdjustmentItems						
						) = await GetOutboundItemsAsync(fromDate, toDate, storeCode);

					var outboundConfigs = BuildOutboundConfigs(
						storeSaleItems
						, storeReturnItems
						, storeGoodsItems
						, storeGoodsReturnItems
						, storeShippingItems
						, storeReceivingItems
						, storeInventoryAdjustmentItems
						, saleApiUrl
						, inventoryApiUrl);

					foreach (var cfg in outboundConfigs)
					{
						var itemsList = cfg.Items.ToList();
						if (!itemsList.Any())
						{
							// logger.log($"[AUTO - API]		No records for {cfg.DocType} in store {storeCode}");
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
								password, 
								isAuto
							);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"[ERROR] AutoSendXmlFilesToApi failed: {ex}", isAuto);
			}
		}

		private async Task<(
			IEnumerable<object> storeSaleItems,
			IEnumerable<object> storeReturnItems,
			IEnumerable<object> storeGoodsItems,
			IEnumerable<object> storeGoodsReturnItems,
			IEnumerable<object> storeShippingItems,
			IEnumerable<object> storeReceivingItems,
			IEnumerable<object> storeInventoryAdjustmentItems
			)>
		GetOutboundItemsAsync(DateTime fromDate, DateTime toDate, string storeCode)
		{
			var storeSaleItems = await _repositories.StoreSaleRepository
				.GetStoreSaleAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeReturnItems = await _repositories.StoreReturnRepository
				.GetStoreReturnAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeGoodsItems = await _repositories.StoreGoodsRepository
				.GetStoreGoodsAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeGoodsReturnItems = await _repositories.StoreGoodsReturnRepository
				.GetStoreGoodsReturnAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeShippingItems = await _repositories.StoreShippingRepository
				.GetStoreShippingAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeReceivingItems = await _repositories.StoreReceivingRepository
				.GetStoreReceivingAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			var storeInventoryAdjustmentItems = await _repositories.StoreInventoryAdjustmentRepository
				.GetStoreInventoryAdjustmentAsync(fromDate, toDate, storeCode, "API")
				?? Enumerable.Empty<object>();

			return (storeSaleItems
				, storeReturnItems
				, storeGoodsItems
				, storeGoodsReturnItems
				, storeShippingItems
				, storeReceivingItems
				, storeInventoryAdjustmentItems				
				);
		}

		private List<OutboundConfig> BuildOutboundConfigs(
			IEnumerable<object> storeSaleItems,
			IEnumerable<object> storeReturnItems,
			IEnumerable<object> storeGoodsItems,
			IEnumerable<object> storeGoodsReturnItems,
			IEnumerable<object> storeShippingItems,
			IEnumerable<object> storeReceivingItems,
			IEnumerable<object> storeInventoryAdjustmentItems,
			string saleApiUrl,
			string inventoryApiUrl)
		{
			var configs = new List<OutboundConfig>
			{
				new OutboundConfig
				{
					Items = storeSaleItems,
					GetSid = i => ((StoreSaleModel)i).DocSid.ToString(),
					GetDocNo = i => ((StoreSaleModel)i).TransSequenceNo.ToString(),
					GetDate = i => ((StoreSaleModel)i).TransBusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreSale.GenerateXml(list.Cast<StoreSaleModel>().ToList(), null, "template"),
					DocType = "storesale",
					ApiUrl = saleApiUrl
				},
				new OutboundConfig
				{
					Items = storeReturnItems,
					GetSid = i => ((StoreReturnModel)i).DocSid.ToString(),
					GetDocNo = i => ((StoreReturnModel)i).SequenceNo.ToString(),
					GetDate = i => ((StoreReturnModel)i).BusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreReturn.GenerateXml(list.Cast<StoreReturnModel>().ToList(), null, "template"),
					DocType = "storereturn",
					ApiUrl = saleApiUrl
				},
				new OutboundConfig
				{
					Items = storeGoodsItems,
					GetSid = i => ((StoreGoodsModel)i).VouSid.ToString(),
					GetDocNo = i => ((StoreGoodsModel)i).TransSequenceNo.ToString(),
					GetDate = i => ((StoreGoodsModel)i).TransBusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreGoods.GenerateXml(list.Cast<StoreGoodsModel>().ToList(), null, "template"),
					DocType = "storegoods",
					ApiUrl = inventoryApiUrl
				},
				new OutboundConfig
				{
					Items = storeGoodsReturnItems,
					GetSid = i => ((StoreGoodsReturnModel)i).VouSid.ToString(),
					GetDocNo = i => ((StoreGoodsReturnModel)i).TransSequenceNo.ToString(),
					GetDate = i => ((StoreGoodsReturnModel)i).TransBusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreGoodsReturn.GenerateXml(list.Cast<StoreGoodsReturnModel>().ToList(), null, "template"),
					DocType = "storegoodsreturn",
					ApiUrl = inventoryApiUrl
				},
				new OutboundConfig
				{
					Items = storeShippingItems,
					GetSid = i => ((StoreShippingModel)i).SlipSid.ToString(),
					GetDocNo = i => ((StoreShippingModel)i).SequenceNo.ToString(),
					GetDate = i => ((StoreShippingModel)i).BusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreShipping.GenerateXml(list.Cast<StoreShippingModel>().ToList(), null, "template"),
					DocType = "storeshipping",
					ApiUrl = inventoryApiUrl
				},
				new OutboundConfig
				{
					Items = storeReceivingItems,
					GetSid = i => ((StoreReceivingModel)i).VouSid.ToString(),
					GetDocNo = i => ((StoreReceivingModel)i).SequenceNo.ToString(),
					GetDate = i => ((StoreReceivingModel)i).BusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreReceiving.GenerateXml(list.Cast<StoreReceivingModel>().ToList(), null, "template"),
					DocType = "storereceiving",
					ApiUrl = inventoryApiUrl
				},
				new OutboundConfig
				{
					Items = storeInventoryAdjustmentItems,
					GetSid = i => ((StoreInventoryAdjustmentModel)i).AdjSid.ToString(),
					GetDocNo = i => ((StoreInventoryAdjustmentModel)i).SequenceNo.ToString(),
					GetDate = i => ((StoreInventoryAdjustmentModel)i).BusinessDayDate.DateTime,
					XmlGen = list => OutboundStoreInventoryAdjustment.GenerateXml(list.Cast<StoreInventoryAdjustmentModel>().ToList(), null, "template"),
					DocType = "storeinventoryadjustment",
					ApiUrl = inventoryApiUrl
				},
			};

			return configs;
		}

		private readonly Dictionary<string, string> _docTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "RETAIL_SALE", "storesale" },
			{ "RETURN_SALE", "storereturn" },
			{ "ASN - RECEIVING", "storegoods" },
			{ "RETURN_TO_DC", "storegoodsreturn" },
			{ "STORE_TRANSFER - SHIPPING", "storeshipping" },
			{ "STORE_TRANSFER - RECEIVING", "storereceiving" },
			{ "ADJUSTMENT", "storeinventoryadjustment" }			
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
		string password, 
		bool isAuto)
		{

			if (items == null || !items.Any())
			{
				Logger.LogOutbound($"No {docType} data found to send.", isAuto);
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
							Logger.LogOutbound($"SID {sid} already processed. Skipping.", isAuto);
							continue;
						}

						string xml = generateXmlFunc(new List<T> { item });
						string cleanXml = Regex.Replace(xml, @"<\?xml.*?\?>", "").Trim();

						var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<POSLog xmlns=""http://www.nrf-arts.org/IXRetail/namespace/""
        xmlns:dtv=""http://www.datavantagecorp.com/xstore/""
        xmlns:xs=""http://www.w3.org/2001/XMLSchema-instance""
        xs:schemaLocation=""http://www.nrf-arts.org/IXRetail/namespace/ POSLog.xsd"" >
    {xml}
</POSLog>";

						// Logger.LogOutbound($"SOAP Payload for SID {sid}:\n{soapEnvelope}", isAuto);

						// Save to OUTBOUND\API\PERTRANSACTION
						string folderFormattedDate = docDate.ToString("MMddyyyy");
						string baseDir = AppDomain.CurrentDomain.BaseDirectory;
						string apiFolder = Path.Combine(baseDir, "OUTBOUND", "API");
						string transactionFolder = Path.Combine(apiFolder, "TRANSACTION");
						string dateFolder = Path.Combine(transactionFolder, folderFormattedDate);

						// Ensure directories exist
						Directory.CreateDirectory(apiFolder);
						Directory.CreateDirectory(transactionFolder);
						Directory.CreateDirectory(dateFolder);

						string fileFormattedDate = docDate.ToString("ddMMyyyyHHmmss");
						string fileName = Path.Combine(dateFolder, $"{docType}_{fileFormattedDate}_{docNo}.xml");

						using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
						{
							await writer.WriteAsync(soapEnvelope);
						}

						// Start sending to API
						var content = new System.Net.Http.StringContent(soapEnvelope, System.Text.Encoding.UTF8, "application/xml");
						var response = await client.PostAsync(apiUrl, content);
						string responseContent = await response.Content.ReadAsStringAsync();

						string singleLineResponse = responseContent.Replace("\r", "").Replace("\n", "").Trim();
						Logger.LogOutbound($"[API] Response for SID {sid}: {responseContent}", isAuto);

						if (response.IsSuccessStatusCode)
						{
							Logger.LogOutbound($"[API] XML sent to API Mulesoft SUCCESSFULLY | Type: {docType} | SID: {sid} | DocNo: {docNo}", isAuto);
							InsertProcessedTransaction(sid, docNo, docType, docDate.ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Success", responseContent);
						}
						else
						{
							Logger.LogOutbound($"[API] XML sent to API Mulesoft FAILED | Type: {docType} | SID: {sid} | DocNo: {docNo} | Status: {response.StatusCode} | Reason: {response.ReasonPhrase}", isAuto);
							InsertProcessedTransaction(sid, docNo, docType, docDate.ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Failed", responseContent);
						}

					}
					catch (Exception ex)
					{
						string sid = getSid(item);
						string docNo = getDocNo(item);
						Logger.LogError($"[API ERROR] {docType} | SID {sid} | Exception: {ex}", isAuto);
						InsertProcessedTransaction(sid, docNo, docType, getCreatedDate(item).ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Error");
					}
				}
			}
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

		private void InsertProcessedTransaction(string sid, string docNo, string type, string createdDatetime, string status, string response = null)
		{
			EnsureDatabase();

			string dbPath = Path.Combine(Application.StartupPath, "AppData", "ProcessedPrismTransactions.db");
			string connStr = $"Data Source={dbPath};Version=3;";
			string currentDatetime = DateTime.Now.ToString("dd-MMM-yy hh:mm:ss tt zzz");

			using (var conn = new SQLiteConnection(connStr))
			{
				conn.Open();
				string insert = @"
					INSERT INTO ProcessedPrismTransactions 
						(SID
						, DOC_NO
						, TYPE
						, CREATED_DATETIME
						, PROCESSED_DATETIME
						, STATUS
						, RESPONSE)
					VALUES 
						(@SID
						, @DOCNO
						, @TYPE
						, @CREATEDDATETIME
						, @PROCESSEDDATETIME
						, @STATUS
						, @RESPONSE)
					ON CONFLICT(SID, TYPE) DO 
						UPDATE 
							SET
								STATUS = excluded.STATUS,
								PROCESSED_DATETIME = excluded.PROCESSED_DATETIME,
								RESPONSE = excluded.RESPONSE
				";
				using (var cmd = new SQLiteCommand(insert, conn))
				{
					cmd.Parameters.AddWithValue("@SID", sid);
					cmd.Parameters.AddWithValue("@DOCNO", docNo);
					cmd.Parameters.AddWithValue("@TYPE", type);
					cmd.Parameters.AddWithValue("@CREATEDDATETIME", createdDatetime);
					cmd.Parameters.AddWithValue("@PROCESSEDDATETIME", currentDatetime);
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
						DOC_NO TEXT,
						TYPE TEXT,
						CREATED_DATETIME TEXT,
						PROCESSED_DATETIME TEXT,
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
