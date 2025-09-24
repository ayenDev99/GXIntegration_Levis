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
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

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
				clickAction: async () => await SendXmlFilesToApi()
			);

			this.Controls.Add(btnSendXml);
		}

		// ***************************************************
		// Process Methods
		// ***************************************************
		private async Task SendXmlFilesToApi()
		{
			// ✅ Commit any pending edits before reading checkbox values
			guna1DataGridView1.EndEdit();

			var apiConfig = GlobalHelper.LoadApiConnection();
			string username = apiConfig["Username"];
			string password = apiConfig["Password"];
			string saleApiUrl = apiConfig["SaleApiUrl"];
			string inventoryApiUrl = apiConfig["InventoryApiUrl"];

			// ✅ Safer checkbox selection
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

			Logger.Log("Selected DocTypes: " + string.Join(",", selectedDocTypes));

			if (!selectedDocTypes.Any())
			{
				MessageBox.Show("Please select at least one process.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			btnSendXml.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;

			var results = new List<string>();

			try
			{
				var timeRange = TimeHelper.GetPhilippineTimeRange(60);
				var prismStores = await _repositories.PrismRepository.GetRpsStore("ACTIVE", "1");

				foreach (var store in prismStores)
				{
					var storeCode = ((IDictionary<string, object>)store)
						.TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";

					Logger.Log($"[STORE] {storeCode}");

					// ✅ fetch data safely (always fallback to empty list)
					var storeSaleItems = await _repositories.StoreSaleRepository.GetStoreSaleAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();
					var storeShippingItems = await _repositories.StoreShippingRepository.GetStoreShippingAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();
					var storeReceivingItems = await _repositories.StoreReceivingRepository.GetStoreReceivingAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();
					var storeInventoryAdjustmentItems = await _repositories.StoreInventoryAdjustmentRepository.GetStoreInventoryAdjustmentAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();
					var storeReturnItems = await _repositories.StoreReturnRepository.GetStoreReturnAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();
					var storeGoodsReturnItems = await _repositories.StoreGoodsReturnRepository.GetStoreGoodsReturnAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();
					var storeGoodsItems = await _repositories.StoreGoodsRepository.GetStoreGoodsAsync(timeRange.from_date, timeRange.to_date, storeCode) ?? Enumerable.Empty<object>();

					// ✅ config setup
					var outboundConfigs = new List<OutboundConfig>
					{
						new OutboundConfig
						{
							Items = storeGoodsItems,
							GetSid = i => ((StoreGoodsModel)i).VouSid.ToString(),
							GetDocNo = i => ((StoreGoodsModel)i).SequenceNo.ToString(),
							GetDate = i => ((StoreGoodsModel)i).BusinessDayDate.DateTime,
							XmlGen = list => OutboundStoreGoods.GenerateXml(list.Cast<StoreGoodsModel>().ToList(), null, "template"),
							DocType = "storegoods",
							ApiUrl = inventoryApiUrl
						},
						new OutboundConfig
						{
							Items = storeGoodsReturnItems,
							GetSid = i => ((StoreGoodsReturnModel)i).VouSid.ToString(),
							GetDocNo = i => ((StoreGoodsReturnModel)i).SequenceNo.ToString(),
							GetDate = i => ((StoreGoodsReturnModel)i).BusinessDayDate.DateTime,
							XmlGen = list => OutboundStoreGoodsReturn.GenerateXml(list.Cast<StoreGoodsReturnModel>().ToList(), null, "template"),
							DocType = "storegoodsreturn",
							ApiUrl = inventoryApiUrl
						},
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
							Items = storeInventoryAdjustmentItems,
							GetSid = i => ((StoreInventoryAdjustmentModel)i).AdjSid.ToString(),
							GetDocNo = i => ((StoreInventoryAdjustmentModel)i).SequenceNo.ToString(),
							GetDate = i => ((StoreInventoryAdjustmentModel)i).BusinessDayDate.DateTime,
							XmlGen = list => OutboundStoreInventoryAdjustment.GenerateXml(list.Cast<StoreInventoryAdjustmentModel>().ToList(), null, "template"),
							DocType = "storeinventoryadjustment",
							ApiUrl = inventoryApiUrl
						},
						new OutboundConfig
						{
							Items = storeShippingItems,
							GetSid = i => ((StoreShippingModel)i).VouSid.ToString(),
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
						}
					};

					Logger.Log($"[FETCH] {storeCode} | Sales: {storeSaleItems.Count()} | Shipping: {storeShippingItems.Count()} | Receiving: {storeReceivingItems.Count()} ...");

					var filteredConfigs = outboundConfigs.Where(cfg => selectedDocTypes.Contains(cfg.DocType.ToLowerInvariant()));

					foreach (var cfg in filteredConfigs)
					{

						var itemsList = cfg.Items.ToList();
						if (!itemsList.Any())
						{
							results.Add($"[INFO] No records for {cfg.DocType} in store {storeCode}");
							continue;
						}

						Logger.Log($"[CONFIG] Processing {cfg.DocType} for store {storeCode} | Items: {itemsList.Count}");


						foreach (var item in itemsList)
						{
							try
							{
								string sid = cfg.GetSid(item);
								string docNo = cfg.GetDocNo(item);
								DateTime docDate = cfg.GetDate(item);

								Logger.Log($"[PROCESSING] {cfg.DocType} | Store {storeCode} | DocNo {docNo} | SID {sid} | Date {docDate:yyyy-MM-dd}");

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

								results.Add($"[SUCCESS] {cfg.DocType} | Store {storeCode} | DocNo {docNo} | SID {sid}");
							}
							catch (Exception ex)
							{
								results.Add($"[ERROR] {cfg.DocType} | Store {storeCode} | Exception: {ex.Message}");
								Logger.Log($"[ERROR] {cfg.DocType} | Store {storeCode} | {ex}");
							}
						}
					}
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
				btnSendXml.Enabled = true;
			}

			// ✅ Show all results at once
			MessageBox.Show(
				string.Join(Environment.NewLine, results),
				"Outbound Processing Complete",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information
			);
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
							Logger.Log($"[API SUCCESS] {docType} | SID {sid} | DocNo {docNo} | Status: {response.StatusCode}");
							InsertProcessedTransaction(sid, docType, docDate.ToString("dd-MMM-yy hh:mm:ss tt zzz"), "Success", responseContent);
						}
						else
						{
							Logger.Log($"[API FAILED] {docType} | SID {sid} | DocNo {docNo} | Status: {response.StatusCode} | Reason: {response.ReasonPhrase}");
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

			MessageBox.Show($"{docType.ToUpper()} data processed. Full API responses saved to AppData folder.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
