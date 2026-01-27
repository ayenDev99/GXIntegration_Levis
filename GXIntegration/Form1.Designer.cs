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
			this.Elipse_Form = new Guna.UI2.WinForms.Guna2Elipse(this.components);
			this.SideBar = new Guna.UI2.WinForms.Guna2Panel();
			this.Guna2Panel5 = new Guna.UI2.WinForms.Guna2Panel();
			this.Outbound_Button = new Guna.UI2.WinForms.Guna2Button();
			this.Guna2Panel8 = new Guna.UI2.WinForms.Guna2Panel();
			this.Inbound_Button = new Guna.UI2.WinForms.Guna2Button();
			this.Guna2Panel4 = new Guna.UI2.WinForms.Guna2Panel();
			this.Configuration_Button = new Guna.UI2.WinForms.Guna2Button();
			this.Guna2Panel3 = new Guna.UI2.WinForms.Guna2Panel();
			this.Home_Button = new Guna.UI2.WinForms.Guna2Button();
			this.Guna2Panel2 = new Guna.UI2.WinForms.Guna2Panel();
			this.Guna2Panel9 = new Guna.UI2.WinForms.Guna2Panel();
			this.versionNo = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.DragControl_Form = new Guna.UI2.WinForms.Guna2DragControl(this.components);
			this.Timer_Sidebar_Menu = new System.Windows.Forms.Timer(this.components);
			this.MainContentPanel = new System.Windows.Forms.Panel();
			this.Close_Button = new Guna.UI2.WinForms.Guna2ImageButton();
			this.SideBar.SuspendLayout();
			this.Guna2Panel5.SuspendLayout();
			this.Guna2Panel8.SuspendLayout();
			this.Guna2Panel4.SuspendLayout();
			this.Guna2Panel3.SuspendLayout();
			this.Guna2Panel2.SuspendLayout();
			this.Guna2Panel9.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.MainContentPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// Elipse_Form
			// 
			//this.Elipse_Form.Radius = 9;
			this.Elipse_Form.TargetControl = this;
			// 
			// SideBar
			// 
			this.SideBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(0)))), ((int)(((byte)(102)))));
			this.SideBar.Controls.Add(this.Guna2Panel5);
			this.SideBar.Controls.Add(this.Guna2Panel8);
			this.SideBar.Controls.Add(this.Guna2Panel4);
			this.SideBar.Controls.Add(this.Guna2Panel3);
			this.SideBar.Controls.Add(this.Guna2Panel2);
			this.SideBar.Dock = System.Windows.Forms.DockStyle.Left;
			this.SideBar.Location = new System.Drawing.Point(0, 0);
			this.SideBar.MaximumSize = new System.Drawing.Size(217, 494);
			this.SideBar.MinimumSize = new System.Drawing.Size(55, 494);
			this.SideBar.Name = "SideBar";
			this.SideBar.Size = new System.Drawing.Size(217, 494);
			this.SideBar.TabIndex = 0;
			//this.SideBar.Paint += new System.Windows.Forms.PaintEventHandler(this.GunaPanel1_Paint);
            this.SideBar.Paint += new PaintEventHandler(this.SideBar_Paint);

            // 
            // Guna2 Outbound Button
            Outbound_Button.Animated = true; // smooth hover/press animations
            Outbound_Button.FillColor = Color.Transparent; // BaseColor equivalent
            Outbound_Button.ForeColor = Color.White;
            Outbound_Button.Font = new Font("Segoe UI", 8F);
            Outbound_Button.Image = ((Image)(resources.GetObject("Outbound_Button.Image")));
            Outbound_Button.ImageSize = new Size(17, 17);
            Outbound_Button.Location = new Point(7, 8);
            Outbound_Button.Name = "Outbound_Button";
            Outbound_Button.Size = new Size(203, 40);
            Outbound_Button.Text = "Outbound";
            Outbound_Button.TextOffset = new Point(15, 0); // X/Y text offset
            Outbound_Button.Cursor = Cursors.Hand;

            // Hover effects
            Outbound_Button.HoverState.FillColor = Color.Transparent;
            Outbound_Button.HoverState.ForeColor = Color.Silver;
            Outbound_Button.HoverState.Image = null;

            // Pressed effect
            Outbound_Button.PressedColor = Color.White;

            // Click event
            Outbound_Button.Click += new EventHandler(this.Outbound_Button_Click);

            // Add to form or parent panel
            this.Controls.Add(Outbound_Button);

            // 
            // Guna2Panel5
            // 
            this.Guna2Panel5.Controls.Add(this.Outbound_Button);
            this.Guna2Panel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.Guna2Panel5.Location = new System.Drawing.Point(0, 252);
            this.Guna2Panel5.Name = "Guna2Panel5";
            this.Guna2Panel5.Size = new System.Drawing.Size(217, 55);
            this.Guna2Panel5.TabIndex = 3;

            // 
            // Guna2 Inbound Button
            Inbound_Button.Animated = true; // enables smooth hover/press animations
            Inbound_Button.FillColor = Color.Transparent; // equivalent to BaseColor
            Inbound_Button.ForeColor = Color.White;
            Inbound_Button.Font = new Font("Segoe UI", 8F);
            Inbound_Button.Image = ((Image)(resources.GetObject("Inbound_Button.Image")));
            Inbound_Button.ImageSize = new Size(17, 17);
            Inbound_Button.Location = new Point(7, 8);
            Inbound_Button.Name = "Inbound_Button";
            Inbound_Button.Size = new Size(203, 40);
            Inbound_Button.Text = "Inbound";
            Inbound_Button.TextOffset = new Point(15, 0); // X/Y text offset
            Inbound_Button.Cursor = Cursors.Hand;

            // Hover effects
            Inbound_Button.HoverState.FillColor = Color.Transparent;
            Inbound_Button.HoverState.ForeColor = Color.Silver;
            Inbound_Button.HoverState.Image = null;

            // Pressed effect
            Inbound_Button.PressedColor = Color.White;

            // Click event
            Inbound_Button.Click += new EventHandler(this.Inbound_Button_Click);

            // Add to form or parent panel
            this.Controls.Add(Inbound_Button);

            // 
            // Guna2Panel8
            // 
            this.Guna2Panel8.Controls.Add(this.Inbound_Button);
            this.Guna2Panel8.Dock = System.Windows.Forms.DockStyle.Top;
            this.Guna2Panel8.Location = new System.Drawing.Point(0, 197);
            this.Guna2Panel8.Name = "Guna2Panel8";
            this.Guna2Panel8.Size = new System.Drawing.Size(217, 55);
            this.Guna2Panel8.TabIndex = 5;

            // 
            // Configuration_Button
            // 
            Configuration_Button.Animated = true;
            Configuration_Button.FillColor = System.Drawing.Color.Transparent; // equivalent to BaseColor
            Configuration_Button.ForeColor = System.Drawing.Color.White;
            Configuration_Button.Font = new System.Drawing.Font("Segoe UI", 8F);
            Configuration_Button.Image = ((System.Drawing.Image)(resources.GetObject("Configuration_Button.Image")));
            Configuration_Button.ImageSize = new System.Drawing.Size(17, 17);
            Configuration_Button.Location = new System.Drawing.Point(7, 7);
            Configuration_Button.Name = "Configuration_Button";
            Configuration_Button.Size = new System.Drawing.Size(203, 40);
            Configuration_Button.Text = "Configuration";
            Configuration_Button.TextOffset = new System.Drawing.Point(15, 0); // Text offset in Guna2
            Configuration_Button.HoverState.FillColor = System.Drawing.Color.Transparent;
            Configuration_Button.HoverState.ForeColor = System.Drawing.Color.Silver;
            Configuration_Button.HoverState.Image = null;
            Configuration_Button.PressedColor = System.Drawing.Color.White;
            Configuration_Button.Cursor = Cursors.Hand;

            // Click event
            Configuration_Button.Click += new System.EventHandler(this.Configuration_Button_Click);

            // Add to a panel or form
            this.Controls.Add(Configuration_Button);

            // 
            // Guna2Panel4
            // 
            this.Guna2Panel4.Controls.Add(this.Configuration_Button);
            this.Guna2Panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.Guna2Panel4.Location = new System.Drawing.Point(0, 142);
            this.Guna2Panel4.Name = "Guna2Panel4";
            this.Guna2Panel4.Size = new System.Drawing.Size(217, 55);
            this.Guna2Panel4.TabIndex = 2;

            // 
            // Home_Button
            // 
            Home_Button.Animated = true;
            Home_Button.FillColor = System.Drawing.Color.Transparent; // equivalent to BaseColor
            Home_Button.ForeColor = System.Drawing.Color.White;
            Home_Button.Font = new System.Drawing.Font("Segoe UI", 8F);
            Home_Button.Image = ((System.Drawing.Image)(resources.GetObject("Home_Button.Image")));
            Home_Button.ImageSize = new System.Drawing.Size(17, 17);
            //Home_Button.Location = new System.Drawing.Point(7, 7);
            Home_Button.Name = "Home_Button";
            Home_Button.Size = new System.Drawing.Size(203, 40);
            Home_Button.Text = "Home";
            Home_Button.TextOffset = new System.Drawing.Point(15, 0); // Text offset in Guna2
            Home_Button.Cursor = Cursors.Hand;

            // Hover effects
            Home_Button.HoverState.FillColor = System.Drawing.Color.Transparent;
            Home_Button.HoverState.ForeColor = System.Drawing.Color.Silver;
            Home_Button.HoverState.Image = null;

            // Pressed effect
            Home_Button.PressedColor = System.Drawing.Color.White;

            // Click event
            Home_Button.Click += new System.EventHandler(this.Home_Button_Click);

            // Add to your form or panel
            this.Controls.Add(Home_Button);

            // 
            // Guna2Panel3
            // 
            this.Guna2Panel3.Controls.Add(this.Home_Button);
            this.Guna2Panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.Guna2Panel3.Location = new System.Drawing.Point(0, 87);
            this.Guna2Panel3.Name = "Guna2Panel3";
            this.Guna2Panel3.Size = new System.Drawing.Size(217, 55);
            this.Guna2Panel3.TabIndex = 1;

            // 
            // Guna2Panel2
            // 
            this.Guna2Panel2.Controls.Add(this.Guna2Panel9);
			this.Guna2Panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.Guna2Panel2.Location = new System.Drawing.Point(0, 0);
			this.Guna2Panel2.Name = "Guna2Panel2";
			this.Guna2Panel2.Size = new System.Drawing.Size(217, 87);
			this.Guna2Panel2.TabIndex = 0;
			this.Guna2Panel2.Paint += new PaintEventHandler(this.SideBar_Paint);
			// 
			// Guna2Panel9
			// 
			this.Guna2Panel9.Controls.Add(this.versionNo);
			this.Guna2Panel9.Controls.Add(this.pictureBox1);
			this.Guna2Panel9.Location = new System.Drawing.Point(0, 12);
			this.Guna2Panel9.Name = "Guna2Panel9";
			this.Guna2Panel9.Size = new System.Drawing.Size(213, 54);
			this.Guna2Panel9.TabIndex = 2;
			// 
			// versionNo
			// 
			this.versionNo.AutoSize = true;
			this.versionNo.Font = new System.Drawing.Font("Segoe UI", 7F);
			this.versionNo.ForeColor = System.Drawing.Color.White;
			this.versionNo.Location = new System.Drawing.Point(90, 39);
			this.versionNo.Name = "versionNo";
			this.versionNo.Size = new System.Drawing.Size(29, 12);
			this.versionNo.TabIndex = 2;
			this.versionNo.Text = "v1.0.0";
			this.versionNo.Click += new System.EventHandler(this.label1_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::GXIntegration_Levis.Properties.Resources.geniexlogo1;
			this.pictureBox1.Location = new System.Drawing.Point(61, 3);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(84, 29);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
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
			// MainContentPanel
			// 
			this.MainContentPanel.BackColor = System.Drawing.Color.White;
			this.MainContentPanel.Controls.Add(this.Close_Button);
			this.MainContentPanel.Location = new System.Drawing.Point(0, 0);
			this.MainContentPanel.Name = "MainContentPanel";
			this.MainContentPanel.Size = new System.Drawing.Size(900, 494);
			this.MainContentPanel.TabIndex = 3;
			this.MainContentPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.MainContentPanel_Paint);
            // 
            // Guna2 Close Button
            Close_Button.BackColor = Color.White; // background color
            Close_Button.Cursor = Cursors.Hand;
            Close_Button.Image = global::GXIntegration_Levis.Properties.Resources.multiply_48px__; // normal image
            Close_Button.ImageSize = new Size(16, 16);
            Close_Button.Location = new Point(869, 3);
            Close_Button.Name = "Close_Button";
            Close_Button.Size = new Size(28, 24);
            Close_Button.TabIndex = 1;

            // Hover image
            Close_Button.HoverState.Image = global::GXIntegration_Levis.Properties.Resources.multiply_48px_____;
            Close_Button.HoverState.ImageSize = new Size(16, 16); // optional, same as normal
            Close_Button.HoverState.Parent = Close_Button; // ensures hover effect applies

            // Click event
            Close_Button.Click += new EventHandler(this.Close_Button_Click);

            // Add to form or panel
            this.Controls.Add(Close_Button);

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(900, 494);
			this.Controls.Add(this.SideBar);
			this.Controls.Add(this.MainContentPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "k.//";
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.Load += new System.EventHandler(this.Form1_Load);
			this.SideBar.ResumeLayout(false);
			this.Guna2Panel5.ResumeLayout(false);
			this.Guna2Panel8.ResumeLayout(false);
			this.Guna2Panel4.ResumeLayout(false);
			this.Guna2Panel3.ResumeLayout(false);
			this.Guna2Panel2.ResumeLayout(false);
			this.Guna2Panel9.ResumeLayout(false);
			this.Guna2Panel9.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.MainContentPanel.ResumeLayout(false);
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
        private Guna.UI2.WinForms.Guna2Panel SideBar;
        private Guna.UI2.WinForms.Guna2Panel Guna2Panel3;
        private Guna.UI2.WinForms.Guna2Panel Guna2Panel2;
        private Guna.UI2.WinForms.Guna2ImageButton Close_Button;
        private Guna.UI2.WinForms.Guna2Button Inbound_Button;
        private Guna.UI2.WinForms.Guna2Panel Guna2Panel5;
        private Guna.UI2.WinForms.Guna2Button Outbound_Button;
        private Guna.UI2.WinForms.Guna2Panel Guna2Panel8;
        private Guna.UI2.WinForms.Guna2Panel Guna2Panel4;
        private Guna.UI2.WinForms.Guna2Button Configuration_Button;
        private Guna.UI2.WinForms.Guna2Button Home_Button;
        private Guna.UI2.WinForms.Guna2Panel Guna2Panel9;
        private Guna.UI2.WinForms.Guna2DragControl DragControl_Form;
        private System.Windows.Forms.Timer Timer_Sidebar_Menu;
		private PictureBox pictureBox1;
		private Label versionNo;
	}
	}

