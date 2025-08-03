using System;
using System.Drawing;
using System.Windows.Forms;

namespace GXIntegration_Levis
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
			this.SuspendLayout();
			// 
			// labelTitle
			// 
			this.labelTitle.AutoSize = true;
			this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
			this.labelTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(120)))));
			this.labelTitle.Location = new System.Drawing.Point(340, 116);
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
			this.labelSubtitle.Location = new System.Drawing.Point(380, 177);
			this.labelSubtitle.Name = "labelSubtitle";
			this.labelSubtitle.Size = new System.Drawing.Size(298, 21);
			this.labelSubtitle.TabIndex = 1;
			this.labelSubtitle.Text = "Welcome to the integration control center";
			// 
			// HomePage
			// 
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.labelTitle);
			this.Controls.Add(this.labelSubtitle);
			this.Name = "HomePage";
			this.Size = new System.Drawing.Size(800, 500);
			this.ResumeLayout(false);
			this.PerformLayout();

			}
		}

	
}
