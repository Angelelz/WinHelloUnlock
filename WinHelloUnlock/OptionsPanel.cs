using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;

namespace WinHelloUnlock
{
    public partial class OptionsPanel : UserControl
    {
        public static string db = WinHelloUnlockExt.dbName;
        private static string onOpenEnabled = "";
        bool enabled = true;
        private static bool first = false;

        /// <summary>
        /// Options Panel class with all WinHelloUnlock options
        /// </summary>
        public OptionsPanel()
        {
            InitializeComponent();
            db = WinHelloUnlockExt.dbName;
            RefreshOptions();
            checkBox.Checked = enabled;
            checkBox1.Checked = WinHelloUnlockExt.LockAfterAutoType;
        }

        /// <summary>Register for the FormClosing event.</summary>
		protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (ParentForm != null)
            {
                // Save the settings on FormClosing.
                ParentForm.FormClosing += delegate (object sender2, FormClosingEventArgs e2)
                {
                    if (ParentForm.DialogResult == DialogResult.OK)
                    {
                        WinHelloUnlockExt.LockAfterAutoType = checkBox1.Checked;
                        if (checkBox1.Checked) WinHelloUnlockExt.database.CustomData.Set(WinHelloUnlockExt.ProductName + "AT", "true");
                        else WinHelloUnlockExt.database.CustomData.Set(WinHelloUnlockExt.ProductName + "AT", "false");

                        if (onOpenEnabled != checkBox.Checked.ToString())
                        {
                            if (checkBox.Checked)
                            {
                                WinHelloUnlockExt.database.CustomData.Set(WinHelloUnlockExt.ProductName, "true");
                                WinHelloUnlockExt.enablePlugin = true;
                                
                            }
                            else
                            {
                                if (deleteButton.Enabled) UWPLibrary.DeleteHelloData(WinHelloUnlockExt.dbName);
                                WinHelloUnlockExt.database.CustomData.Set(WinHelloUnlockExt.ProductName, "false");
                                WinHelloUnlockExt.enablePlugin = false;
                            }
                            

                        }

                        WinHelloUnlockExt.database.Modified = true;
                        try { WinHelloUnlockExt.database.Save(null); }
                        catch { }

                        //WinHelloUnlockExt.database.Save(null);
                    }
                };
            }

        }

        /// <summary>Opens the readme.</summary>
		private void HelpButton_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Angelelz/WinHelloUnlock/blob/master/ReadMe.md");
        }

        /// <summary>Creates WinHelloUnock data when the Create Button is clicked.</summary>
        private async void CreateButton_Click(object sender, EventArgs e)
        {
            await UWPLibrary.CreateHelloData(WinHelloUnlockExt.dbName);
            RefreshOptions();
        }

        /// <summary>Deletes WinHelloUnock data when the Delete Button is clicked.</summary>
        private void DeleteteButton_Click(object sender, EventArgs e)
        {
            UWPLibrary.DeleteHelloData(WinHelloUnlockExt.dbName);
            RefreshOptions();
        }

        /// <summary>Updates the form when the CheckBox is clicked.</summary>
        private void CheckBox_Change(object sender, EventArgs e)
        {
            bool check = checkBox.Checked;
            first = Task.Run(() => UWPLibrary.FirstTime(db)).Result;

            checkBox1.Enabled = check;
            createButton.Enabled = first && check;
            deleteButton.Enabled = !first;
            label1.Text = "";
            if (!check && !first) label1.Text = "WinHelloUnlock Data will be deleted";
        }

        private void RefreshOptions()
        {
            string text = "";
            string path = WinHelloUnlockExt.database.IOConnectionInfo.Path;
            string fileName = Path.GetFileName(path);
            if (path.Length > 50)
                text = path.Substring(0, Math.Max(0,45 - fileName.Length)) + " ... " + fileName;
            else text = path;
            settingsGroupBox.Text = "WinHelloUnlock Settings for " + text;

            first = Task.Run(() => UWPLibrary.FirstTime(db)).Result;
            onOpenEnabled = WinHelloUnlockExt.database.CustomData.Get(WinHelloUnlockExt.ProductName);

            enabled = true;
            if (onOpenEnabled == "false")
                enabled = false;

            if (enabled && !first) infoLabel.Text = "WinHelloUnlock is configured for this Database";
            else infoLabel.Text = "WinHelloUnlock is NOT configured for this Database";

            //checkBox.Checked = enabled;
            createButton.Enabled = first && (enabled || checkBox.Checked);
            deleteButton.Enabled = !first;
            //checkBox1.Checked = WinHelloUnlockExt.LockAfterAutoType;
            checkBox1.Enabled = enabled;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
