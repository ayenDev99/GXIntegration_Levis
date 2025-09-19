using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.OutboundHandlers;
using GXIntegration_Levis.Properties;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;


namespace GXIntegration_Levis.Views
{
	public partial class OutboundEODTab : UserControl
	{
		private GunaDataGridView guna1DataGridView1;
		private GunaButton btnSendXml;
		private Dictionary<string, Func<Task>> downloadActions;
		private GXConfig config;
		private readonly OutboundRepositories repositories;

		public OutboundEODTab(GXConfig config, OutboundRepositories repositories)
		{
			this.config = config;
			this.repositories = repositories;

			InitializeComponent();
			InitializeDownloadActions();
			InitializeGrid();
			InitializeProcessAllButton();
		}

		// ***************************************************
		// Initialization Methods
		// ***************************************************
		public async Task TriggerDownloadAsync()
		{
			await ProcessAllDownloads();
		}

		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(20, 20),
				Size = new Size(704, 140),
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

			guna1DataGridView1.ColumnCount = 5;
			guna1DataGridView1.Columns[0].Name = "ID";
			guna1DataGridView1.Columns[1].Name = "Name";
			guna1DataGridView1.Columns[2].Name = "File Name Format";
			guna1DataGridView1.Columns[3].Name = "Generate by";
			guna1DataGridView1.Columns[4].Name = "Type";

			guna1DataGridView1.Columns[0].Width = 20;
			guna1DataGridView1.Columns[1].Width = 170;
			guna1DataGridView1.Columns[2].Width = 600;
			guna1DataGridView1.Columns[3].Width = 80;
			guna1DataGridView1.Columns[4].Width = 50;

			var imageColumn = new DataGridViewImageColumn
			{
				Name = "Action",
				HeaderText = "Action",
				Image = Resources.icon_download,
				Width = 50,
				ImageLayout = DataGridViewImageCellLayout.Zoom
			};

			guna1DataGridView1.CellContentClick += CellContentClick;
			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			void AddRow(string id, string name, string format, string generate, string type)
				=> guna1DataGridView1.Rows.Add(id, name, format, generate, type);

			AddRow("1", "PRICE", "[Region]_[CountryCode]_PRICING_[DaySequence]_[yyyymmddhhmmss]", "by Market", ".txt");
			AddRow("2", "INVENTORY SNAPSHOTS", "[Region]_[CountryCode]_[StoreCode]_PSSTKR_[DaySequence]_[yyyymmddhhmmss]", "by Store", ".txt");
			AddRow("3", "INTRANSIT", "[Region]_[CountryCode]_INTRANSIT_[DaySequence]_[yyyymmddhhmmss]", "by Market", ".txt");
			AddRow("4", "INVENTORYCOUNT", "[Region]_[CountryCode]_[StoreCode]_INVENTORYCOUNT_[DaySequence]_[yyyymmddhhmmss]", "by Store", ".xml");
			AddRow("5", "POSLOG", "[Region]_[CountryCode]_[StoreCode]_POSLOG_[DaySequence]_[yyyymmddhhmmss]", "by Store", ".xml");

			this.Controls.Add(guna1DataGridView1);
		}

		private void InitializeProcessAllButton()
		{
			btnSendXml = GlobalHelper.CreateButton(
				text: "Download All and Send to SFTP",
				location: new Point(20, 300),
				clickAction: async () => await ProcessAllDownloads()
			);

			this.Controls.Add(btnSendXml);
		}

		private void InitializeDownloadActions()
		{
			downloadActions = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
			{
				["ASN - RECEIVING"] = () => OutboundStoreGoods.Execute(repositories.StoreGoodsRepository, config, "xml"),
				["RETURN_TO_DC"] = () => OutboundStoreGoodsReturn.Execute(repositories.StoreGoodsReturnRepository, config, "xml"),
				["RETAIL_SALE"] = () => OutboundStoreSale.Execute(repositories.StoreSaleRepository, config, "xml"),
				["RETURN_SALE"] = () => OutboundStoreReturn.Execute(repositories.StoreReturnRepository, config, "xml"),
				["ADJUSTMENT"] = () => OutboundStoreInventoryAdjustment.Execute(repositories.StoreInventoryAdjustmentRepository, config, "xml"),
				["STORE_TRANSFER - SHIPPING"] = () => OutboundStoreShipping.Execute(repositories.StoreShippingRepository, config, "xml"),
				["STORE_TRANSFER - RECEIVING"] = () => OutboundStoreReceiving.Execute(repositories.StoreReceivingRepository, config, "xml"),
				["INVENTORY_COUNT"] = () => OutboundStoreReceiving.Execute(repositories.StoreReceivingRepository, config, "xml"),
				["INVENTORY SNAPSHOTS"] = () => OutboundInventorySnapshots.Execute(repositories.InventoryRepository, config),
				["INTRANSIT"] = () => OutboundInTransit.Execute(repositories.InTransitRepository, config),
				["PRICE"] = () => OutboundPrice.Execute(repositories.PriceRepository, config)
			};
		}

		// ***************************************************
		// Process Methods
		// ***************************************************
		private async Task ProcessAllDownloads()
		{
			Logger.Log($"--------------------------------------------------------------------------");

			btnSendXml.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;

			try
			{
				//Logger.Log("[OUTBOUND - EOD] [TXT] Start Downloading files on local dir...");
				//await OutboundPrice.Execute(repositories.PriceRepository, config);
				//await OutboundInventorySnapshots.Execute(repositories.InventoryRepository, config);
				//await OutboundInTransit.Execute(repositories.InTransitRepository, config);
				//Logger.Log("[OUTBOUND - EOD] [TXT] Download process completed.");

				Logger.Log("[OUTBOUND - EOD] [XML] Starting Downloading files on local dir...");
				//await ExecuteStoreInventoryCountAsync();
				await ExecuteAllAndSaveToSingleXmlAsync();
				Logger.Log("[OUTBOUND - EOD] [XML] Download process completed.");

				//Logger.Log("[OUTBOUND - EOD] [SFTP] Start Uploading generated files to SFTP...");
				//await UploadToSftpAsync();
				//Logger.Log("[OUTBOUND - EOD] [SFTP] Upload to SFTP process completed.");

			}
			finally
			{
				btnSendXml.Enabled = true;
				Cursor.Current = Cursors.Default;
			}
		}

		private async Task ExecuteAllAndSaveToSingleXmlAsync(CancellationToken cancellationToken = default)
		{
			var prismStores = await repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
			var (fromDate, toDate) = GlobalHelper.GetProcessingTimeWindow(config);
			string outboundDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");
			string archiveDir = Path.Combine(outboundDir, "ARCHIVE", DateTime.Now.ToString("yyyyMMdd"));

			Directory.CreateDirectory(outboundDir);
			Directory.CreateDirectory(archiveDir);

			string countryCode = config.CountryCode ?? "XX";
			string todayPrefix = DateTime.Now.ToString("ddMMyyyy");

			Logger.Log($"[OUTBOUND - EOD] [XML] Start Generating POSLOG...");

			foreach (var store in prismStores)
			{
				string storeCode = ((IDictionary<string, object>)store).TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";
				Logger.Log($"[OUTBOUND - EOD] [XML] STORE_CODE : {storeCode}...");
				//Logger.Log($"[OUTBOUND - EOD] [XML] >> STORE CODE : {storeCode} <<");

				try
				{
					// Fetch data per store
					var storeSaleItems = await repositories.StoreSaleRepository.GetStoreSaleAsync(fromDate, toDate, storeCode);
					var storeShippingItems = await repositories.StoreShippingRepository.GetStoreShippingAsync(fromDate, toDate, storeCode);
					var storeReceivingItems = await repositories.StoreReceivingRepository.GetStoreReceivingAsync(fromDate, toDate, storeCode);
					var storeInventoryAdjustmentItems = await repositories.StoreInventoryAdjustmentRepository.GetStoreInventoryAdjustmentAsync(fromDate, toDate, storeCode);
					var storeReturnItems = await repositories.StoreReturnRepository.GetStoreReturnAsync(fromDate, toDate, storeCode);
					var storeGoodsReturnItems = await repositories.StoreGoodsReturnRepository.GetStoreGoodsReturnAsync(fromDate, toDate, storeCode);
					var storeGoodsItems = await repositories.StoreGoodsRepository.GetStoreGoodsAsync(fromDate, toDate, storeCode);

					// Generate XML fragments
					var xmlFragments = new[]
					{
						OutboundStoreSale.GenerateXml(storeSaleItems, null, "template"),
						OutboundStoreShipping.GenerateXml(storeShippingItems, null, "template"),
						OutboundStoreReceiving.GenerateXml(storeReceivingItems, null, "template"),
						OutboundStoreInventoryAdjustment.GenerateXml(storeInventoryAdjustmentItems, null, "template"),
						OutboundStoreReturn.GenerateXml(storeReturnItems, null, "template"),
						OutboundStoreGoodsReturn.GenerateXml(storeGoodsReturnItems, null, "template"),
						OutboundStoreGoods.GenerateXml(storeGoodsItems, null, "template"),
					};

					string[] xmlTypes = new[]
					{
						"StoreSale",
						"StoreShipping",
						"StoreReceiving",
						"StoreInventoryAdjustment",
						"StoreReturn",
						"StoreGoodsReturn",
						"StoreGoods",
					};

					var dataModules = new List<(string Label, IEnumerable<object> Items)>
					{
						("StoreSale", storeSaleItems as IEnumerable<object>),
						("StoreShipping", storeShippingItems as IEnumerable<object>),
						("StoreReceiving", storeReceivingItems as IEnumerable<object>),
						("StoreInventoryAdjustment", storeInventoryAdjustmentItems as IEnumerable<object>),
						("StoreReturn", storeReturnItems as IEnumerable<object>),
						("StoreGoodsReturn", storeGoodsReturnItems as IEnumerable<object>),
						("StoreGoods", storeGoodsItems as IEnumerable<object>),
					};

					//var storeRoot = new XElement("OutboundData");
					List<XElement> validFragments = new List<XElement>();

					for (int i = 0; i < xmlFragments.Length; i++)
					{
						string fragment = xmlFragments[i];
						string xmlType = xmlTypes[i];

						var countsByType = dataModules.ToDictionary(dm => dm.Label, dm => dm.Items?.Count() ?? 0);

						if (!string.IsNullOrWhiteSpace(fragment))
						{
							try
							{
								var parsedFragment = XElement.Parse(fragment);
								validFragments.Add(parsedFragment);

								int count = countsByType.ContainsKey(xmlType) ? countsByType[xmlType] : 0;
								Logger.Log($"[OUTBOUND - EOD]		Successfully generated {xmlType} XML. Item count: {count}");
							}
							catch (Exception ex)
							{
								Logger.Log($"[XML] Failed to parse {xmlType} XML for store {storeCode}: {ex.Message}");
							}
						}
						else
						{
							Logger.Log($"[XML] {xmlType} data for store {storeCode} is empty. Skipping.");
						}
					}

					// Skip file generation if no data at all
					if (!validFragments.Any())
					{
						Logger.Log($"[INFO] No outbound data generated for store {storeCode}. File will not be created.");
						continue;
					}

					// Prepare output XML document
					var document = new XDocument(
													new XDeclaration("1.0", "utf-8", "yes"),
													new XElement("Root", validFragments) // Or another name
												);

					var settings = new XmlWriterSettings
					{
						Indent = true,
						Encoding = Encoding.UTF8,
						OmitXmlDeclaration = false,
						Async = true
					};

					// Sequence logic
					var existingFiles = Directory.GetFiles(archiveDir, $"AMA_{countryCode}_{storeCode}_POSLOG_*.xml")
						.Where(f => Path.GetFileName(f).Contains(todayPrefix))
						.ToList();

					int nextSequence = existingFiles.Count + 1;
					string sequenceStr = nextSequence.ToString("D2");
					string timestamp = DateTime.Now.ToString("ddMMyyyyHHmmss");

					string fileName = $"AMA_{countryCode}_{storeCode}_POSLOG_{sequenceStr}_{timestamp}.xml";
					string filePath = Path.Combine(outboundDir, fileName);

					// Write to file
					using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
					using (var writer = XmlWriter.Create(stream, settings))
					{
						await writer.WriteStartDocumentAsync();

						// <POSLog ...>
						writer.WriteStartElement("POSLog", GlobalOutbound.NsIXRetail);
						writer.WriteAttributeString("xmlns", "dtv", null, GlobalOutbound.NsDtv);
						writer.WriteAttributeString("xmlns", "xs", null, GlobalOutbound.NsXsi);
						writer.WriteAttributeString("dtv", GlobalOutbound.NsDtv);
						writer.WriteAttributeString("xs", GlobalOutbound.NsXsi);
						writer.WriteAttributeString("schemaLocation", GlobalOutbound.NsIXRetail + "POSLog.xsd");

						foreach (var fragment in validFragments)
						{
							using (var reader = fragment.CreateReader())
							{
								await writer.WriteNodeAsync(reader, true);
							}

							// Add a newline after each fragment
							await writer.WriteCommentAsync(" ");
						}

						writer.WriteEndElement(); // </POSLog>

						await writer.WriteEndDocumentAsync();
						await writer.FlushAsync();
					}


					Logger.Log($"[OUTBOUND - EOD] [FILE] Downloaded successfully | File Name: {fileName}");
				}
				catch (Exception ex)
				{
					Logger.Log($"[ERROR] Failed to process store {storeCode}: {ex}");
				}
			}
		}

		private async Task ExecuteStoreInventoryCountAsync()
		{
			var (fromDate, toDate) = GlobalHelper.GetProcessingTimeWindow(config);
			var prismStores = await repositories.PrismRepository.GetRpsStore("ACTIVE", "1");
			foreach (var store in prismStores)
			{
				string storeCode = ((IDictionary<string, object>)store).TryGetValue("ADDRESS4", out var addr) ? addr?.ToString() : "N/A";
				await OutboundStoreInventoryCount.Execute(repositories.StoreInventoryCountRepository, config, "xml", storeCode);
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
				string localDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUTBOUND");

				try
				{
					using (var sftp = new SftpClient(host, port, username, password))
					{
						sftp.Connect();

						if (!sftp.Exists(remoteDirectory))
							sftp.CreateDirectory(remoteDirectory);

						if (!Directory.Exists(localDirectory))
						{
							Logger.Log("Local directory does not exist: " + localDirectory);
							//MessageBox.Show($"Local directory does not exist:\n{localDirectory}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						var files = Directory.GetFiles(localDirectory)
							.Where(f => f.EndsWith(".txt") || f.EndsWith(".xml"))
							.ToArray();

						foreach (var filePath in files)
						{
							try
							{
								using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
								{
									string remotePath = remoteDirectory + Path.GetFileName(filePath);
									sftp.UploadFile(fileStream, remotePath, true);
								}

								string archiveRootDir = Path.Combine(localDirectory, "ARCHIVE");
								string archiveDateDir = Path.Combine(archiveRootDir, DateTime.Now.ToString("yyyyMMdd"));
								Directory.CreateDirectory(archiveDateDir);

								string archivedPath = Path.Combine(archiveDateDir, Path.GetFileName(filePath));

								if (File.Exists(archivedPath))
								{
									File.Delete(archivedPath);
									Logger.Log($"Overwriting archived file: {archivedPath}");
								}

								File.Move(filePath, archivedPath);
								Logger.Log($"[OUTBOUND - EOD] [SFTP] Uploaded and archived: {archivedPath}");
							}
							catch (Exception ex)
							{
								Logger.Log($"Error handling file '{Path.GetFileName(filePath)}': {ex}");
							}
						}

						sftp.Disconnect();

						MessageBox.Show("Upload to SFTP completed successfully.", "SFTP Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
				catch (Exception ex)
				{
					Logger.Log("\"SFTP Upload failed.");
					//MessageBox.Show($"SFTP Upload failed:\n{ex.Message}", "SFTP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			});
		}

		// ***************************************************
		// Handlers/Helpers
		// ***************************************************
		public async void CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
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
	}
}
