using GXIntegration.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationPage : UserControl
	{
		private static GXConfig config;

		private TabControl tabControl;
		private TabPage tabDb, tabSftp, tabApi;

		public ConfigurationPage()
		{
			config = GXConfig.Load("config.xml");

			InitializeComponent();
			InitializeTabs();
		}

		private void InitializeTabs()
		{
			tabControl = new TabControl
			{
				Location = new Point(225, 20),
				Size = new Size(870, 450),
				Font = new Font("Segoe UI", 9)
			};

			tabDb = new TabPage("Database");
			tabSftp = new TabPage("SFTP");
			tabApi = new TabPage("API");


			tabDb.Controls.Add(new ConfigurationDBTab(config) { Dock = DockStyle.Fill });
			tabSftp.Controls.Add(new ConfigurationSFTPTab(config) { Dock = DockStyle.Fill });
			tabApi.Controls.Add(new ConfigurationAPITab() { Dock = DockStyle.Fill });

			this.Controls.Add(tabControl);
			tabControl.TabPages.Add(tabDb);
			tabControl.TabPages.Add(tabSftp);
			tabControl.TabPages.Add(tabApi);
		}


	}
}
