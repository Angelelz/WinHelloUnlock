namespace WinHelloUnlock
{
    partial class OptionsPanel
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
            this.helpButton = new System.Windows.Forms.Button();
            this.infoLabel = new System.Windows.Forms.Label();
            this.deleteButton = new System.Windows.Forms.Button();
            this.createButton = new System.Windows.Forms.Button();
            this.settingsGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBox = new System.Windows.Forms.CheckBox();
            this.settingsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // helpButton
            // 
            this.helpButton.AccessibleName = "";
            this.helpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.helpButton.Location = new System.Drawing.Point(1154, 577);
            this.helpButton.Margin = new System.Windows.Forms.Padding(7);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(100, 51);
            this.helpButton.TabIndex = 3;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.HelpButton_Click);
            // 
            // infoLabel
            // 
            this.infoLabel.AutoSize = true;
            this.infoLabel.Location = new System.Drawing.Point(16, 53);
            this.infoLabel.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(573, 29);
            this.infoLabel.TabIndex = 5;
            this.infoLabel.Text = "WinHelloUnlock is NOT configured for this Database";
            // 
            // deleteButton
            // 
            this.deleteButton.AccessibleName = "";
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.deleteButton.Location = new System.Drawing.Point(670, 246);
            this.deleteButton.Margin = new System.Windows.Forms.Padding(7);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(568, 51);
            this.deleteButton.TabIndex = 6;
            this.deleteButton.Text = "Delete WinHelloUnlock data for this Database";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.DeleteteButton_Click);
            // 
            // createButton
            // 
            this.createButton.AccessibleName = "";
            this.createButton.Location = new System.Drawing.Point(21, 246);
            this.createButton.Margin = new System.Windows.Forms.Padding(7);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(568, 51);
            this.createButton.TabIndex = 7;
            this.createButton.Text = "Create WinHelloUnlock data for this Database";
            this.createButton.UseVisualStyleBackColor = true;
            this.createButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // settingsGroupBox
            // 
            this.settingsGroupBox.Controls.Add(this.label1);
            this.settingsGroupBox.Controls.Add(this.checkBox);
            this.settingsGroupBox.Controls.Add(this.createButton);
            this.settingsGroupBox.Controls.Add(this.deleteButton);
            this.settingsGroupBox.Controls.Add(this.infoLabel);
            this.settingsGroupBox.Location = new System.Drawing.Point(16, 32);
            this.settingsGroupBox.Margin = new System.Windows.Forms.Padding(7);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Padding = new System.Windows.Forms.Padding(7);
            this.settingsGroupBox.Size = new System.Drawing.Size(1260, 339);
            this.settingsGroupBox.TabIndex = 10;
            this.settingsGroupBox.TabStop = false;
            this.settingsGroupBox.Text = "WinHelloUnlock Settings";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(52, 149);
            this.label1.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 29);
            this.label1.TabIndex = 9;
            // 
            // checkBox
            // 
            this.checkBox.AutoSize = true;
            this.checkBox.Location = new System.Drawing.Point(21, 109);
            this.checkBox.Margin = new System.Windows.Forms.Padding(7);
            this.checkBox.Name = "checkBox";
            this.checkBox.Size = new System.Drawing.Size(484, 33);
            this.checkBox.TabIndex = 8;
            this.checkBox.Text = "Enable WinHelloUnlock for this Database";
            this.checkBox.UseVisualStyleBackColor = true;
            this.checkBox.CheckedChanged += new System.EventHandler(this.CheckBox_Change);
            // 
            // OptionsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.settingsGroupBox);
            this.Controls.Add(this.helpButton);
            this.Margin = new System.Windows.Forms.Padding(7);
            this.Name = "OptionsPanel";
            this.Size = new System.Drawing.Size(1297, 665);
            this.settingsGroupBox.ResumeLayout(false);
            this.settingsGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.CheckBox checkBox;
        private System.Windows.Forms.Label label1;
    }
}
