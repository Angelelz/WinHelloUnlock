using KeePass.Plugins;
using KeePass.Forms;
using KeePass.UI;
using System;
using System.Reflection;
using System.Drawing;
using System.Diagnostics;
using KeePassLib;
using KeePassLib.Utility;
using KeePassLib.Serialization;

namespace WinHelloUnlock
{
    public class WinHelloUnlockExt : Plugin
    {
        private static IPluginHost host = null;
        public const string ShortProductName = "HelloUnlock";
        public const string ProductName = "WinHelloUnlock";
        public static string dbName;
        public static PwDatabase database = null;
        public static bool enablePlugin = false;
        public static int tries = 0;

        public static IPluginHost Host
        {
            get { return host; }
        }

        public override Image SmallIcon
        {
            get { return Properties.Resources.windows_hello16x16; }
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

            return true;
        }

        public override void Terminate()
        {
            if (host == null) { return; }

            GlobalWindowManager.WindowAdded -= WindowAddedHandler;
            host.MainWindow.FileOpened -= FileOpenedHandler;

            host = null;
        }

        /// <summary>
		/// Called everytime a database is opened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private async void FileOpenedHandler(object sender, FileOpenedEventArgs e)
        {
            if (e.Database.CustomData.Get(ProductName) == null)
            {
                e.Database.CustomData.Set(ProductName, "true");
                e.Database.Modified = true;
                try { e.Database.Save(null); }
                catch { }
            }

            if (e.Database.CustomData.Get(ProductName) == "true") enablePlugin = true;
            if (e.Database.CustomData.Get(ProductName) == "false")
            {
                enablePlugin = false;
                return;
            }

            var ioInfo = e.Database.IOConnectionInfo;
            dbName = ioInfo.Path;
            bool firstTime = await UWPLibrary.FirstTime(dbName);
            database = e.Database;

            await UWPLibrary.CreateHelloData(dbName);
            tries = 0;
            
        }

        /// <summary>
		/// Used to modify other form when they load.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void WindowAddedHandler(object sender, GwmWindowEventArgs e)
        {
            database = KeePass.Program.MainForm.ActiveDatabase;
            dbName = database.IOConnectionInfo.Path;
            bool secureDesktop = false;

            if (e.Form is KeyPromptForm keyPromptForm)
            {
                var fieldInfo = keyPromptForm.GetType().GetField("m_ioInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                var ioInfo = fieldInfo.GetValue(keyPromptForm) as IOConnectionInfo;
                string dbName = ioInfo.Path;
                bool isHelloAvailable = await UWPLibrary.IsHelloAvailable();

                if (!await UWPLibrary.FirstTime(dbName) && isHelloAvailable)
                {
                    Library.UnlockDatabase(ioInfo, dbName, keyPromptForm, secureDesktop);
                }
                else if (!await UWPLibrary.FirstTime(dbName))
                {
                    MessageService.ShowInfo("This Database has credential data saved. Enable Windows Hello to use.");
                }
            }
            if (e.Form is OptionsForm optionsForm)
            {
                optionsForm.Shown += delegate (object sender2, EventArgs e2)
                {
                    
                    try
                    {
                        Library.AddWinHelloOptions(optionsForm);
                    }
                    catch (Exception ex)
                    {
                        MessageService.ShowWarning("Error: " + ex.Message);
                        //Debug.Fail(ex.ToString());
                    }
                };
            }
        }

        

    }
}