namespace GXIntegration_Levis
{
	partial class OutboundPage
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.testInventorySnapshot = new System.Windows.Forms.Button();
			this.testInTransit = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// testInventorySnapshot
			// 
			this.testInventorySnapshot.Cursor = System.Windows.Forms.Cursors.Hand;
			this.testInventorySnapshot.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.testInventorySnapshot.Location = new System.Drawing.Point(1029, 95);
			this.testInventorySnapshot.Name = "testInventorySnapshot";
			this.testInventorySnapshot.Size = new System.Drawing.Size(65, 18);
			this.testInventorySnapshot.TabIndex = 0;
			this.testInventorySnapshot.Text = "Download";
			this.testInventorySnapshot.UseVisualStyleBackColor = true;
			this.testInventorySnapshot.Click += new System.EventHandler(this.testInventorySnapshot_Click);
			// 
			// testInTransit
			// 
			this.testInTransit.Cursor = System.Windows.Forms.Cursors.Hand;
			this.testInTransit.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.testInTransit.Location = new System.Drawing.Point(1029, 116);
			this.testInTransit.Name = "testInTransit";
			this.testInTransit.Size = new System.Drawing.Size(65, 18);
			this.testInTransit.TabIndex = 1;
			this.testInTransit.Text = "Download";
			this.testInTransit.UseVisualStyleBackColor = true;
			this.testInTransit.Click += new System.EventHandler(this.testInTransit_Click);
			// 
			// OutboundPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.testInTransit);
			this.Controls.Add(this.testInventorySnapshot);
			this.Name = "OutboundPage";
			this.Size = new System.Drawing.Size(1108, 494);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button testInventorySnapshot;
		private System.Windows.Forms.Button testInTransit;
	}
}
