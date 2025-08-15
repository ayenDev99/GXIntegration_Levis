namespace GXIntegration_Levis.Views
{
	partial class InboundPage
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
			this.inventoryButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// inventoryButton
			// 
			this.inventoryButton.Location = new System.Drawing.Point(564, 324);
			this.inventoryButton.Name = "inventoryButton";
			this.inventoryButton.Size = new System.Drawing.Size(128, 23);
			this.inventoryButton.TabIndex = 0;
			this.inventoryButton.Text = "Test Inventory";
			this.inventoryButton.UseVisualStyleBackColor = true;
			this.inventoryButton.Click += new System.EventHandler(this.inventoryButton_Click);
			// 
			// InboundPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.inventoryButton);
			this.Name = "InboundPage";
			this.Size = new System.Drawing.Size(805, 494);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button inventoryButton;
	}
}
