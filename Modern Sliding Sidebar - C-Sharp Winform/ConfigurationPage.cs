using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXIntegration_Levis
{
	public partial class ConfigurationPage : UserControl
	{
		public ConfigurationPage()
		{
			InitializeComponent();

			// Add tabs programmatically or use designer
			TabPage databaseTab = new TabPage("Database");
			TabPage apiTab = new TabPage("API");
			TabPage sftpTab = new TabPage("SFTP");
			TabPage timingTab = new TabPage("Timing");

			// Add controls to tabs as needed
			databaseTab.Controls.Add(new Label() { Text = "Database Connection here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
			apiTab.Controls.Add(new Label() { Text = "API Connection here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
			sftpTab.Controls.Add(new Label() { Text = "SFTP Connection here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
			timingTab.Controls.Add(new Label() { Text = "Timing of Generation here", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

			tabControl1.TabPages.Add(databaseTab);
			tabControl1.TabPages.Add(apiTab);
			tabControl1.TabPages.Add(sftpTab);
			tabControl1.TabPages.Add(timingTab);
		}

		private void ConfigurationPage_Load(object sender, EventArgs e)
		{

		}
	}
}
