namespace RvtVa3c
{
    partial class ParameterFilter
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
            this.checkUncheck = new System.Windows.Forms.Button();
            this.export_button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // checkUncheck
            // 
            this.checkUncheck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkUncheck.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkUncheck.Location = new System.Drawing.Point(12, 228);
            this.checkUncheck.Name = "checkUncheck";
            this.checkUncheck.Size = new System.Drawing.Size(184, 33);
            this.checkUncheck.TabIndex = 4;
            this.checkUncheck.Text = "Check / Uncheck All";
            this.checkUncheck.UseVisualStyleBackColor = true;
            this.checkUncheck.Click += new System.EventHandler(this.checkUncheck_Click);
            // 
            // export_button
            // 
            this.export_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.export_button.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.export_button.Location = new System.Drawing.Point(210, 228);
            this.export_button.Name = "export_button";
            this.export_button.Size = new System.Drawing.Size(197, 33);
            this.export_button.TabIndex = 5;
            this.export_button.Text = "EXPORT";
            this.export_button.UseVisualStyleBackColor = true;
            this.export_button.Click += new System.EventHandler(this.export_button_Click);
            // 
            // ParameterFilter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(419, 273);
            this.ControlBox = false;
            this.Controls.Add(this.export_button);
            this.Controls.Add(this.checkUncheck);
            this.Name = "ParameterFilter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ParameterFilter";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button checkUncheck;
        private System.Windows.Forms.Button export_button;



    }
}