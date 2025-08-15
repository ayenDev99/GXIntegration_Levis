using Guna.UI.WinForms;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.OutboundHandlers;
using GXIntegration_Levis.Properties;
using GXIntegration.Properties;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace GXIntegration_Levis
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

		private GunaDataGridView guna1DataGridView1;
		private GunaButton processAllButton;
		private TabControl tabControl;
		private TabPage tabText, tabXml, tabApi;
		private GunaButton processApiButton;


		private int _hoveredRowIndex = -1;
		private Dictionary<string, Func<Task>> downloadActions;

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

			initialCreateDatabase();
			InitializeComponent();
			InitializeTabs();
			InitializeDownloadActions();
		}

		private void initialCreateDatabase()
		{
			string dbPath = "MyDatabase.sqlite";

			Logger.Log($"TEST");

			// Create the database file if it doesn't exist
			if (!File.Exists(dbPath))
			{
				SQLiteConnection.CreateFile(dbPath);
				Console.WriteLine("Database created successfully.");
			}

			// Create a connection string
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
				Location = new Point(220, 10),
				Size = new Size(850, 450),
				Font = new Font("Segoe UI", 9)
			};

			tabText = new TabPage("TEXT");
			tabXml = new TabPage("XML");
			tabApi = new TabPage("API");

			tabControl.TabPages.Add(tabText);
			tabControl.TabPages.Add(tabXml);
			tabControl.TabPages.Add(tabApi);

			this.Controls.Add(tabControl);

			InitializeGrid();
			InitializeProcessAllButton();
			InitializeApiProcessButton();
		}

		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(20, 20),
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

			guna1DataGridView1.ColumnCount = 4;
			guna1DataGridView1.Columns[0].Name = "ID";
			guna1DataGridView1.Columns[1].Name = "Name";
			guna1DataGridView1.Columns[2].Name = "File Name Format";
			guna1DataGridView1.Columns[3].Name = "File Type";

			guna1DataGridView1.Columns[0].Width = 30;
			guna1DataGridView1.Columns[1].Width = 200;
			guna1DataGridView1.Columns[2].Width = 455;
			guna1DataGridView1.Columns[3].Width = 70;

			var imageColumn = new DataGridViewImageColumn
			{
				Name = "Action",
				HeaderText = "Action",
				Image = Resources.icon_download,
				Width = 50,
				ImageLayout = DataGridViewImageCellLayout.Zoom
			};
			guna1DataGridView1.Columns.Add(imageColumn);

			guna1DataGridView1.CellContentClick += Guna1DataGridView1_CellContentClick;
			guna1DataGridView1.CellMouseMove += Guna1DataGridView1_CellMouseMove;
			guna1DataGridView1.CellMouseLeave += Guna1DataGridView1_CellMouseLeave;

			AddRow("1", "ASN - RECEIVING", "StoreGoods_[yyyymmddhhmmss]", ".xml");
			AddRow("2", "RETURN_TO_DC", "StoreGoodsReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("3", "RETAIL_SALE", "StoreSale_[yyyymmddhhmmss]", ".xml");
			AddRow("4", "RETURN_SALE", "StoreReturn_[yyyymmddhhmmss]", ".xml");
			AddRow("5", "ADJUSTMENT", "StoreInventoryAdjustment_[yyyymmddhhmmss]", ".xml");
			AddRow("6", "STORE_TRANSFER - SHIPPING", "StoreShipping_[yyyymmddhhmmss]", ".xml");
			AddRow("7", "STORE_TRANSFER - RECEIVING", "StoreReceiving_[yyyymmddhhmmss]", ".xml");
			AddRow("8", "INVENTORY SNAPSHOTS", "LS[Country code]_AMA_PSSTKR_[yyyymmddhhmmss]", ".txt");
			AddRow("9", "INTRANSIT", "LS[Country Code]_[REGION Code]_INTRANSIT_[yyyymmddhhmmss]", ".txt");
			AddRow("10", "PRICE", "[REGION Code]_[Country code]_PRICING_[yyyymmddhhmmss]", ".txt");

			tabText.Controls.Add(guna1DataGridView1);
		}

		private void InitializeProcessAllButton()
		{
			processAllButton = new GunaButton
			{
				Text = "Process All",
				Location = new Point(20, 340),
				Size = new Size(150, 40),
				BaseColor = Color.FromArgb(100, 88, 255),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 9F, FontStyle.Bold),
				OnHoverBaseColor = Color.FromArgb(72, 61, 255),
				Cursor = Cursors.Hand
			};

			processAllButton.Click += async (s, e) => await ProcessAllDownloads();

			tabText.Controls.Add(processAllButton);
		}

		private void AddRow(string id, string name, string format, string type)
		{
			guna1DataGridView1.Rows.Add(id, name, format, type);
		}

		private void InitializeDownloadActions()
		{
			downloadActions = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
			{
				["ASN - RECEIVING"] = () => OutboundASN.Execute(_asnRepository, config),
				["RETURN_TO_DC"] = () => OutboundStoreGoodsReturn.Execute(_storeGoodsReturnRepository, config),
				["RETAIL_SALE"] = () => OutboundStoreSale.Execute(_storeSaleRepository, config, "xml"),
				["RETURN_SALE"] = () => OutboundStoreReturn.Execute(_storeReturnRepository, config),
				["ADJUSTMENT"] = () => OutboundStoreInventoryAdjustment.Execute(_storeInventoryAdjustmentRepository, config),
				["STORE_TRANSFER - SHIPPING"] = () => OutboundStoreShipping.Execute(_storeShippingRepository, config),
				["STORE_TRANSFER - RECEIVING"] = () => OutboundStoreReceiving.Execute(_storeReceivingRepository, config),
				["INVENTORY SNAPSHOTS"] = () => OutboundInventorySnapshots.Execute(_inventoryRepository, config),
				["INTRANSIT"] = () => OutboundInTransit.Execute(_inTransitRepository, config),
				["PRICE"] = () => OutboundPrice.Execute(_priceRepository, config)
			};
		}

		// Include your existing Guna1 event handlers like Guna1DataGridView1_CellContentClick, etc.

		private async Task ProcessAllDownloads()
		{
			processAllButton.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;

			try
			{
				foreach (var action in downloadActions)
				{
					try
					{
						await action.Value.Invoke();
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Failed to process {action.Key}:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}

				MessageBox.Show("All downloads processed. Starting SFTP upload...", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				await UploadToSftpAsync();
			}
			finally
			{
				processAllButton.Enabled = true;
				Cursor.Current = Cursors.Default;
			}
		}

		private async Task UploadToSftpAsync()
		{
			await Task.Run(() =>
			{
				string host = "levib2bstage.levi.com";
				int port = 49153;
				string username = "TestRetailPro";
				string password = "X67zZkTTAkIC";
				string remoteDirectory = "/IN/";
				string localDirectory = @"C:\GXIntegration_Levis\Modern Sliding Sidebar - C-Sharp Winform\bin\Debug\OUTBOUND\";

				try
				{
					using (var sftp = new SftpClient(host, port, username, password))
					{
						sftp.Connect();

						if (!sftp.Exists(remoteDirectory))
							sftp.CreateDirectory(remoteDirectory);

						if (!Directory.Exists(localDirectory))
						{
							MessageBox.Show($"Local directory does not exist:\n{localDirectory}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						var files = Directory.GetFiles(localDirectory)
							.Where(f => f.EndsWith(".txt") || f.EndsWith(".xml"))
							.ToArray();

						if (files.Length == 0)
						{
							MessageBox.Show("No files to upload.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}

						foreach (var filePath in files)
						{
							try
							{
								using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
								{
									string remotePath = remoteDirectory + Path.GetFileName(filePath);
									sftp.UploadFile(fileStream, remotePath, true);
								}
							}
							catch (Exception ex)
							{
								MessageBox.Show($"Failed to upload {Path.GetFileName(filePath)}:\n{ex.Message}", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							}
						}

						sftp.Disconnect();
						MessageBox.Show("Upload to SFTP completed successfully.", "SFTP Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"SFTP Upload failed:\n{ex.Message}", "SFTP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			});
		}

		private async void Guna1DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
{
	if (e.RowIndex < 0 || guna1DataGridView1.Columns[e.ColumnIndex].Name != "Action")
		return;

	string name = guna1DataGridView1.Rows[e.RowIndex].Cells["Name"].Value?.ToString();

	if (downloadActions.TryGetValue(name, out var handler))
	{
		try
		{
			await handler();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error executing action for {name}:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
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
					guna1DataGridView1.Rows[_hoveredRowIndex].DefaultCellStyle.BackColor = Color.White;

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

		private void InitializeApiProcessButton()
		{
			processApiButton = new GunaButton
			{
				Text = "Send XML to API",
				Location = new Point(20, 20),
				Size = new Size(200, 40),
				BaseColor = Color.FromArgb(100, 88, 255),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 9F, FontStyle.Bold),
				OnHoverBaseColor = Color.FromArgb(72, 61, 255),
				Cursor = Cursors.Hand
			};

			processApiButton.Click += async (s, e) => await SendXmlFilesToApi();

			tabApi.Controls.Add(processApiButton);
		}

		private async Task SendXmlFilesToApi()
		{
			string apiUrl = "https://mule-rtf-test.levi.com/retail-pos-ph-rpp-exp-api-dev1/retail-pos-ph-rpp-exp-api/v1/sale";
			string username = "1d75a7f3-1b67-4c6e-9c6e-d0f6ba114417";
			string password = "3~E8Q~CKgCliOmXmKjSVXJtrffHYED4_cKDPhax4";

			string xmlTemplate = await OutboundStoreSale.ExecuteAPI(_storeSaleRepository, config, "template");

			Logger.Log($"TEMPLATE : {xmlTemplate}");

			using (var client = new System.Net.Http.HttpClient())
			{
				var byteArray = System.Text.Encoding.UTF8.GetBytes($"{username}:{password}");
				client.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

				try
				{
					var soapEnvelope = $@"<?xml version=""1.0"" ?>
						<S:Envelope xmlns:S=""http://schemas.xmlsoap.org/soap/envelope/"">
						  <S:Body>
							<ns2:postTransaction xmlns:ns2=""http://v1.ws.poslog.xcenter.dtv/"">
							  <rawPoslogString>{System.Security.SecurityElement.Escape(xmlTemplate)}</rawPoslogString>
							</ns2:postTransaction>
						  </S:Body>
						</S:Envelope>";

					var content = new System.Net.Http.StringContent(soapEnvelope, System.Text.Encoding.UTF8, "application/xml");
					var response = await client.PostAsync(apiUrl, content);
					var result = await response.Content.ReadAsStringAsync();

					if (response.IsSuccessStatusCode)
					{
						Logger.Log($"[API POST] SUCCESS: Template sent | Status: {response.StatusCode}");
						MessageBox.Show("Template XML sent successfully.", "API Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						Logger.Log($"[API POST] FAILURE: Template send failed | Status: {response.StatusCode} | Reason: {response.ReasonPhrase}");
						MessageBox.Show($"Template send failed:\n{result}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
				catch (Exception ex)
				{
					Logger.Log($"[API POST] ERROR while sending template\nException: {ex.GetType().Name} | Message: {ex.Message}\nStackTrace: {ex.StackTrace}");
					MessageBox.Show($"Error sending template:\n{ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}



	}
}
