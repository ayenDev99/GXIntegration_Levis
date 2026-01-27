using GXIntegration.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{
	public partial class ConfigurationPage : UserControl
	{
		private static GXConfig config;

		private TabControl tabControl;
		private TabPage tabPrism, tabDb, tabSftp, tabApi;

		public ConfigurationPage()
		{
            InitializeComponent();

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
			config = GXConfig.Load(configPath);

			InitializeTabs();
		}

		private void InitializeTabs()
		{
			tabControl = new TabControl
			{
                Location = new Point(200, 30),
                Size = new Size(900, 570),

                //Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
			};

			tabPrism = new TabPage("Prism Connection");
			tabDb = new TabPage("Prism Database");
			tabSftp = new TabPage("SFTP");
			tabApi = new TabPage("API");

			tabPrism.Controls.Add(new ConfigurationPrismTab(config) { Dock = DockStyle.Fill });
			tabDb.Controls.Add(new ConfigurationDBTab(config) { Dock = DockStyle.Fill });
			tabSftp.Controls.Add(new ConfigurationSFTPTab(config) { Dock = DockStyle.Fill });
			tabApi.Controls.Add(new ConfigurationAPITab(config) { Dock = DockStyle.Fill });

			this.Controls.Add(tabControl);
			tabControl.TabPages.Add(tabPrism);
			tabControl.TabPages.Add(tabDb);
			tabControl.TabPages.Add(tabSftp);
			tabControl.TabPages.Add(tabApi);
		}


	}
}
