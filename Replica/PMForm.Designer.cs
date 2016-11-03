namespace Replica
{
    partial class PMForm
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
            this.txtBoxLoadedScript = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtBoxLoadedScript
            // 
            this.txtBoxLoadedScript.Enabled = false;
            this.txtBoxLoadedScript.Location = new System.Drawing.Point(12, 12);
            this.txtBoxLoadedScript.Multiline = true;
            this.txtBoxLoadedScript.Name = "txtBoxLoadedScript";
            this.txtBoxLoadedScript.Size = new System.Drawing.Size(100, 106);
            this.txtBoxLoadedScript.TabIndex = 0;
            // 
            // PMForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(915, 262);
            this.Controls.Add(this.txtBoxLoadedScript);
            this.Name = "PMForm";
            this.Text = "PMForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtBoxLoadedScript;
    }
}