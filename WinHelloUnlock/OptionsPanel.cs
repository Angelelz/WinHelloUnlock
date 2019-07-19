using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace WinHelloUnlock
{
    public partial class OptionsPanel : UserControl
    {
        public static string db = WinHelloUnlockExt.dbName;
        private static string onOpenEnabled = "";
        public OptionsPanel()
        {
            InitializeComponent();
            onOpenEnabled = WinHelloUnlockExt.database.CustomData.Get(WinHelloUnlockExt.ProductName);
            Task<bool> firstTime = Task.Run(async () =>
            {
                bool f = await UWPLibrary.FirstTime(db);
                return f;
            });

            bool enabled = true;
            if (WinHelloUnlockExt.database.CustomData.Get(WinHelloUnlockExt.ProductName) == "false")
                enabled = false;
            
            if (firstTime.Result)
            {
                infoLabel.Text = "WinHelloUnlock is NOT configured for this Database";
                deleteButton.Enabled = false;
                if (enabled) checkBox.Checked = true;
                else
                {
                    checkBox.Checked = false;
                    createButton.Enabled = false;
                }
            }
            else
            {
                infoLabel.Text = "WinHelloUnlock is configured for this Database";
                createButton.Enabled = false;
                if (enabled) checkBox.Checked = true;
                else
                {
                    checkBox.Checked = false;
                    deleteButton.Enabled = false;
                }
            }
        }

        /// <summary>Register for the FormClosing event.</summary>
		protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Task<bool> firstTime = Task.Run(async () =>
            {
                bool f = await UWPLibrary.FirstTime(db);
                return f;
            });
            bool first = firstTime.Result;
            if (ParentForm != null)
            {
                // Save the settings on FormClosing.
                ParentForm.FormClosing += delegate (object sender2, FormClosingEventArgs e2)
                {
                    if (ParentForm.DialogResult == DialogResult.OK)
                    {
                        if (onOpenEnabled != checkBox.Checked.ToString())
                        {
                            if (checkBox.Checked)
                            {
                                WinHelloUnlockExt.database.CustomData.Set(WinHelloUnlockExt.ProductName, "true");
                                WinHelloUnlockExt.enablePlugin = true;
                                WinHelloUnlockExt.database.Modified = true;
                                try { WinHelloUnlockExt.database.Save(null); }
                                catch { }
                            }
                            else
                            {
                                if (deleteButton.Enabled) UWPLibrary.DeleteHelloData(WinHelloUnlockExt.dbName);
                                WinHelloUnlockExt.database.CustomData.Set(WinHelloUnlockExt.ProductName, "false");
                                WinHelloUnlockExt.enablePlugin = false;
                                WinHelloUnlockExt.database.Modified = true;
                                try { WinHelloUnlockExt.database.Save(null); }
                                catch { }
                            }
                        }
                        //WinHelloUnlockExt.database.Save(null);
                    }
                };
            }

        }

        /// <summary>Opens the readme.</summary>
		private void HelpButton_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/KN4CK3R/KeePassQuickUnlock/blob/master/README.md");
        }

        private async void CreateButton_Click(object sender, EventArgs e)
        {
            bool check = checkBox.Checked;
            bool first = await UWPLibrary.FirstTime(db);
            bool created = await UWPLibrary.CreateHelloData(WinHelloUnlockExt.dbName);
            createButton.Enabled = !created && check;
            deleteButton.Enabled = !createButton.Enabled;
            if (created) infoLabel.Text = "WinHelloUnlock is configured for this Database";
        }

        private async void DeleteteButton_Click(object sender, EventArgs e)
        {
            bool check = checkBox.Checked;
            bool first = await UWPLibrary.FirstTime(db);
            UWPLibrary.DeleteHelloData(WinHelloUnlockExt.dbName);
            bool first2 = await UWPLibrary.FirstTime(db);
            deleteButton.Enabled = !first2;
            createButton.Enabled = first2 && check;
            if (first2) infoLabel.Text = "WinHelloUnlock is NOT configured for this Database";
        }

        private void CheckBox_Change(object sender, EventArgs e)
        {
            Task<bool> firstTime = Task.Run(async () =>
            {
                bool f = await UWPLibrary.FirstTime(db);
                return f;
            });
            bool check = checkBox.Checked;
            bool first = firstTime.Result;

            createButton.Enabled = first && check;
            deleteButton.Enabled = !first;
            label1.Text = "";
            if (!check && !first) label1.Text = "WinHelloUnlock Data will be deleted";
        }

    }
}
