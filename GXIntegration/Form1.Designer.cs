using System;
using System.Drawing;
using System.Windows.Forms;

namespace GXIntegration
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.Elipse_Form = new Guna.UI2.WinForms.Guna2Elipse(this.components);
            this.DragControl_Form = new Guna.UI2.WinForms.Guna2DragControl(this.components);
            this.Timer_Sidebar_Menu = new System.Windows.Forms.Timer(this.components);
            this.pnlSideBar = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2GradientPanel1 = new Guna.UI2.WinForms.Guna2GradientPanel();
            this.Outbound_Button = new Guna.UI2.WinForms.Guna2Button();
            this.Inbound_Button = new Guna.UI2.WinForms.Guna2Button();
            this.Configuration_Button = new Guna.UI2.WinForms.Guna2Button();
            this.Home_Button = new Guna.UI2.WinForms.Guna2Button();
            this.pnlLogo = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2HtmlLabel2 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.guna2PictureBox1 = new Guna.UI2.WinForms.Guna2PictureBox();
            this.lblDateTime = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.pnlContainer = new Guna.UI2.WinForms.Guna2Panel();
            this.pnlMainContent = new Guna.UI2.WinForms.Guna2Panel();
            this.pnlTopBar = new Guna.UI2.WinForms.Guna2Panel();
            this.btnMini = new Guna.UI2.WinForms.Guna2ControlBox();
            this.btnClose = new Guna.UI2.WinForms.Guna2ControlBox();
            this.timerDateTime = new System.Windows.Forms.Timer(this.components);
            this.guna2DragControl1 = new Guna.UI2.WinForms.Guna2DragControl(this.components);
            this.pnlSideBar.SuspendLayout();
            this.guna2GradientPanel1.SuspendLayout();
            this.pnlLogo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.guna2PictureBox1)).BeginInit();
            this.pnlContainer.SuspendLayout();
            this.pnlTopBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // Elipse_Form
            // 
            this.Elipse_Form.TargetControl = this;
            // 
            // DragControl_Form
            // 
            this.DragControl_Form.DockIndicatorTransparencyValue = 0.6D;
            this.DragControl_Form.TargetControl = this.pnlTopBar;
            this.DragControl_Form.UseTransparentDrag = true;
            // 
            // pnlSideBar
            // 
            this.pnlSideBar.Controls.Add(this.guna2GradientPanel1);
            this.pnlSideBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSideBar.Location = new System.Drawing.Point(0, 0);
            this.pnlSideBar.Name = "pnlSideBar";
            this.pnlSideBar.Size = new System.Drawing.Size(200, 494);
            this.pnlSideBar.TabIndex = 0;
            // 
            // guna2GradientPanel1
            // 
            this.guna2GradientPanel1.Controls.Add(this.Outbound_Button);
            this.guna2GradientPanel1.Controls.Add(this.Inbound_Button);
            this.guna2GradientPanel1.Controls.Add(this.Configuration_Button);
            this.guna2GradientPanel1.Controls.Add(this.Home_Button);
            this.guna2GradientPanel1.Controls.Add(this.pnlLogo);
            this.guna2GradientPanel1.Controls.Add(this.lblDateTime);
            this.guna2GradientPanel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.guna2GradientPanel1.FillColor = System.Drawing.Color.Indigo;
            this.guna2GradientPanel1.FillColor2 = System.Drawing.Color.BlueViolet;
            this.guna2GradientPanel1.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            this.guna2GradientPanel1.Location = new System.Drawing.Point(0, 0);
            this.guna2GradientPanel1.Name = "guna2GradientPanel1";
            this.guna2GradientPanel1.Size = new System.Drawing.Size(200, 494);
            this.guna2GradientPanel1.TabIndex = 0;
            // 
            // Outbound_Button
            // 
            this.Outbound_Button.BackColor = System.Drawing.Color.Transparent;
            this.Outbound_Button.ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton;
            this.Outbound_Button.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.Outbound_Button.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.Outbound_Button.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.Outbound_Button.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.Outbound_Button.Dock = System.Windows.Forms.DockStyle.Top;
            this.Outbound_Button.FillColor = System.Drawing.Color.Transparent;
            this.Outbound_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Outbound_Button.ForeColor = System.Drawing.Color.White;
            this.Outbound_Button.Image = global::GXIntegration_Levis.Properties.Resources.icon_outbound;
            this.Outbound_Button.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Outbound_Button.ImageOffset = new System.Drawing.Point(7, 0);
            this.Outbound_Button.ImageSize = new System.Drawing.Size(18, 18);
            this.Outbound_Button.Location = new System.Drawing.Point(0, 233);
            this.Outbound_Button.Name = "Outbound_Button";
            this.Outbound_Button.Size = new System.Drawing.Size(200, 40);
            this.Outbound_Button.TabIndex = 5;
            this.Outbound_Button.Text = "Outbound";
            this.Outbound_Button.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Outbound_Button.TextOffset = new System.Drawing.Point(8, 0);
            this.Outbound_Button.Click += new System.EventHandler(this.Outbound_Button_Click);
            // 
            // Inbound_Button
            // 
            this.Inbound_Button.BackColor = System.Drawing.Color.Transparent;
            this.Inbound_Button.ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton;
            this.Inbound_Button.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.Inbound_Button.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.Inbound_Button.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.Inbound_Button.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.Inbound_Button.Dock = System.Windows.Forms.DockStyle.Top;
            this.Inbound_Button.FillColor = System.Drawing.Color.Transparent;
            this.Inbound_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Inbound_Button.ForeColor = System.Drawing.Color.White;
            this.Inbound_Button.Image = global::GXIntegration_Levis.Properties.Resources.icon_inbound;
            this.Inbound_Button.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Inbound_Button.ImageOffset = new System.Drawing.Point(7, 0);
            this.Inbound_Button.ImageSize = new System.Drawing.Size(15, 15);
            this.Inbound_Button.Location = new System.Drawing.Point(0, 193);
            this.Inbound_Button.Name = "Inbound_Button";
            this.Inbound_Button.Size = new System.Drawing.Size(200, 40);
            this.Inbound_Button.TabIndex = 4;
            this.Inbound_Button.Text = "Inbound";
            this.Inbound_Button.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Inbound_Button.TextOffset = new System.Drawing.Point(12, 0);
            this.Inbound_Button.Click += new System.EventHandler(this.Inbound_Button_Click);
            // 
            // Configuration_Button
            // 
            this.Configuration_Button.BackColor = System.Drawing.Color.Transparent;
            this.Configuration_Button.ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton;
            this.Configuration_Button.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.Configuration_Button.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.Configuration_Button.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.Configuration_Button.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.Configuration_Button.Dock = System.Windows.Forms.DockStyle.Top;
            this.Configuration_Button.FillColor = System.Drawing.Color.Transparent;
            this.Configuration_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Configuration_Button.ForeColor = System.Drawing.Color.White;
            this.Configuration_Button.Image = global::GXIntegration_Levis.Properties.Resources.icon_config;
            this.Configuration_Button.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Configuration_Button.ImageOffset = new System.Drawing.Point(5, 0);
            this.Configuration_Button.ImageSize = new System.Drawing.Size(17, 17);
            this.Configuration_Button.Location = new System.Drawing.Point(0, 153);
            this.Configuration_Button.Name = "Configuration_Button";
            this.Configuration_Button.Size = new System.Drawing.Size(200, 40);
            this.Configuration_Button.TabIndex = 3;
            this.Configuration_Button.Text = "Configuration";
            this.Configuration_Button.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Configuration_Button.TextOffset = new System.Drawing.Point(10, 0);
            this.Configuration_Button.Click += new System.EventHandler(this.Configuration_Button_Click);
            // 
            // Home_Button
            // 
            this.Home_Button.BackColor = System.Drawing.Color.Transparent;
            this.Home_Button.ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton;
            this.Home_Button.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.Home_Button.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.Home_Button.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.Home_Button.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.Home_Button.Dock = System.Windows.Forms.DockStyle.Top;
            this.Home_Button.FillColor = System.Drawing.Color.Transparent;
            this.Home_Button.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Home_Button.ForeColor = System.Drawing.Color.White;
            this.Home_Button.Image = global::GXIntegration_Levis.Properties.Resources.icon_home_;
            this.Home_Button.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Home_Button.ImageOffset = new System.Drawing.Point(5, 0);
            this.Home_Button.ImageSize = new System.Drawing.Size(17, 17);
            this.Home_Button.Location = new System.Drawing.Point(0, 113);
            this.Home_Button.Name = "Home_Button";
            this.Home_Button.Size = new System.Drawing.Size(200, 40);
            this.Home_Button.TabIndex = 2;
            this.Home_Button.Text = "Home";
            this.Home_Button.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.Home_Button.TextOffset = new System.Drawing.Point(10, 0);
            this.Home_Button.Click += new System.EventHandler(this.Home_Button_Click);
            // 
            // pnlLogo
            // 
            this.pnlLogo.BackColor = System.Drawing.Color.Transparent;
            this.pnlLogo.Controls.Add(this.guna2HtmlLabel2);
            this.pnlLogo.Controls.Add(this.guna2PictureBox1);
            this.pnlLogo.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLogo.FillColor = System.Drawing.Color.Transparent;
            this.pnlLogo.Location = new System.Drawing.Point(0, 0);
            this.pnlLogo.Name = "pnlLogo";
            this.pnlLogo.Size = new System.Drawing.Size(200, 113);
            this.pnlLogo.TabIndex = 1;
            // 
            // guna2HtmlLabel2
            // 
            this.guna2HtmlLabel2.AutoSize = false;
            this.guna2HtmlLabel2.BackColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.guna2HtmlLabel2.ForeColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel2.Location = new System.Drawing.Point(87, 83);
            this.guna2HtmlLabel2.Name = "guna2HtmlLabel2";
            this.guna2HtmlLabel2.Size = new System.Drawing.Size(33, 15);
            this.guna2HtmlLabel2.TabIndex = 1;
            this.guna2HtmlLabel2.Text = "v1.0.0";
            // 
            // guna2PictureBox1
            // 
            this.guna2PictureBox1.FillColor = System.Drawing.Color.Transparent;
            this.guna2PictureBox1.Image = global::GXIntegration_Levis.Properties.Resources.logo_geniex;
            this.guna2PictureBox1.ImageRotate = 0F;
            this.guna2PictureBox1.Location = new System.Drawing.Point(57, 20);
            this.guna2PictureBox1.Name = "guna2PictureBox1";
            this.guna2PictureBox1.Size = new System.Drawing.Size(91, 57);
            this.guna2PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.guna2PictureBox1.TabIndex = 0;
            this.guna2PictureBox1.TabStop = false;
            // 
            // lblDateTime
            // 
            this.lblDateTime.AutoSize = false;
            this.lblDateTime.BackColor = System.Drawing.Color.Transparent;
            this.lblDateTime.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblDateTime.ForeColor = System.Drawing.Color.White;
            this.lblDateTime.Location = new System.Drawing.Point(0, 464);
            this.lblDateTime.Name = "lblDateTime";
            this.lblDateTime.Size = new System.Drawing.Size(200, 30);
            this.lblDateTime.TabIndex = 0;
            this.lblDateTime.Text = "guna2HtmlLabel1";
            this.lblDateTime.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlContainer
            // 
            this.pnlContainer.BackColor = System.Drawing.SystemColors.Control;
            this.pnlContainer.Controls.Add(this.pnlTopBar);
            this.pnlContainer.Controls.Add(this.pnlMainContent);
            this.pnlContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContainer.FillColor = System.Drawing.Color.White;
            this.pnlContainer.Location = new System.Drawing.Point(200, 0);
            this.pnlContainer.Name = "pnlContainer";
            this.pnlContainer.Size = new System.Drawing.Size(700, 494);
            this.pnlContainer.TabIndex = 1;
            // 
            // pnlMainContent
            // 
            this.pnlMainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMainContent.Location = new System.Drawing.Point(0, 0);
            this.pnlMainContent.Name = "pnlMainContent";
            this.pnlMainContent.Size = new System.Drawing.Size(700, 494);
            this.pnlMainContent.TabIndex = 0;
            // 
            // pnlTopBar
            // 
            this.pnlTopBar.BackColor = System.Drawing.Color.Transparent;
            this.pnlTopBar.Controls.Add(this.btnMini);
            this.pnlTopBar.Controls.Add(this.btnClose);
            this.pnlTopBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTopBar.FillColor = System.Drawing.SystemColors.Control;
            this.pnlTopBar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.pnlTopBar.Location = new System.Drawing.Point(0, 0);
            this.pnlTopBar.Name = "pnlTopBar";
            this.pnlTopBar.Size = new System.Drawing.Size(700, 31);
            this.pnlTopBar.TabIndex = 2;
            this.pnlTopBar.UseTransparentBackground = true;
            // 
            // btnMini
            // 
            this.btnMini.BackColor = System.Drawing.Color.Transparent;
            this.btnMini.BorderColor = System.Drawing.Color.Transparent;
            this.btnMini.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            this.btnMini.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMini.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnMini.FillColor = System.Drawing.Color.Transparent;
            this.btnMini.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMini.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.btnMini.IconColor = System.Drawing.Color.Black;
            this.btnMini.Location = new System.Drawing.Point(610, 0);
            this.btnMini.Name = "btnMini";
            this.btnMini.Size = new System.Drawing.Size(45, 31);
            this.btnMini.TabIndex = 1;
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.Transparent;
            this.btnClose.BorderColor = System.Drawing.Color.Transparent;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.FillColor = System.Drawing.Color.Transparent;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnClose.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.btnClose.IconColor = System.Drawing.Color.Black;
            this.btnClose.Location = new System.Drawing.Point(655, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(45, 31);
            this.btnClose.TabIndex = 0;
            // 
            // timerDateTime
            // 
            this.timerDateTime.Enabled = true;
            this.timerDateTime.Interval = 1000;
            timerDateTime.Tick += timerDateTime_Tick;
            // 
            // guna2DragControl1
            // 
            this.guna2DragControl1.DockIndicatorTransparencyValue = 0.6D;
            this.guna2DragControl1.TargetControl = this.guna2GradientPanel1;
            this.guna2DragControl1.UseTransparentDrag = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 494);
            this.Controls.Add(this.pnlContainer);
            this.Controls.Add(this.pnlSideBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "k.//";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlSideBar.ResumeLayout(false);
            this.guna2GradientPanel1.ResumeLayout(false);
            this.pnlLogo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.guna2PictureBox1)).EndInit();
            this.pnlContainer.ResumeLayout(false);
            this.pnlTopBar.ResumeLayout(false);
            this.ResumeLayout(false);

		}

        private void SideBar_Paint(object sender, PaintEventArgs e)
        {
            // Example: draw a simple border
            Panel panel = sender as Panel;
            if (panel != null)
            {
                using (Pen pen = new Pen(Color.Silver, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            }
        }
        #endregion

        private Guna.UI2.WinForms.Guna2Elipse Elipse_Form;
        private Guna.UI2.WinForms.Guna2DragControl DragControl_Form;
        private System.Windows.Forms.Timer Timer_Sidebar_Menu;
        private Guna.UI2.WinForms.Guna2Panel pnlSideBar;
        private Guna.UI2.WinForms.Guna2GradientPanel guna2GradientPanel1;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblDateTime;
        private Guna.UI2.WinForms.Guna2Panel pnlLogo;
        private Guna.UI2.WinForms.Guna2Button Outbound_Button;
        private Guna.UI2.WinForms.Guna2Button Inbound_Button;
        private Guna.UI2.WinForms.Guna2Button Configuration_Button;
        private Guna.UI2.WinForms.Guna2Button Home_Button;
        private Guna.UI2.WinForms.Guna2Panel pnlContainer;
        private Guna.UI2.WinForms.Guna2Panel pnlMainContent;
        private Guna.UI2.WinForms.Guna2PictureBox guna2PictureBox1;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel2;
        private Guna.UI2.WinForms.Guna2Panel pnlTopBar;
        private Guna.UI2.WinForms.Guna2ControlBox btnClose;
        private Guna.UI2.WinForms.Guna2ControlBox btnMini;
        private Timer timerDateTime;
        private Guna.UI2.WinForms.Guna2DragControl guna2DragControl1;
    }
	}

