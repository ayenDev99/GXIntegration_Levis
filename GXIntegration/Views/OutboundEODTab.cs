using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.Model;
using GXIntegration_Levis.OutboundHandlers;
using GXIntegration_Levis.Properties;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static System.Data.Entity.Infrastructure.Design.Executor;



namespace GXIntegration_Levis.Views
{
	public partial class OutboundEODTab : UserControl
	{
		private GunaDataGridView guna1DataGridView1;
		private Dictionary<string, Func<Task>> downloadActions;
		private GXConfig config;
		private readonly OutboundRepositories repositories;

		private DateTimePicker datePickerFrom;
		private DateTimePicker datePickerTo;
		private Label lblFrom;
		private Label lblTo;
		private GunaButton btnSendXml;
		private CheckBox headerCheckBox;

		private Panel loadingOverlay;
		private GunaWinCircleProgressIndicator spinner;
		private Label lblLoadingText;

		public OutboundEODTab(GXConfig config, OutboundRepositories repositories)
		{
			this.config = config;
			this.repositories = repositories;

			InitializeComponent();
			InitializeGrid();
			InitializeControls();
		}

		// ***************************************************
		// Initialization Methods
		// ***************************************************
		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(20, 50),
				Size = new Size(520, 140),
				AllowUserToAddRows = false,
				ScrollBars = ScrollBars.Both,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				GridColor = Color.LightGray,
				Theme = GunaDataGridViewPresetThemes.Guna
			};

			headerCheckBox = new CheckBox
			{
				Size = new Size(15, 15),
				BackColor = Color.Transparent
			};

			// Style
			guna1DataGridView1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
			guna1DataGridView1.ThemeStyle.HeaderStyle.ForeColor = Color.White;
			guna1DataGridView1.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
			guna1DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

			// Add checkbox column
			var checkBoxColumn = new DataGridViewCheckBoxColumn
			{
				Name = "Select",
				HeaderText = "",
				Width = 40,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None
			};
			guna1DataGridView1.Columns.Add(checkBoxColumn);

			Point headerCellLocation = guna1DataGridView1.GetCellDisplayRectangle(0, -1, true).Location;
			headerCheckBox.Location = new Point(headerCellLocation.X + 12, headerCellLocation.Y + 4); // Adjust as needed
			headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;

			guna1DataGridView1.Controls.Add(headerCheckBox);
			guna1DataGridView1.Columns.Add("ID", "ID");
			guna1DataGridView1.Columns.Add("Name", "Name");
			guna1DataGridView1.Columns.Add("FileNameFormat", "File Name Format");
			guna1DataGridView1.Columns.Add("GenerateBy", "Generate by");
			guna1DataGridView1.Columns.Add("Type", "Type");

			guna1DataGridView1.Columns["ID"].Width = 20;
			guna1DataGridView1.Columns["Name"].Width = 170;
			guna1DataGridView1.Columns["FileNameFormat"].Width = 600;
			guna1DataGridView1.Columns["GenerateBy"].Width = 80;
			guna1DataGridView1.Columns["Type"].Width = 50;

			guna1DataGridView1.CellContentClick += CellContentClick;
			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			void AddRow(string id, string name, string format, string generate, string type)
				=> guna1DataGridView1.Rows.Add(false, id, name, format, generate, type, Resources.icon_download);

			AddRow("1", "PRICE", "[Region]_[CountryCode]_PRICING_[DaySequence]_[yyyymmddhhmmss]", "by Market", ".txt");
			AddRow("2", "INVENTORY SNAPSHOTS", "[Region]_[CountryCode]_[StoreCode]_PSSTKR_[DaySequence]_[yyyymmddhhmmss]", "by Store", ".txt");
			AddRow("3", "INTRANSIT", "[Region]_[CountryCode]_INTRANSIT_[DaySequence]_[yyyymmddhhmmss]", "by Market", ".txt");
			AddRow("4", "INVENTORYCOUNT", "[Region]_[CountryCode]_[StoreCode]_INVENTORYCOUNT_[DaySequence]_[yyyymmddhhmmss]", "by Store", ".xml");
			AddRow("5", "POSLOG", "[Region]_[CountryCode]_[StoreCode]_POSLOG_[DaySequence]_[yyyymmddhhmmss]", "by Store", ".xml");

			this.Controls.Add(guna1DataGridView1);
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
				CustomFormat = "yyyy-MM-dd",
				Width = 160,
				ShowUpDown = false,
				Value = DateTime.Today
			};

			lblTo = new Label
			{
				Text = "To:",
				Location = new Point(250, 24),
				AutoSize = true
			};

			datePickerTo = new DateTimePicker
			{
				Location = new Point(290, 20),
				Format = DateTimePickerFormat.Custom,
				CustomFormat = "yyyy-MM-dd",
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
				text: "Download All and Send to SFTP",
				location: new Point(20, 250),
				clickAction: async () => await ManualProcess()
			);

			this.Controls.Add(btnSendXml);
		}

		public async Task TriggerEODAsync(string processTime)
		{
			await AutoProcess(processTime);
		}

		// ***************************************************
		// Process Methods
		// ***************************************************
		private async Task AutoProcess(string processTime)
		{
			bool isAuto = true;
			Logger.LogOutbound($"[AUTO PROCESS] Scheduled Time: {processTime}");

			try
			{
				if (!TimeSpan.TryParse(processTime, out var toTime))
				{
					Logger.LogOutbound($"ERROR: Invalid processTime value '{processTime}'. Expected format HH:mm:ss");
					return;
				}

				DateTime today = DateTime.Today;
				DateTime fromDate = today;
				DateTime toDate = today.Add(toTime);

				Logger.LogOutbound($"[OUTBOUND EOD-AUTO] Processing Date Range From: {fromDate:yyyy-MM-dd HH:mm:ss} To: {toDate:yyyy-MM-dd HH:mm:ss}");

				var prismStores = await repositories.PrismRepository.GetRpsStore("ACTIVE", "1");

				await OutboundPrice.Execute(repositories.PriceRepository, config, fromDate);
				await OutboundInventorySnapshots.Execute(repositories.InventoryRepository, config, prismStores, fromDate);
				await OutboundInTransit.Execute(repositories.InTransitRepository, config, fromDate);
				await ExecuteStoreInventoryCountAsync(prismStores, fromDate, toDate, isAuto);
				await ExecuteAllAndSaveToSingleXmlAsync(prismStores, fromDate, toDate);

				Logger.LogOutbound("[OUTBOUND EOD-AUTO] [SFTP] Start Uploading generated files to SFTP...");
				await UploadToSftpAsync();

				Logger.LogOutbound("[OUTBOUND EOD-AUTO] Process completed successfully.");
			}
			catch (Exception ex)
			{
				Logger.LogError($"ERROR during AutoProcess: {ex}");
			}
		}

		private async Task ManualProcess()
		{
			Logger.LogOutbound("[OUTBOUND EOD-MANUAL] Start Manual Process...");
			bool isAuto = false;
			guna1DataGridView1.EndEdit();

			var selectedDocTypes = guna1DataGridView1.Rows
				.Cast<DataGridViewRow>()
				.Where(r => r.Cells["Select"].Value is bool b && b)
				.Select(r =>
				{
					var name = r.Cells["Name"].Value?.ToString();
					return string.IsNullOrWhiteSpace(name)
						? null
						: _docTypeMap.TryGetValue(name, out var mapped)
							? mapped
							: name;
				})
				.Where(s => !string.IsNullOrEmpty(s))
				.ToHashSet();

			Logger.LogOutbound("[OUTBOUND EOD-MANUAL] Selected Transaction Types: " + string.Join(",", selectedDocTypes));

			if (!selectedDocTypes.Any())
			{
				MessageBox.Show("Please select at least one transaction.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var fromDate = datePickerFrom.Value.Date;
			var toDate = datePickerTo.Value.Date;

			if (fromDate > toDate)
			{
				MessageBox.Show("From Date cannot be later than To Date.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			btnSendXml.Enabled = false;
			btnSendXml.Text = "Processing...";
			btnSendXml.BaseColor = Color.Gray; // optional, to indicate disabled
			btnSendXml.Refresh();
			Cursor.Current = Cursors.WaitCursor;

			try
			{
				var prismStores = await repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
				Logger.LogOutbound($"[OUTBOUND EOD-MANUAL] Processing Date Range From: {fromDate:yyyy-MM-dd} To: {toDate:yyyy-MM-dd}");

				var processActions = new Dictionary<string, Func<DateTime, Task>>(StringComparer.OrdinalIgnoreCase)
				{
					["PRICE"] = async (date) => await OutboundPrice.Execute(repositories.PriceRepository, config, date),
					["INVENTORY SNAPSHOTS"] = async (date) => await OutboundInventorySnapshots.Execute(repositories.InventoryRepository, config, prismStores, date),
					["INTRANSIT"] = async (date) => await OutboundInTransit.Execute(repositories.InTransitRepository, config, date),
					["INVENTORYCOUNT"] = async (date) => await ExecuteStoreInventoryCountAsync(prismStores, date, date, isAuto),
					["POSLOG"] = async (date) => await ExecuteAllAndSaveToSingleXmlAsync(prismStores, date, date)
				};

				for (var date = fromDate; date <= toDate; date = date.AddDays(1))
				{
					Logger.LogOutbound($"[OUTBOUND EOD] Processing date: {date:yyyy-MM-dd}");

					foreach (var docType in selectedDocTypes)
					{
						if (processActions.TryGetValue(docType, out var action))
						{
							Logger.LogOutbound($"[OUTBOUND EOD] Executing {docType} for {date:yyyy-MM-dd}...");
							var sw = System.Diagnostics.Stopwatch.StartNew();
							await action(date);
							sw.Stop();
							Logger.LogOutbound($"[OUTBOUND EOD] Finished {docType} for {date:yyyy-MM-dd} in {sw.ElapsedMilliseconds} ms");
						}
						else
						{
							Logger.LogOutbound($"[OUTBOUND EOD] No action defined for {docType}");
						}
					}
				}

				Logger.LogOutbound("[OUTBOUND EOD] [SFTP] Start Uploading generated files to SFTP...");
				await UploadToSftpAsync();

				MessageBox.Show("OUTBOUND EOD Processed Successfully.");
			}
			finally
			{
				btnSendXml.Enabled = true;
				btnSendXml.Text = "Download All and Send to SFTP";
				btnSendXml.BaseColor = Color.FromArgb(100, 88, 255); // restore original color
				Cursor.Current = Cursors.Default;
			}
		}

		private async Task ExecuteAllAndSaveToSingleXmlAsync(dynamic prismStores, DateTime fromDate, DateTime toDate)
		{
			string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
			string baseArchiveDir = Path.Combine(outboundDir, "ARCHIVE");

			Directory.CreateDirectory(outboundDir);
			Directory.CreateDirectory(baseArchiveDir);

			string countryCode = config.CountryCode ?? "XX";

			Logger.LogOutbound("[OUTBOUND EOD] [XML] Start Generating POSLOG per day...");

			for (var currentDate = fromDate.Date; currentDate <= toDate.Date; currentDate = currentDate.AddDays(1))
			{
				string dateStr = currentDate.ToString("yyyyMMdd");
				string todayPrefix = currentDate.ToString("ddMMyyyy");

				string archiveDir = Path.Combine(baseArchiveDir, dateStr);
				Directory.CreateDirectory(archiveDir);

				Logger.LogOutbound($"[OUTBOUND EOD] Processing date: {dateStr}");

				foreach (var store in prismStores)
				{
					string storeCode = ((IDictionary<string, object>)store).TryGetValue("ADDRESS4", out var addr)
						? addr?.ToString()
						: "N/A";

					Logger.LogOutbound($"[OUTBOUND EOD] [XML] STORE_CODE: {storeCode} | DATE: {dateStr}");

					try
					{
						// Fetch data for that store + specific date
						var storeSaleItems = await repositories.StoreSaleRepository.GetStoreSaleAsync(currentDate, currentDate, storeCode, "EOD");
						var storeReturnItems = await repositories.StoreReturnRepository.GetStoreReturnAsync(currentDate, currentDate, storeCode, "EOD");
						var storeGoodsItems = await repositories.StoreGoodsRepository.GetStoreGoodsAsync(currentDate, currentDate, storeCode, "EOD");
						var storeGoodsReturnItems = await repositories.StoreGoodsReturnRepository.GetStoreGoodsReturnAsync(currentDate, currentDate, storeCode, "EOD");
						var storeShippingItems = await repositories.StoreShippingRepository.GetStoreShippingAsync(currentDate, currentDate, storeCode, "EOD");
						var storeReceivingItems = await repositories.StoreReceivingRepository.GetStoreReceivingAsync(currentDate, currentDate, storeCode, "EOD");
						var storeInventoryAdjustmentItems = await repositories.StoreInventoryAdjustmentRepository.GetStoreInventoryAdjustmentAsync(currentDate, currentDate, storeCode, "EOD");

						var xmlFragments = new[]
						{
							OutboundStoreSale.GenerateXml(storeSaleItems, null, "template"),
							OutboundStoreReturn.GenerateXml(storeReturnItems, null, "template"),
							OutboundStoreGoods.GenerateXml(storeGoodsItems, null, "template"),
							OutboundStoreGoodsReturn.GenerateXml(storeGoodsReturnItems, null, "template"),
							OutboundStoreShipping.GenerateXml(storeShippingItems, null, "template"),
							OutboundStoreReceiving.GenerateXml(storeReceivingItems, null, "template"),
							OutboundStoreInventoryAdjustment.GenerateXml(storeInventoryAdjustmentItems, null, "template"),
						};

						string[] xmlTypes = { "StoreSale", "StoreReturn", "StoreGoods", "StoreGoodsReturn", "StoreShipping", "StoreReceiving", "StoreInventoryAdjustment" };

						var dataModules = new List<(string Label, IEnumerable<object> Items)>
						{
							("StoreSale", storeSaleItems as IEnumerable<object>),
							("StoreReturn", storeReturnItems as IEnumerable<object>),
							("StoreGoods", storeGoodsItems as IEnumerable<object>),
							("StoreGoodsReturn", storeGoodsReturnItems as IEnumerable<object>),
							("StoreShipping", storeShippingItems as IEnumerable<object>),
							("StoreReceiving", storeReceivingItems as IEnumerable<object>),
							("StoreInventoryAdjustment", storeInventoryAdjustmentItems as IEnumerable<object>)
						};

						List<XElement> validFragments = new List<XElement>();
						var countsByType = dataModules.ToDictionary(dm => dm.Label, dm => dm.Items?.Count() ?? 0);

						for (int i = 0; i < xmlFragments.Length; i++)
						{
							string fragment = xmlFragments[i];
							string xmlType = xmlTypes[i];

							if (!string.IsNullOrWhiteSpace(fragment))
							{
								string cleaned = fragment
									.Replace("<Transactions xmlns=\"\">", "")
									.Replace("</Transactions>", "")
									.Trim();
								try
								{
									var parsedFragment = XElement.Parse("<Root>" + cleaned + "</Root>");

									foreach (var child in parsedFragment.Elements())
									{
										validFragments.Add(child);
									}

									int count = countsByType[xmlType];
									Logger.LogOutbound($"[OUTBOUND EOD] [XML] Successfully generated {xmlType} XML for {storeCode} | Date: {dateStr} | Count: {count}");
								}
								catch (Exception ex)
								{
									Logger.LogOutbound($"[OUTBOUND EOD] [XML] Failed to parse {xmlType} XML for store {storeCode}: {ex.Message}");
								}
							}
							else
							{
								Logger.LogOutbound($"[OUTBOUND EOD] [XML] No {xmlType} data for store {storeCode} | Date: {dateStr}");
							}
						}

						if (!validFragments.Any())
						{
							Logger.LogOutbound($"[OUTBOUND EOD] [XML] No POSLOG data for store {storeCode} | Date: {dateStr}. Skipping file creation.");
							continue;
						}

						var settings = new XmlWriterSettings
						{
							Indent = true,
							Encoding = Encoding.UTF8,
							OmitXmlDeclaration = false,
							Async = true
						};

						var existingFiles = Directory.GetFiles(archiveDir, $"AMA_{countryCode}_{storeCode}_POSLOG_*.xml")
							.Where(f => Path.GetFileName(f).Contains(todayPrefix))
							.ToList();

						int nextSequence = existingFiles.Count + 1;
						string sequenceStr = nextSequence.ToString("D2");
						string timestamp = currentDate.ToString("ddMMyyyyHHmmss");

						string fileName = $"AMA_{countryCode}_{storeCode}_POSLOG_{sequenceStr}_{timestamp}.xml";
						string filePath = Path.Combine(outboundDir, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
						using (var writer = XmlWriter.Create(stream, settings))
						{
							await writer.WriteStartDocumentAsync();
							writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
							writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
							writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
							writer.WriteAttributeString("schemaLocation", GlobalOutbound.NsIXRetail + "POSLog.xsd");

							foreach (var fragment in validFragments)
							{
								using (var reader = fragment.CreateReader())
								{
									await writer.WriteNodeAsync(reader, true);
								}
							}

							writer.WriteEndElement(); // POSLog
							await writer.WriteEndDocumentAsync();
							await writer.FlushAsync();
						}

						Logger.LogOutbound($"[OUTBOUND EOD] [XML] File generated successfully | {fileName}");
					}
					catch (Exception ex)
					{
						Logger.LogError($"[ERROR] Failed to process store {storeCode} on {dateStr}: {ex}");
					}
				}
			}

			Logger.LogOutbound("[OUTBOUND EOD] [XML] Completed all dates.");
		}

		private async Task ExecuteStoreInventoryCountAsync(dynamic prismStores, DateTime fromDate, DateTime toDate, bool isAuto)
		{
			string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
			string baseArchiveDir = Path.Combine(outboundDir, "ARCHIVE");

			Directory.CreateDirectory(outboundDir);
			Directory.CreateDirectory(baseArchiveDir);

			Logger.LogOutbound($"[OUTBOUND EOD] [XML] Start Generating INVENTORYCOUNT per day...");

			for (var currentDate = fromDate.Date; currentDate <= toDate.Date; currentDate = currentDate.AddDays(1))
			{
				string dateStr = currentDate.ToString("yyyyMMdd");
				string todayPrefix = currentDate.ToString("ddMMyyyy");

				string archiveDir = Path.Combine(baseArchiveDir, dateStr);
				Directory.CreateDirectory(archiveDir);

				Logger.LogOutbound($"[OUTBOUND EOD] Processing date: {dateStr}");

				foreach (var store in prismStores)
				{
					if (store is IDictionary<string, object> dict && dict.TryGetValue("ADDRESS4", out var addr) && addr != null)
					{
						string storeCode = addr.ToString();

						try
						{
							var repo = new StoreInventoryCountRepository(config.MainDbConnection);

							int pageSize = 100000;
							int startRow = 1;
							int totalFetched = 0;
							var allItems = new List<StoreInventoryCountModel>();

							while (true)
							{
								var batch = await repo.GetPagedStoreInventoryCountAsync(
									currentDate, currentDate, storeCode, startRow, startRow + pageSize - 1);

								if (batch.Count == 0)
									break;

								allItems.AddRange(batch);
								totalFetched += batch.Count;

								Logger.LogOutbound($"[OUTBOUND EOD] [XML] Fetched batch of {batch.Count} (Total: {totalFetched}) for StoreCode {storeCode}");

								startRow += pageSize;
							}

							if (!allItems.Any())
							{
								Logger.LogOutbound($"[OUTBOUND EOD] [XML] No INVENTORYCOUNT data generated for store {storeCode}. File will not be created.");
								continue;
							}

							await OutboundStoreInventoryCount.Execute(currentDate, allItems, config, "xml", storeCode, isAuto);
						}
						catch (Exception ex)
						{
							Logger.LogError($"[OUTBOUND EOD] Failed for StoreCode: {storeCode} | Exception: {ex.Message}");
						}
					}
					else
					{
						Logger.LogOutbound("[OUTBOUND EOD] Store skipped due to missing ADDRESS4 field");
					}
				}
			}
		}

		private async Task UploadToSftpAsync()
		{
			var sftpConfig = GlobalHelper.LoadSftpConnection();

			if (!sftpConfig.TryGetValue("Host", out string host) ||
				!sftpConfig.TryGetValue("Port", out string port) ||
				!sftpConfig.TryGetValue("Username", out string username) ||
				!sftpConfig.TryGetValue("Password", out string password))
			{
				MessageBox.Show("[ERROR] SFTP configuration is missing. Please navigate to the 'Configuration SFTP' tab to set up the SFTP connection.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string localDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");

			var directoryMap = GlobalHelper.LoadPathMap("OutSFTPPath");

			await Task.Run(() =>
			{
				try
				{
					if (!Directory.Exists(localDirectory))
					{
						Logger.LogOutbound($"[OUTBOUND EOD] [SFTP] Local directory does not exist: {localDirectory}");
						return;
					}

					var files = Directory.GetFiles(localDirectory, "*.*")
						.Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
									f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
						.ToArray();

					if (!files.Any())
					{
						Logger.LogOutbound("[OUTBOUND EOD] [SFTP] No outbound files found to upload.");
						return;
					}

					int portNumber = Convert.ToInt32(port);
					using (var sftp = new SftpClient(host, portNumber, username, password))
					{
						sftp.Connect();

						foreach (var filePath in files)
						{
							try
							{
								string fileName = Path.GetFileName(filePath);

								// Pick the correct remote directory based on filename
								string remoteDirectory = GetRemoteDirectory(fileName, directoryMap);

								if (!sftp.Exists(remoteDirectory))
									sftp.CreateDirectory(remoteDirectory);

								string remotePath = remoteDirectory + fileName;

								using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
								{
									sftp.UploadFile(fileStream, remotePath, true);
								}

								ArchiveFile(filePath, localDirectory);

								Logger.LogOutbound($"[OUTBOUND EOD] [SFTP] Uploaded '{fileName}' → {remoteDirectory} and archived.");
							}
							catch (Exception ex)
							{
								Logger.LogOutbound($"Error handling file '{Path.GetFileName(filePath)}': {ex}");
							}
						}

						sftp.Disconnect();
					}

					Logger.LogOutbound("[OUTBOUND EOD] [SFTP] Upload to SFTP process completed.");
				}
				catch (Exception ex)
				{
					Logger.LogError($"SFTP Upload failed: {ex}");
				}
			});
		}

		private void ArchiveFile(string filePath, string localDirectory)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
				{
					Logger.LogOutbound($"[ARCHIVE] File not found: {filePath}");
					return;
				}

				string fileName = Path.GetFileName(filePath);

				var match = Regex.Match(fileName, @"_(\d{8})\d{6}\.(xml|txt)$", RegexOptions.IgnoreCase);
				if (!match.Success)
				{
					Logger.LogOutbound($"[ARCHIVE] Invalid date format in filename: {fileName}");
					return;
				}

				string datePart = match.Groups[1].Value; // "08102025"
				if (!DateTime.TryParseExact(datePart, "ddMMyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
				{
					Logger.LogOutbound($"[ARCHIVE] Failed to parse date from filename: {fileName}");
					return;
				}

				string formattedDate = parsedDate.ToString("yyyyMMdd");
				string archiveRootDir = Path.Combine(localDirectory, "ARCHIVE");
				string archiveDateDir = Path.Combine(archiveRootDir, formattedDate);

				Directory.CreateDirectory(archiveDateDir);

				string archivedPath = Path.Combine(archiveDateDir, fileName);

				if (File.Exists(archivedPath))
				{
					Logger.LogOutbound($"[ARCHIVE] Overwriting existing file: {archivedPath}");
					File.Delete(archivedPath);
				}

				File.Move(filePath, archivedPath);
				Logger.LogOutbound($"[ARCHIVE] Moved file to: {archivedPath}");
			}
			catch (Exception ex)
			{
				Logger.LogError($"[ARCHIVE] Error archiving file: {ex.Message}");
			}
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


		// ***************************************************
		// Handlers/Helpers
		// ***************************************************
		public async void CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == guna1DataGridView1.Columns["Select"].Index && e.RowIndex >= 0)
			{
				// Toggle the checkbox value
				DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)guna1DataGridView1.Rows[e.RowIndex].Cells["Select"];
				chk.Value = !(chk.Value != null && (bool)chk.Value);

				// Sync header checkbox
				bool allChecked = guna1DataGridView1.Rows.Cast<DataGridViewRow>()
					.All(r => Convert.ToBoolean(r.Cells["Select"].Value));

				headerCheckBox.CheckedChanged -= HeaderCheckBox_CheckedChanged;
				headerCheckBox.Checked = allChecked;
				headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;
			}

			await GlobalHelper.HandleDownloadClick(
				guna1DataGridView1, downloadActions, e.RowIndex, e.ColumnIndex, "Action"
			);
		}

		private void CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
		{
			GlobalHelper.HandleCellMouseMove(guna1DataGridView1, e);
		}

		private void CellMouseLeave(object sender, DataGridViewCellEventArgs e)
		{
			GlobalHelper.HandleCellMouseLeave(guna1DataGridView1);
		}

		private string GetRemoteDirectory(string fileName, Dictionary<string, string> directoryMap)
		{
			foreach (var kvp in directoryMap)
			{
				if (kvp.Key != "DEFAULT" &&
					fileName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return kvp.Value;
				}
			}

			return directoryMap.ContainsKey("DEFAULT") ? directoryMap["DEFAULT"] : "/IN/OTHERS/";
		}

		private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			bool isChecked = ((CheckBox)sender).Checked;

			foreach (DataGridViewRow row in guna1DataGridView1.Rows)
			{
				DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells["Select"];
				chk.Value = isChecked;
			}

			guna1DataGridView1.RefreshEdit();
		}

	}
}
