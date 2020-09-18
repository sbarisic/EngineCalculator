namespace ELM327_LogConverter {
	partial class ConvertDialog {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.label1 = new System.Windows.Forms.Label();
			this.tbWeight = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.tbGear = new System.Windows.Forms.TextBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(164, 20);
			this.label1.TabIndex = 0;
			this.label1.Text = "Total occupant weight";
			// 
			// tbWeight
			// 
			this.tbWeight.Location = new System.Drawing.Point(197, 9);
			this.tbWeight.Name = "tbWeight";
			this.tbWeight.Size = new System.Drawing.Size(320, 20);
			this.tbWeight.TabIndex = 1;
			this.tbWeight.Text = "70";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(12, 39);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(106, 20);
			this.label2.TabIndex = 2;
			this.label2.Text = "Gearbox gear";
			// 
			// tbGear
			// 
			this.tbGear.Location = new System.Drawing.Point(197, 39);
			this.tbGear.Name = "tbGear";
			this.tbGear.Size = new System.Drawing.Size(320, 20);
			this.tbGear.TabIndex = 3;
			this.tbGear.Text = "2";
			// 
			// btnOk
			// 
			this.btnOk.Location = new System.Drawing.Point(354, 111);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(163, 34);
			this.btnOk.TabIndex = 4;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// ConvertDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(529, 157);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.tbGear);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.tbWeight);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConvertDialog";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "ConvertDialog";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbWeight;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbGear;
		private System.Windows.Forms.Button btnOk;
	}
}