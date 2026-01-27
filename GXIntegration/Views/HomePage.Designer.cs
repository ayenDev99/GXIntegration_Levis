using System;
using System.Drawing;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{

		partial class HomePage
		{
			private System.ComponentModel.IContainer components = null;
			private Label labelTitle;

			protected override void Dispose(bool disposing)
			{
				if (disposing && (components != null))
					components.Dispose();

				base.Dispose(disposing);
			}

			private void InitializeComponent()
			{
            this.labelTitle = new System.Windows.Forms.Label();
            this.logoPrism = new System.Windows.Forms.PictureBox();
            this.logoLevis = new System.Windows.Forms.PictureBox();
            this.lblPrismVr = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2Panel2 = new Guna.UI2.WinForms.Guna2Panel();
            ((System.ComponentModel.ISupportInitialize)(this.logoPrism)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logoLevis)).BeginInit();
            this.guna2Panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.labelTitle.Location = new System.Drawing.Point(392, 273);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(316, 32);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Retail Pro Prism and S4 ERP";
            // 
            // logoPrism
            // 
            this.logoPrism.Image = global::GXIntegration_Levis.Properties.Resources.retailpro_logo;
            this.logoPrism.Location = new System.Drawing.Point(774, 397);
            this.logoPrism.Name = "logoPrism";
            this.logoPrism.Size = new System.Drawing.Size(100, 50);
            this.logoPrism.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPrism.TabIndex = 3;
            this.logoPrism.TabStop = false;
            this.logoPrism.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // logoLevis
            // 
            this.logoLevis.Image = global::GXIntegration_Levis.Properties.Resources.levis_logo;
            this.logoLevis.Location = new System.Drawing.Point(379, 29);
            this.logoLevis.Name = "logoLevis";
            this.logoLevis.Size = new System.Drawing.Size(341, 276);
            this.logoLevis.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoLevis.TabIndex = 2;
            this.logoLevis.TabStop = false;
            this.logoLevis.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // lblPrismVr
            // 
            this.lblPrismVr.BackColor = System.Drawing.Color.Transparent;
            this.lblPrismVr.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPrismVr.Location = new System.Drawing.Point(797, 451);
            this.lblPrismVr.Name = "lblPrismVr";
            this.lblPrismVr.Size = new System.Drawing.Size(77, 16);
            this.lblPrismVr.TabIndex = 4;
            this.lblPrismVr.Text = "vr. 1.14.6.1231 ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label1.Location = new System.Drawing.Point(430, 305);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(251, 32);
            this.label1.TabIndex = 5;
            this.label1.Text = "Upgraded Integration";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.Controls.Add(this.guna2Panel2);
            this.guna2Panel1.Controls.Add(this.logoPrism);
            this.guna2Panel1.Controls.Add(this.lblPrismVr);
            this.guna2Panel1.Controls.Add(this.labelTitle);
            this.guna2Panel1.Controls.Add(this.label1);
            this.guna2Panel1.Controls.Add(this.logoLevis);
            this.guna2Panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.guna2Panel1.Location = new System.Drawing.Point(0, 0);
            this.guna2Panel1.Margin = new System.Windows.Forms.Padding(0);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.Size = new System.Drawing.Size(900, 494);
            this.guna2Panel1.TabIndex = 6;
            // 
            // guna2Panel2
            // 
            this.guna2Panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.guna2Panel2.Location = new System.Drawing.Point(0, 0);
            this.guna2Panel2.Name = "guna2Panel2";
            this.guna2Panel2.Size = new System.Drawing.Size(200, 494);
            this.guna2Panel2.TabIndex = 6;
            // 
            // HomePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.Menu;
            this.Controls.Add(this.guna2Panel1);
            this.Name = "HomePage";
            this.Size = new System.Drawing.Size(900, 494);
            this.Load += new System.EventHandler(this.HomePage_Load);
            ((System.ComponentModel.ISupportInitialize)(this.logoPrism)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.logoLevis)).EndInit();
            this.guna2Panel1.ResumeLayout(false);
            this.guna2Panel1.PerformLayout();
            this.ResumeLayout(false);

			}

		private PictureBox logoLevis;
		private PictureBox logoPrism;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblPrismVr;
        private Label label1;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel2;
    }

	
}
