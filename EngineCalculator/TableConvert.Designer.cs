namespace EngineCalculator {
    partial class TableConvert {
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
            this.tbAirTemp = new System.Windows.Forms.TextBox();
            this.btnRecalculate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.Grid = new EngineCalculator.ReoGridControlHAAAX();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbAirTemp
            // 
            this.tbAirTemp.Location = new System.Drawing.Point(199, 12);
            this.tbAirTemp.Name = "tbAirTemp";
            this.tbAirTemp.Size = new System.Drawing.Size(100, 22);
            this.tbAirTemp.TabIndex = 1;
            this.tbAirTemp.Text = "30";
            // 
            // btnRecalculate
            // 
            this.btnRecalculate.Location = new System.Drawing.Point(305, 12);
            this.btnRecalculate.Name = "btnRecalculate";
            this.btnRecalculate.Size = new System.Drawing.Size(115, 23);
            this.btnRecalculate.TabIndex = 2;
            this.btnRecalculate.Text = "Recalculate";
            this.btnRecalculate.UseVisualStyleBackColor = true;
            this.btnRecalculate.Click += new System.EventHandler(this.btnRecalculate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Ambient Temperature";
            // 
            // Grid
            // 
            this.Grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Grid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.Grid.ColumnHeaderContextMenuStrip = null;
            this.Grid.LeadHeaderContextMenuStrip = null;
            this.Grid.Location = new System.Drawing.Point(15, 75);
            this.Grid.Name = "Grid";
            this.Grid.RowHeaderContextMenuStrip = null;
            this.Grid.Script = null;
            this.Grid.SheetTabContextMenuStrip = null;
            this.Grid.SheetTabNewButtonVisible = true;
            this.Grid.SheetTabVisible = true;
            this.Grid.SheetTabWidth = 60;
            this.Grid.ShowScrollEndSpacing = true;
            this.Grid.Size = new System.Drawing.Size(1842, 963);
            this.Grid.TabIndex = 0;
            this.Grid.Text = "reoGridControl1";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(426, 12);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(115, 23);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save out.emubt";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // TableConvert
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1869, 1050);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRecalculate);
            this.Controls.Add(this.tbAirTemp);
            this.Controls.Add(this.Grid);
            this.Name = "TableConvert";
            this.Text = "TableConvert";
            this.Load += new System.EventHandler(this.TableConvert_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ReoGridControlHAAAX Grid;
        private System.Windows.Forms.TextBox tbAirTemp;
        private System.Windows.Forms.Button btnRecalculate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSave;
    }
}