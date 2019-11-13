using KeePass.Plugins;
using KeePass.Forms;
using KeePass.UI;
using System;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.Diagnostics;
using KeePassLib;
using KeePass.Util;
using KeePassLib.Utility;
using KeePassLib.Serialization;

namespace WinHelloUnlock
{
    public class WinHelloUnlockExt : Plugin
    {
        private static IPluginHost host = null;

        // Global settings and constants
        public const string ShortProductName = "HelloUnlock";
        public const string ProductName = "WinHelloUnlock";
        public static string dbName;
        public static PwDatabase database = null;
        public static bool enablePlugin = false;
        public static int tries = 0;
        public static bool opened = true;
        public static bool secureChaged = false;
        public static bool isAutoTyping = false;
        public static bool LockAfterAutoType = false;
        public static UpdateCheckForm updateCheckForm = null;

        public static IPluginHost Host
        {
            get { return host; }
        }

        public override Image SmallIcon
        {
            get { return Properties.Resources.windows_hello16x16; }
        }

        public override string UpdateUrl
        {
            get { return "https://github.com/Angelelz/WinHelloUnlock/raw/master/WinHelloUnlock/keepass.version"; }
        }

        public override bool Initialize(IPluginHost _host)
        {
            if (host != null)
            {
                Debug.Assert(false);
                Terminate();
            }
            if (_host == null) { return false; }

            host = _host;

            GlobalWindowManager.WindowAdded += WindowAddedHandler;
            host.MainWindow.FileOpened += FileOpenedHandler;
            host.MainWindow.FileSaved += OnSavedDB;
            host.MainWindow.DocumentManager.ActiveDocumentSelected += ActiveDocChanged;

            return true;
        }

        public override void Terminate()
        {
            if (host == null) { return; }

            GlobalWindowManager.WindowAdded -= WindowAddedHandler;
            host.MainWindow.FileOpened -= FileOpenedHandler;
            host.MainWindow.FileSaved -= OnSavedDB;
            host.MainWindow.DocumentManager.ActiveDocumentSelected -= ActiveDocChanged;
            UWPLibrary.ck = null;
            host = null;
        }

        /// <summary>
		/// Called everytime a database is opened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private async void FileOpenedHandler(object sender, FileOpenedEventArgs e)
        {
            var ioInfo = e.Database.IOConnectionInfo;
            if (e.Database.CustomData.Get(ProductName) == null) // If there is no CustomData in this database
            {
                // Create CustomData to save global setting to enable or disable the plugin
                e.Database.CustomData.Set(ProductName, "true");
                e.Database.Modified = true;
                
                // Try to save the database
                try { e.Database.Save(null); }
                catch { }
            }

            // Global settings to be used in the Options Panel
            dbName = Library.CharChange(ioInfo.Path);
            database = e.Database;
            UWPLibrary.ck = database.MasterKey;
            if (e.Database.CustomData.Get(ProductName + "AT") == "true") LockAfterAutoType = true;
            else LockAfterAutoType = false;

            if (e.Database.CustomData.Get(ProductName) == "true") enablePlugin = true;
            if (e.Database.CustomData.Get(ProductName) == "false") // if plugin is disabled for the database
            {
                enablePlugin = false;
                return; // Don't do anything else
            }

            if (await UWPLibrary.FirstTime(dbName)) // If the database has no credentials saved
            {
                bool isHelloAvailable = await UWPLibrary.IsHelloAvailable();

                if (isHelloAvailable)
                {
                    // Ask the user if he/she wants to configure the plugin
                    bool yesOrNo = MessageService.AskYesNo("Do You want to set " +
                    WinHelloUnlockExt.ProductName + " for " + dbName + " now?", WinHelloUnlockExt.ShortProductName, true);

                    // In case he/she wants, create the credentials
                    if (yesOrNo)
                        await UWPLibrary.CreateHelloData(dbName);
                }
            }

            // Set global settings back to default
            tries = 0;
            opened = true;
        }

        /// <summary>
		/// Used to modify other form when they load.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void WindowAddedHandler(object sender, GwmWindowEventArgs e)
        {
            // If a database is attempted to be unlocked
            if (e.Form is KeyPromptForm keyPromptForm)
            {
                keyPromptForm.Opacity = 0;
                keyPromptForm.Visible = false;
                var mf = KeePass.Program.MainForm;
                isAutoTyping = (bool)mf.GetType().GetField("m_bIsAutoTyping", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mf);

                var fieldInfo = keyPromptForm.GetType().GetField("m_ioInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                var ioInfo = fieldInfo.GetValue(keyPromptForm) as IOConnectionInfo;
                string dbName = Library.CharChange(ioInfo.Path);
                bool isHelloAvailable = await UWPLibrary.IsHelloAvailable();

                // if the database has credentials saved and Windows Hello is available
                if (!await UWPLibrary.FirstTime(dbName) && isHelloAvailable)
                {
                    // If there is no other Windows Hello Prompt opened
                    if (opened)
                    {
                        opened = false;
                        
                        Library.UnlockDatabase(ioInfo, keyPromptForm);
                    }
                    else // If there is another Windows Hello Prompt opened, just close this regular prompt
                        // This is usefull for when there is a double attempt to unlock the database by some plugins (ChromeIPass)
                        Library.CloseFormWithResult(keyPromptForm, DialogResult.Cancel);
                }
                else if (!await UWPLibrary.FirstTime(dbName))
                {
                    MessageService.ShowInfo("This Database has credential data saved. Enable Windows Hello to use.");
                    keyPromptForm.Opacity = 1;
                    keyPromptForm.Visible = true;
                }
                else
                {
                    keyPromptForm.Opacity = 1;
                    keyPromptForm.Visible = true;
                }
            }

            // If the Options window is opened
            if (e.Form is OptionsForm optionsForm)
            {
                if (!host.MainWindow.ActiveDatabase.IsOpen) return; //  If there is no database opened, don't do anything

                optionsForm.Shown += (object sender2, EventArgs e2) =>
                {
                    
                    try
                    {
                        Library.AddWinHelloOptions(optionsForm);
                    }
                    catch (Exception ex)
                    {
                        MessageService.ShowWarning("WinHelloUnlock Error: " + ex.Message);
                    }
                };
            }

            // If the Update Check Window is opened.
            // This is used because the Update Check Window prevents the a database from being opened
            if (e.Form is UpdateCheckForm ucf && !opened)
                WinHelloUnlockExt.updateCheckForm = ucf;
        }

        /// <summary>
		/// Used to Update global settings when the active database is changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void ActiveDocChanged(object sender, EventArgs e)
        {
            database = Host.MainWindow.ActiveDatabase;
            if (database.CustomData.Get(ProductName + "AT") == "true") LockAfterAutoType = true;
            else LockAfterAutoType = false;
            var ioInfo = database.IOConnectionInfo;
            dbName = Library.CharChange(ioInfo.Path);
            if (database.MasterKey != null)
                UWPLibrary.ck = database.MasterKey;
        }

        /// <summary>
		/// Used to detect if the master key has been changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private async void OnSavedDB(Object sender, FileSavedEventArgs args)
        {
            var db = args.Database;
            var ioInfo = db.IOConnectionInfo;
            string dbPath = Library.CharChange(ioInfo.Path);
            if (!await UWPLibrary.FirstTime(dbPath) && await UWPLibrary.IsHelloAvailable() && !Library.CheckMasterKey(ioInfo, UWPLibrary.ck))
                await Library.HandleMasterKeyChange(ioInfo, dbPath, false);
        }

    }
}