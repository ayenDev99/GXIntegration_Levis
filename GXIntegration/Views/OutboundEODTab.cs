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
using System.Threading.Tasks;
using System.Windows.Forms;

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
		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(20, 20),
				Size = new Size(704, 220),
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
			guna1DataGridView1.Columns[3].Width = 85;

			var imageColumn = new DataGridViewImageColumn
			{
				Name = "Action",
				HeaderText = "Action",
				Image = Resources.icon_download,
				Width = 50,
				ImageLayout = DataGridViewImageCellLayout.Zoom
			};
			guna1DataGridView1.Columns.Add(imageColumn);

			guna1DataGridView1.CellContentClick += CellContentClick;
			guna1DataGridView1.CellMouseMove += CellMouseMove;
			guna1DataGridView1.CellMouseLeave += CellMouseLeave;

			void AddRow(string id, string name, string format, string type)
				=> guna1DataGridView1.Rows.Add(id, name, format, type);

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
				["ASN - RECEIVING"] = () => OutboundASN.Execute(repositories.ASNRepository, config),
				["RETURN_TO_DC"] = () => OutboundStoreGoodsReturn.Execute(repositories.StoreGoodsReturnRepository, config),
				["RETAIL_SALE"] = () => OutboundStoreSale.Execute(repositories.StoreSaleRepository, config, "xml"),
				["RETURN_SALE"] = () => OutboundStoreReturn.Execute(repositories.StoreReturnRepository, config),
				["ADJUSTMENT"] = () => OutboundStoreInventoryAdjustment.Execute(repositories.StoreInventoryAdjustmentRepository, config),
				["STORE_TRANSFER - SHIPPING"] = () => OutboundStoreShipping.Execute(repositories.StoreShippingRepository, config),
				["STORE_TRANSFER - RECEIVING"] = () => OutboundStoreReceiving.Execute(repositories.StoreReceivingRepository, config),
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
			btnSendXml.Enabled = false;
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
				btnSendXml.Enabled = true;
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
				string localDirectory = @"C:\GXIntegration_Levis\GXIntegration\bin\Debug\OUTBOUND\";

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
