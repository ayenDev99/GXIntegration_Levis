using System;
using System.Drawing;
using System.Windows.Forms;

namespace GXIntegration_Levis.Views
{

		partial class HomePage
		{
			private System.ComponentModel.IContainer components = null;
			private Label labelTitle;
			private Label labelSubtitle;

			protected override void Dispose(bool disposing)
			{
				if (disposing && (components != null))
					components.Dispose();

				base.Dispose(disposing);
			}

			private void InitializeComponent()
			{
			this.labelTitle = new System.Windows.Forms.Label();
			this.labelSubtitle = new System.Windows.Forms.Label();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// labelTitle
			// 
			this.labelTitle.AutoSize = true;
			this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
			this.labelTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(120)))));
			this.labelTitle.Location = new System.Drawing.Point(379, 201);
			this.labelTitle.Name = "labelTitle";
			this.labelTitle.Size = new System.Drawing.Size(372, 37);
			this.labelTitle.TabIndex = 0;
			this.labelTitle.Text = "Retail Pro Prism and S4 ERP";
			// 
			// labelSubtitle
			// 
			this.labelSubtitle.AutoSize = true;
			this.labelSubtitle.Font = new System.Drawing.Font("Segoe UI", 12F);
			this.labelSubtitle.ForeColor = System.Drawing.Color.DimGray;
			this.labelSubtitle.Location = new System.Drawing.Point(420, 238);
			this.labelSubtitle.Name = "labelSubtitle";
			this.labelSubtitle.Size = new System.Drawing.Size(298, 21);
			this.labelSubtitle.TabIndex = 1;
			this.labelSubtitle.Text = "Welcome to the integration control center";
			// 
			// pictureBox2
			// 
			this.pictureBox2.Image = global::GXIntegration_Levis.Properties.Resources.retailpro_logo;
			this.pictureBox2.Location = new System.Drawing.Point(757, 392);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(100, 50);
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBox2.TabIndex = 3;
			this.pictureBox2.TabStop = false;
			this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::GXIntegration_Levis.Properties.Resources.levis_logo;
			this.pictureBox1.Location = new System.Drawing.Point(413, 9);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(292, 221);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
			// 
			// HomePage
			// 
			this.BackColor = System.Drawing.SystemColors.Menu;
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(this.labelTitle);
			this.Controls.Add(this.labelSubtitle);
			this.Controls.Add(this.pictureBox1);
			this.Name = "HomePage";
			this.Size = new System.Drawing.Size(900, 500);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		private PictureBox pictureBox1;
		private PictureBox pictureBox2;
	}

	
}
