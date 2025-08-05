using Dapper;
using Guna.UI.WinForms;
using GXIntegration_Levis;
using Modern_Sliding_Sidebar___C_Sharp_Winform.Properties;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Modern_Sliding_Sidebar___C_Sharp_Winform
{
	public partial class Form1 : Form
    {
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HTCAPTION = 0x2;

		static GXConfig config;
		private ConfigurationPage _configurationPage;

		bool sideBar_Expand = true;
		private Guna.UI.WinForms.GunaButton _activeButton = null;

		public Form1()
        {
            InitializeComponent();
			
			EnableDrag(SideBar); 


			config = GXConfig.Load("config.xml");
			MainContentPanel.Dock = DockStyle.Fill;
		}

		private void EnableDrag(Control control)
		{
			control.MouseDown += (s, e) =>
			{
				if (e.Button == MouseButtons.Left)
				{
					ReleaseCapture();
					SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
				}
			};
		}


		private void Form1_MouseDown(object sender, MouseEventArgs e)
		{
			// Trigger dragging
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
			}
		}

		private void Form1_Load(object sender, EventArgs e)
        {
			SetActiveSidebarButton(Home_Button);
			LoadPage(new HomePage());
			EnableDrag(this);
		}

        private void gunaPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void Close_Button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Timer_Sidebar_Menu_Tick(object sender, EventArgs e)
        {
            if (sideBar_Expand)
            {
                SideBar.Width -= 10;
                if (SideBar.Width == SideBar.MinimumSize.Width)
                {
                    sideBar_Expand = false;
                    Timer_Sidebar_Menu.Stop();
                }
            }
            else
                {
                    SideBar.Width += 10;
                    if (SideBar.Width == SideBar.MaximumSize.Width)
                    {
                        sideBar_Expand = true;
                        Timer_Sidebar_Menu.Stop();
                    }
                }
        }   
        
      
        private void Menu_Button_Click(object sender, EventArgs e)
        {
            Timer_Sidebar_Menu.Start();
        }

        private void gunaImageButton1_Click(object sender, EventArgs e)
        {

        }

        private void Link_Github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            
        }

		// *********************************************************
		// Sidebar Buttons
		// *********************************************************
		private void Home_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
			LoadPage(new HomePage());
		}

		private void Configuration_Button_Click(object sender, EventArgs e)
		{
			if (_configurationPage == null)
				_configurationPage = new ConfigurationPage();

			LoadPage(_configurationPage);
			SetActiveSidebarButton(Configuration_Button);
		}
		private void Inbound_Button_Click(object sender, EventArgs e)
		{
			LoadPage(new InboundPage());
			SetActiveSidebarButton(Inbound_Button);
		}

		private void Outbound_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
		}
		private void About_Button_Click(object sender, EventArgs e)
		{
			SetActiveSidebarButton((Guna.UI.WinForms.GunaButton)sender);
			LoadPage(new AboutPage());
		}


		// *********************************************************
		// Helpers
		// *********************************************************
		private void SetActiveSidebarButton(Guna.UI.WinForms.GunaButton button)
		{
			// Reset previous active button
			if (_activeButton != null)
			{
				_activeButton.BaseColor = Color.Transparent;
				_activeButton.ForeColor = Color.White;
				_activeButton.OnHoverBaseColor = Color.FromArgb(40, 40, 100);
				_activeButton.OnHoverForeColor = Color.White;
			}

			// Set new active button
			_activeButton = button;
			_activeButton.BaseColor = Color.FromArgb(60, 60, 120); // Static active color
			_activeButton.ForeColor = Color.White;
			_activeButton.OnHoverBaseColor = _activeButton.BaseColor; // Lock hover color
			_activeButton.OnHoverForeColor = _activeButton.ForeColor;
		}

		private void LoadPage(UserControl page)
		{
			MainContentPanel.Controls.Clear();
			page.Dock = DockStyle.Fill;
			MainContentPanel.Controls.Add(page);
		}

		private void MainContentPanel_Paint(object sender, PaintEventArgs e)
		{

		}
	}

}
