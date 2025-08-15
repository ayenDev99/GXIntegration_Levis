namespace GXIntegration_Levis.Views
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
			this.SuspendLayout();
			// 
			// testInventorySnapshot
			// 
			this.testInventorySnapshot.Location = new System.Drawing.Point(0, 0);
			this.testInventorySnapshot.Name = "testInventorySnapshot";
			this.testInventorySnapshot.Size = new System.Drawing.Size(75, 23);
			this.testInventorySnapshot.TabIndex = 1;
			// 
			// OutboundPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.testInventorySnapshot);
			this.Name = "OutboundPage";
			this.Size = new System.Drawing.Size(1108, 494);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button testInventorySnapshot;
	}
}
