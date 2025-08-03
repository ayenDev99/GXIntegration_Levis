using System.Drawing;
using System.Windows.Forms;

namespace Modern_Sliding_Sidebar___C_Sharp_Winform
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
		private Panel MainContentPanel;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.Elipse_Form = new Guna.UI.WinForms.GunaElipse(this.components);
			this.SideBar = new Guna.UI.WinForms.GunaPanel();
			this.gunaPanel6 = new Guna.UI.WinForms.GunaPanel();
			this.About_Button = new Guna.UI.WinForms.GunaButton();
			this.gunaPanel5 = new Guna.UI.WinForms.GunaPanel();
			this.Outbound_Button = new Guna.UI.WinForms.GunaButton();
			this.gunaPanel8 = new Guna.UI.WinForms.GunaPanel();
			this.Inbound_Button = new Guna.UI.WinForms.GunaButton();
			this.gunaPanel4 = new Guna.UI.WinForms.GunaPanel();
			this.Configuration_Button = new Guna.UI.WinForms.GunaButton();
			this.gunaPanel3 = new Guna.UI.WinForms.GunaPanel();
			this.Home_Button = new Guna.UI.WinForms.GunaButton();
			this.gunaPanel2 = new Guna.UI.WinForms.GunaPanel();
			this.gunaPanel9 = new Guna.UI.WinForms.GunaPanel();
			this.Menu_Button = new Guna.UI.WinForms.GunaButton();
			this.DragControl_Form = new Guna.UI.WinForms.GunaDragControl(this.components);
			this.Timer_Sidebar_Menu = new System.Windows.Forms.Timer(this.components);
			this.Close_Button = new Guna.UI.WinForms.GunaImageButton();
			this.MainContentPanel = new System.Windows.Forms.Panel();
			this.SideBar.SuspendLayout();
			this.gunaPanel6.SuspendLayout();
			this.gunaPanel5.SuspendLayout();
			this.gunaPanel8.SuspendLayout();
			this.gunaPanel4.SuspendLayout();
			this.gunaPanel3.SuspendLayout();
			this.gunaPanel2.SuspendLayout();
			this.gunaPanel9.SuspendLayout();
			this.SuspendLayout();
			// 
			// Elipse_Form
			// 
			this.Elipse_Form.Radius = 9;
			this.Elipse_Form.TargetControl = this;
			// 
			// SideBar
			// 
			this.SideBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(40)))), ((int)(((byte)(45)))));
			this.SideBar.Controls.Add(this.gunaPanel6);
			this.SideBar.Controls.Add(this.gunaPanel5);
			this.SideBar.Controls.Add(this.gunaPanel8);
			this.SideBar.Controls.Add(this.gunaPanel4);
			this.SideBar.Controls.Add(this.gunaPanel3);
			this.SideBar.Controls.Add(this.gunaPanel2);
			this.SideBar.Dock = System.Windows.Forms.DockStyle.Left;
			this.SideBar.Location = new System.Drawing.Point(0, 0);
			this.SideBar.MaximumSize = new System.Drawing.Size(217, 494);
			this.SideBar.MinimumSize = new System.Drawing.Size(55, 494);
			this.SideBar.Name = "SideBar";
			this.SideBar.Size = new System.Drawing.Size(217, 494);
			this.SideBar.TabIndex = 0;
			this.SideBar.Paint += new System.Windows.Forms.PaintEventHandler(this.gunaPanel1_Paint);
			// 
			// gunaPanel6
			// 
			this.gunaPanel6.Controls.Add(this.About_Button);
			this.gunaPanel6.Dock = System.Windows.Forms.DockStyle.Top;
			this.gunaPanel6.Location = new System.Drawing.Point(0, 306);
			this.gunaPanel6.Name = "gunaPanel6";
			this.gunaPanel6.Size = new System.Drawing.Size(217, 55);
			this.gunaPanel6.TabIndex = 4;
			// 
			// About_Button
			// 
			this.About_Button.AnimationHoverSpeed = 0.07F;
			this.About_Button.AnimationSpeed = 0.03F;
			this.About_Button.BaseColor = System.Drawing.Color.Transparent;
			this.About_Button.BorderColor = System.Drawing.Color.Transparent;
			this.About_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.About_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.About_Button.FocusedColor = System.Drawing.Color.Empty;
			this.About_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.About_Button.ForeColor = System.Drawing.Color.White;
			this.About_Button.Image = ((System.Drawing.Image)(resources.GetObject("About_Button.Image")));
			this.About_Button.ImageSize = new System.Drawing.Size(20, 20);
			this.About_Button.Location = new System.Drawing.Point(7, 8);
			this.About_Button.Name = "About_Button";
			this.About_Button.OnHoverBaseColor = System.Drawing.Color.Transparent;
			this.About_Button.OnHoverBorderColor = System.Drawing.Color.Transparent;
			this.About_Button.OnHoverForeColor = System.Drawing.Color.Silver;
			this.About_Button.OnHoverImage = null;
			this.About_Button.OnPressedColor = System.Drawing.Color.White;
			this.About_Button.Size = new System.Drawing.Size(203, 40);
			this.About_Button.TabIndex = 1;
			this.About_Button.Text = "About";
			this.About_Button.TextOffsetX = 15;
			this.About_Button.Click += new System.EventHandler(this.About_Button_Click);
			// 
			// gunaPanel5
			// 
			this.gunaPanel5.Controls.Add(this.Outbound_Button);
			this.gunaPanel5.Dock = System.Windows.Forms.DockStyle.Top;
			this.gunaPanel5.Location = new System.Drawing.Point(0, 251);
			this.gunaPanel5.Name = "gunaPanel5";
			this.gunaPanel5.Size = new System.Drawing.Size(217, 55);
			this.gunaPanel5.TabIndex = 3;
			// 
			// Outbound_Button
			// 
			this.Outbound_Button.AnimationHoverSpeed = 0.07F;
			this.Outbound_Button.AnimationSpeed = 0.03F;
			this.Outbound_Button.BaseColor = System.Drawing.Color.Transparent;
			this.Outbound_Button.BorderColor = System.Drawing.Color.Transparent;
			this.Outbound_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Outbound_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.Outbound_Button.FocusedColor = System.Drawing.Color.Empty;
			this.Outbound_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Outbound_Button.ForeColor = System.Drawing.Color.White;
			this.Outbound_Button.Image = ((System.Drawing.Image)(resources.GetObject("Outbound_Button.Image")));
			this.Outbound_Button.ImageSize = new System.Drawing.Size(20, 20);
			this.Outbound_Button.Location = new System.Drawing.Point(7, 8);
			this.Outbound_Button.Name = "Outbound_Button";
			this.Outbound_Button.OnHoverBaseColor = System.Drawing.Color.Transparent;
			this.Outbound_Button.OnHoverBorderColor = System.Drawing.Color.Transparent;
			this.Outbound_Button.OnHoverForeColor = System.Drawing.Color.Silver;
			this.Outbound_Button.OnHoverImage = null;
			this.Outbound_Button.OnPressedColor = System.Drawing.Color.White;
			this.Outbound_Button.Size = new System.Drawing.Size(203, 40);
			this.Outbound_Button.TabIndex = 1;
			this.Outbound_Button.Text = "Outbound";
			this.Outbound_Button.TextOffsetX = 15;
			this.Outbound_Button.Click += new System.EventHandler(this.Outbound_Button_Click);
			// 
			// gunaPanel8
			// 
			this.gunaPanel8.Controls.Add(this.Inbound_Button);
			this.gunaPanel8.Dock = System.Windows.Forms.DockStyle.Top;
			this.gunaPanel8.Location = new System.Drawing.Point(0, 196);
			this.gunaPanel8.Name = "gunaPanel8";
			this.gunaPanel8.Size = new System.Drawing.Size(217, 55);
			this.gunaPanel8.TabIndex = 5;
			// 
			// Inbound_Button
			// 
			this.Inbound_Button.AnimationHoverSpeed = 0.07F;
			this.Inbound_Button.AnimationSpeed = 0.03F;
			this.Inbound_Button.BaseColor = System.Drawing.Color.Transparent;
			this.Inbound_Button.BorderColor = System.Drawing.Color.Transparent;
			this.Inbound_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Inbound_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.Inbound_Button.FocusedColor = System.Drawing.Color.Empty;
			this.Inbound_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Inbound_Button.ForeColor = System.Drawing.Color.White;
			this.Inbound_Button.Image = ((System.Drawing.Image)(resources.GetObject("Inbound_Button.Image")));
			this.Inbound_Button.ImageSize = new System.Drawing.Size(20, 20);
			this.Inbound_Button.Location = new System.Drawing.Point(7, 8);
			this.Inbound_Button.Name = "Inbound_Button";
			this.Inbound_Button.OnHoverBaseColor = System.Drawing.Color.Transparent;
			this.Inbound_Button.OnHoverBorderColor = System.Drawing.Color.Transparent;
			this.Inbound_Button.OnHoverForeColor = System.Drawing.Color.Silver;
			this.Inbound_Button.OnHoverImage = null;
			this.Inbound_Button.OnPressedColor = System.Drawing.Color.White;
			this.Inbound_Button.Size = new System.Drawing.Size(203, 40);
			this.Inbound_Button.TabIndex = 1;
			this.Inbound_Button.Text = "Inbound";
			this.Inbound_Button.TextOffsetX = 15;
			this.Inbound_Button.Click += new System.EventHandler(this.Inbound_Button_Click);
			// 
			// gunaPanel4
			// 
			this.gunaPanel4.Controls.Add(this.Configuration_Button);
			this.gunaPanel4.Dock = System.Windows.Forms.DockStyle.Top;
			this.gunaPanel4.Location = new System.Drawing.Point(0, 141);
			this.gunaPanel4.Name = "gunaPanel4";
			this.gunaPanel4.Size = new System.Drawing.Size(217, 55);
			this.gunaPanel4.TabIndex = 2;
			// 
			// Configuration_Button
			// 
			this.Configuration_Button.AnimationHoverSpeed = 0.07F;
			this.Configuration_Button.AnimationSpeed = 0.03F;
			this.Configuration_Button.BaseColor = System.Drawing.Color.Transparent;
			this.Configuration_Button.BorderColor = System.Drawing.Color.Transparent;
			this.Configuration_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Configuration_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.Configuration_Button.FocusedColor = System.Drawing.Color.Empty;
			this.Configuration_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Configuration_Button.ForeColor = System.Drawing.Color.White;
			this.Configuration_Button.Image = ((System.Drawing.Image)(resources.GetObject("Configuration_Button.Image")));
			this.Configuration_Button.ImageSize = new System.Drawing.Size(20, 20);
			this.Configuration_Button.Location = new System.Drawing.Point(7, 7);
			this.Configuration_Button.Name = "Configuration_Button";
			this.Configuration_Button.OnHoverBaseColor = System.Drawing.Color.Transparent;
			this.Configuration_Button.OnHoverBorderColor = System.Drawing.Color.Transparent;
			this.Configuration_Button.OnHoverForeColor = System.Drawing.Color.Silver;
			this.Configuration_Button.OnHoverImage = null;
			this.Configuration_Button.OnPressedColor = System.Drawing.Color.White;
			this.Configuration_Button.Size = new System.Drawing.Size(203, 40);
			this.Configuration_Button.TabIndex = 1;
			this.Configuration_Button.Text = "Configuration";
			this.Configuration_Button.TextOffsetX = 15;
			this.Configuration_Button.Click += new System.EventHandler(this.Configuration_Button_Click);
			// 
			// gunaPanel3
			// 
			this.gunaPanel3.Controls.Add(this.Home_Button);
			this.gunaPanel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.gunaPanel3.Location = new System.Drawing.Point(0, 86);
			this.gunaPanel3.Name = "gunaPanel3";
			this.gunaPanel3.Size = new System.Drawing.Size(217, 55);
			this.gunaPanel3.TabIndex = 1;
			// 
			// Home_Button
			// 
			this.Home_Button.AnimationHoverSpeed = 0.07F;
			this.Home_Button.AnimationSpeed = 0.03F;
			this.Home_Button.BaseColor = System.Drawing.Color.Transparent;
			this.Home_Button.BorderColor = System.Drawing.Color.Transparent;
			this.Home_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Home_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.Home_Button.FocusedColor = System.Drawing.Color.Empty;
			this.Home_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Home_Button.ForeColor = System.Drawing.Color.White;
			this.Home_Button.Image = ((System.Drawing.Image)(resources.GetObject("Home_Button.Image")));
			this.Home_Button.ImageSize = new System.Drawing.Size(20, 20);
			this.Home_Button.Location = new System.Drawing.Point(7, 7);
			this.Home_Button.Name = "Home_Button";
			this.Home_Button.OnHoverBaseColor = System.Drawing.Color.Transparent;
			this.Home_Button.OnHoverBorderColor = System.Drawing.Color.Transparent;
			this.Home_Button.OnHoverForeColor = System.Drawing.Color.Silver;
			this.Home_Button.OnHoverImage = null;
			this.Home_Button.OnPressedColor = System.Drawing.Color.White;
			this.Home_Button.Size = new System.Drawing.Size(203, 40);
			this.Home_Button.TabIndex = 1;
			this.Home_Button.Text = "Home";
			this.Home_Button.TextOffsetX = 15;
			this.Home_Button.Click += new System.EventHandler(this.Home_Button_Click);
			// 
			// gunaPanel2
			// 
			this.gunaPanel2.Controls.Add(this.gunaPanel9);
			this.gunaPanel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.gunaPanel2.Location = new System.Drawing.Point(0, 0);
			this.gunaPanel2.Name = "gunaPanel2";
			this.gunaPanel2.Size = new System.Drawing.Size(217, 86);
			this.gunaPanel2.TabIndex = 0;
			// 
			// gunaPanel9
			// 
			this.gunaPanel9.Controls.Add(this.Menu_Button);
			this.gunaPanel9.Location = new System.Drawing.Point(0, 12);
			this.gunaPanel9.Name = "gunaPanel9";
			this.gunaPanel9.Size = new System.Drawing.Size(213, 68);
			this.gunaPanel9.TabIndex = 2;
			// 
			// Menu_Button
			// 
			this.Menu_Button.AnimationHoverSpeed = 0.07F;
			this.Menu_Button.AnimationSpeed = 0.03F;
			this.Menu_Button.BaseColor = System.Drawing.Color.Transparent;
			this.Menu_Button.BorderColor = System.Drawing.Color.Transparent;
			this.Menu_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Menu_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.Menu_Button.FocusedColor = System.Drawing.Color.Empty;
			this.Menu_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Menu_Button.ForeColor = System.Drawing.Color.White;
			this.Menu_Button.Image = ((System.Drawing.Image)(resources.GetObject("Menu_Button.Image")));
			this.Menu_Button.ImageSize = new System.Drawing.Size(20, 20);
			this.Menu_Button.Location = new System.Drawing.Point(7, 14);
			this.Menu_Button.Name = "Menu_Button";
			this.Menu_Button.OnHoverBaseColor = System.Drawing.Color.Transparent;
			this.Menu_Button.OnHoverBorderColor = System.Drawing.Color.Transparent;
			this.Menu_Button.OnHoverForeColor = System.Drawing.Color.Silver;
			this.Menu_Button.OnHoverImage = null;
			this.Menu_Button.OnPressedColor = System.Drawing.Color.White;
			this.Menu_Button.Size = new System.Drawing.Size(203, 40);
			this.Menu_Button.TabIndex = 1;
			this.Menu_Button.Text = "Menu";
			this.Menu_Button.TextOffsetX = 15;
			this.Menu_Button.Click += new System.EventHandler(this.Menu_Button_Click);
			// 
			// DragControl_Form
			// 
			this.DragControl_Form.TargetControl = this;
			// 
			// Timer_Sidebar_Menu
			// 
			this.Timer_Sidebar_Menu.Interval = 10;
			this.Timer_Sidebar_Menu.Tick += new System.EventHandler(this.Timer_Sidebar_Menu_Tick);
			// 
			// Close_Button
			// 
			this.Close_Button.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Close_Button.DialogResult = System.Windows.Forms.DialogResult.None;
			this.Close_Button.Image = global::GXIntegration_Levis.Properties.Resources.multiply_48px__;
			this.Close_Button.ImageSize = new System.Drawing.Size(18, 18);
			this.Close_Button.Location = new System.Drawing.Point(766, 2);
			this.Close_Button.Name = "Close_Button";
			this.Close_Button.OnHoverImage = global::GXIntegration_Levis.Properties.Resources.multiply_48px_____;
			this.Close_Button.OnHoverImageOffset = new System.Drawing.Point(0, 0);
			this.Close_Button.Size = new System.Drawing.Size(35, 35);
			this.Close_Button.TabIndex = 1;
			this.Close_Button.Click += new System.EventHandler(this.Close_Button_Click);
			// 
			// MainContentPanel
			// 
			this.MainContentPanel.BackColor = System.Drawing.Color.White;
			this.MainContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainContentPanel.Location = new System.Drawing.Point(0, 0);
			this.MainContentPanel.Name = "MainContentPanel";
			this.MainContentPanel.Size = new System.Drawing.Size(805, 494);
			this.MainContentPanel.TabIndex = 3;
			this.MainContentPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.MainContentPanel_Paint);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(805, 494);
			this.Controls.Add(this.Close_Button);
			this.Controls.Add(this.SideBar);
			this.Controls.Add(this.MainContentPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "Form1";
			this.Text = "k.//";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.SideBar.ResumeLayout(false);
			this.gunaPanel6.ResumeLayout(false);
			this.gunaPanel5.ResumeLayout(false);
			this.gunaPanel8.ResumeLayout(false);
			this.gunaPanel4.ResumeLayout(false);
			this.gunaPanel3.ResumeLayout(false);
			this.gunaPanel2.ResumeLayout(false);
			this.gunaPanel9.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Guna.UI.WinForms.GunaElipse Elipse_Form;
        private Guna.UI.WinForms.GunaPanel SideBar;
        private Guna.UI.WinForms.GunaPanel gunaPanel3;
        private Guna.UI.WinForms.GunaPanel gunaPanel2;
        private Guna.UI.WinForms.GunaImageButton Close_Button;
        private Guna.UI.WinForms.GunaPanel gunaPanel6;
        private Guna.UI.WinForms.GunaButton Inbound_Button;
        private Guna.UI.WinForms.GunaPanel gunaPanel5;
        private Guna.UI.WinForms.GunaButton Outbound_Button;
        private Guna.UI.WinForms.GunaPanel gunaPanel8;
        private Guna.UI.WinForms.GunaButton About_Button;
        private Guna.UI.WinForms.GunaPanel gunaPanel4;
        private Guna.UI.WinForms.GunaButton Configuration_Button;
        private Guna.UI.WinForms.GunaButton Home_Button;
        private Guna.UI.WinForms.GunaPanel gunaPanel9;
        private Guna.UI.WinForms.GunaButton Menu_Button;
        private Guna.UI.WinForms.GunaDragControl DragControl_Form;
        private System.Windows.Forms.Timer Timer_Sidebar_Menu;
	

		}
	}

