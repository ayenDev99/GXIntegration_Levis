using Guna.UI.WinForms;
using GXIntegration.Properties;
using GXIntegration_Levis.Data.Access;
using GXIntegration_Levis.Helpers;
using GXIntegration_Levis.OutboundHandlers;
using GXIntegration_Levis.Properties;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Org.BouncyCastle.Math.EC.ECCurve;


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


		private void InitializeGrid()
		{
			guna1DataGridView1 = new GunaDataGridView
			{
				Location = new Point(20, 20),
				Size = new Size(820, 180),
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

			this.Controls.Add(guna1DataGridView1);
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

		private async Task SendXmlFilesToApi()
		{
			string apiUrl = "https://mule-rtf-test.levi.com/retail-pos-ph-rpp-exp-api-dev1/retail-pos-ph-rpp-exp-api/v1/sale";
			string username = "1d75a7f3-1b67-4c6e-9c6e-d0f6ba114417";
			string password = "3~E8Q~CKgCliOmXmKjSVXJtrffHYED4_cKDPhax4";

			string xmlTemplate = await OutboundStoreSale.ExecuteAPI(_repositories.StoreSaleRepository, _config, "template");

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
