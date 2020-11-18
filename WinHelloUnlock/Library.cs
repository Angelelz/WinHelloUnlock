using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using System.Linq;
using KeePass.Forms;
using KeePassLib.Security;
using System.Runtime.InteropServices;
using KeePassLib.Utility;

namespace WinHelloUnlock
{
    public class Library
    {

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("User32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, Int32 lParam);

        /// <summary>
        /// Convert the composite key to a KeyList class
        /// </summary>
        /// <param name="db">The source database.</param>
        /// <returns>A KeyList object containing the composite key.</returns>
        internal static KeyList GetKeys(PwDatabase db)
        {
            CompositeKey dKey = db.MasterKey;
            var passwordKey = dKey.UserKeys.Where(k => k is KcpPassword).FirstOrDefault() as KcpPassword;
            var kFile = dKey.UserKeys.Where(k => k is KcpKeyFile).FirstOrDefault() as KcpKeyFile;
            var uAccount = dKey.UserKeys.Where(k => k is KcpUserAccount).FirstOrDefault() as KcpUserAccount;
            IEnumerable<IUserKey> kList = dKey.UserKeys;
            int kNumber = kList.Count();
            ProtectedString[] pString = new ProtectedString[kNumber];
            String[] tString = new String[kNumber];
            int i = 0;
            foreach (var uKey in kList)
            {
                switch (uKey.GetType().ToString())
                {
                    case "KeePassLib.Keys.KcpPassword":
                        pString[i] = passwordKey.Password;
                        tString[i] = "KeePassLib.Keys.KcpPassword";
                        break;
                    case "KeePassLib.Keys.KcpKeyFile":
                        pString[i] = new ProtectedString(true, kFile.Path);
                        tString[i] = "KeePassLib.Keys.KcpKeyFile";
                        break;
                    case "KeePassLib.Keys.KcpUserAccount":
                        pString[i] = new ProtectedString(true, "WithUA");
                        tString[i] = "KeePassLib.Keys.KcpUserAccount";
                        break;
                }
                ++i;
            }
            return new KeyList(tString, pString);

        }


        /// <summary>
        /// Convert the KeyList object to a composite key
        /// </summary>
        /// <param name="kList">KeyList containing the composite key information.</param>
        /// <returns>CompositeKey object.</returns>
        internal static CompositeKey ConvertToComposite(KeyList kList)
        {
            if (kList.Pass == null || kList.KeyName == null) return null;
            int kNumber = kList.Pass.Count();
            int i = 0;
            CompositeKey mKey = new CompositeKey();
            for (i = 0; i < kNumber; ++i)
            {
                switch (kList.KeyName[i])
                {
                    case "KeePassLib.Keys.KcpPassword":
                        byte[] pb = kList.Pass[i].ReadUtf8();
                        IUserKey mKeyPass = new KcpPassword(pb);
                        MemUtil.ZeroByteArray(pb);
                        mKey.AddUserKey(mKeyPass);
                        break;
                    case "KeePassLib.Keys.KcpKeyFile":
                        IUserKey mKeyFile = new KcpKeyFile(kList.Pass[i].ReadString());
                        mKey.AddUserKey(mKeyFile);
                        break;
                    case "KeePassLib.Keys.KcpUserAccount":
                        IUserKey mKeyUser = new KcpUserAccount();
                        mKey.AddUserKey(mKeyUser);
                        break;
                }
            }
            return mKey;
        }

        /// <summary>
        /// Converts a KeyList object to a properly formatted ProtectedString
        /// </summary>
        /// <param name="keys">KeyList containing the composite key information.</param>
        /// <returns>ProtectedString containing KeyList information.</returns>
        internal static ProtectedString ConvertToPString(KeyList keys)
        {
            string div2 = WinHelloUnlockExt.ProductName + ",";
            string div = WinHelloUnlockExt.ShortProductName + ",";
            if (keys.Pass == null || keys.KeyName == null || keys == null) return null;
            ProtectedString pass = ProtectedString.EmptyEx;
            foreach (ProtectedString ps in keys.Pass)
                pass += ps + div;
            pass = pass.Remove(pass.Length - div.Length, div.Length);
            ProtectedString key = new ProtectedString(true, string.Join(div, keys.KeyName));
            key = key.Insert(key.Length, div2);
            return key + pass;
        }

        /// <summary>
        /// Converts a properly formatted ProtectedString to a KeyList object
        /// </summary>
        /// <param name="keyAndPass">Specially formatted ProtectedString containing key information.</param>
        /// <returns>KeyList based on provided ProtectedString.</returns>
        internal static KeyList ConvertKeyList(ProtectedString keyAndPass)  // Maybe find a way to not use String?
        {
            string div2 = WinHelloUnlockExt.ProductName + ",";
            string div = WinHelloUnlockExt.ShortProductName + ",";
            if (keyAndPass == null) return new KeyList(null, null);
            var keyPass = Split(keyAndPass,div2);
            if (keyPass[1] == null) return new KeyList(null, null);
            var keyName = Split(keyPass[0],div);
            var pass = Split(keyPass[1],div);
            string[] keyArray = keyName.Select(ps => ps.ReadString()).ToArray();
            return new KeyList(keyArray, pass.ToArray());
        }

        /// <summary>
        /// Splits a ProtectedString into a ProtectedString list using a string separator
        /// </summary>
        /// <param name="ps">ProtectedString to separate.</param>
        /// <param name="separator"> String separator.</param>
        /// <returns>ProtectedString List.</returns>
        internal static List<ProtectedString> Split(ProtectedString ps, string separator)
        {
            int index = ps.ReadString().IndexOf(separator); // Would this be safe?
            var list = new List<ProtectedString>();
            if (index < 0)
            {
                list.Add(ps);
                return list;
            }
            
            do {
                list.Add(ps.Remove(index, ps.Length - index));
                index += separator.Length;
                ps = ps.Remove(0, index);
                index = ps.ReadString().IndexOf(separator);
            } while (index > 0);
            list.Add(ps);
            return list;
        }

        /// <summary>
        /// Closes the KeyPromptForm with a specific result (Equivalent to click the specific button)
        /// </summary>
        /// <param name="keyPromptForm">KeyPromptForm to close.</param>
        /// <param name="result">Result to close the form with.</param>
        internal static void CloseFormWithResult(KeyPromptForm keyPromptForm, DialogResult result)
        {
            // Remove flushing
            keyPromptForm.Visible = false;
            keyPromptForm.Opacity = 0;

            keyPromptForm.DialogResult = result;
            keyPromptForm.Close();
        }

        /// <summary>
        /// Unlocks the database prompted to unlock on the KeyPromptForm
        /// </summary>
        /// <param name="ioInfo">IOConnectionInfo that represents the database.</param>
        /// <param name="keyPromptForm">KeyPromptForm to unlock the database from.</param>
        internal async static void UnlockDatabase(IOConnectionInfo ioInfo, KeyPromptForm keyPromptForm)
        {
            // Only one try is allowed
            if (WinHelloUnlockExt.tries < 1)
            {
                if (KeePass.Program.Config.Security.MasterKeyOnSecureDesktop)
                {
                    WinHelloUnlockExt.secureChaged = true;
                    WinHelloUnlockExt.isMonitoring = true;
                    var _ = Task.Factory.StartNew(() => CloseWarning());
                    KeePass.Program.Config.Security.MasterKeyOnSecureDesktop = false;

                    UWPLibrary.Unlock(keyPromptForm, new CompositeKey());
                }
                else
                {
                    
                    await UWPLibrary.UnlockDatabase(ioInfo, keyPromptForm);
                    ++WinHelloUnlockExt.tries;
                    
                    if (WinHelloUnlockExt.secureChaged)
                    {
                        KeePass.Program.Config.Security.MasterKeyOnSecureDesktop = true;
                        WinHelloUnlockExt.secureChaged = false;
                    }
                }

            }
            else
            {
                keyPromptForm.Visible = true;
                keyPromptForm.Opacity = 1;
            }

            WinHelloUnlockExt.opened = true;
        }

        /// <summary>
        /// Reopens the KeyPromptForm whenever a change in MasterKeyOnSecureDesktop is made.
        /// </summary>
        internal static void ReopenKeyPromptForm(KeyPromptForm keyPromptForm)
        {
            if (WinHelloUnlockExt.secureChaged)
            {
                KeePass.Program.Config.Security.MasterKeyOnSecureDesktop = true;
                WinHelloUnlockExt.secureChaged = false;

                WinHelloUnlockExt.isMonitoring = true;
                var _ = Task.Factory.StartNew(() => Library.CloseWarning());

                UWPLibrary.Unlock(keyPromptForm, new CompositeKey());
            }
            else
            {
                keyPromptForm.Visible = true;
                keyPromptForm.Opacity = 1;
            }
        }

        /// <summary>
        /// Must be executed as background task.
        /// </summary>
        internal static void CloseWarning()
        {
            IntPtr ptrFF = Library.FindWindow("#32770", "KeePass");
            while (WinHelloUnlockExt.isMonitoring)
            {
                if (ptrFF != IntPtr.Zero)
                {
                    SendMessage(ptrFF, 0x0010, 0, 0);

                    break;
                }
                else ptrFF = Library.FindWindow("#32770", "KeePass");
                Thread.Sleep(10);
            }
        }

        /// <summary>
		/// Used to modify options form when it loads.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        internal static void AddWinHelloOptions(OptionsForm optionsForm)
        {
            if (optionsForm.Controls.Find("m_tabMain", true).FirstOrDefault() is TabControl m_tabMain)
            {
                if (m_tabMain.ImageList == null)
                {
                    m_tabMain.ImageList = new ImageList();
                }
                var imageIndex = m_tabMain.ImageList.Images.Add(Properties.Resources.windows_hello16x16, Color.Transparent);

                var newTab = new TabPage(WinHelloUnlockExt.ProductName)
                {
                    UseVisualStyleBackColor = true,
                    ImageIndex = imageIndex
                };

                var optionsPanel = new OptionsPanel();
                newTab.Controls.Add(optionsPanel);
                optionsPanel.Dock = DockStyle.Fill;


                m_tabMain.TabPages.Add(newTab);
            }
        }

        /// <summary>
        /// Changes '/' for '\' because Windows Hello throws an error when '/' is in the name of the credential
        /// </summary>
        /// <param name="name">String to replace the character.</param>
        internal static string CharChange(string name)
        {
            return name.Replace('/', '\\');
        }

        /// <summary>
        /// Checks whether the provided master key actually unlocks the database
        /// </summary>
        /// <param name="ioInfo">IOConnectionInfo that represents the database.</param>
        /// <param name="key">Master Key to check.</param>
        internal static bool CheckMasterKey(IOConnectionInfo ioinfo, CompositeKey key)
        {
            PwDatabase db = new PwDatabase();
            try
            {
                db.Open(ioinfo, key, null);
                bool isopen = db.IsOpen;
                db.Close();
                return isopen;
            }
            catch(Exception) { return false; }
        }

        /// <summary>
        /// When the plugin detects a change in the MasterKey, it uses this method to prompt the user to update or delete the data
        /// </summary>
        /// <param name="ioInfo">IOConnectionInfo that represents the database.</param>
        /// <param name="dbPath">Name of the credential to update or delete.</param>
        /// <param name="opening">If the masterkey change was detected during database unlock use true.</param>
        internal static async Task HandleMasterKeyChange(IOConnectionInfo ioInfo, string dbPath, bool opening)
        {
            if (opening)
            {
                string str = WinHelloUnlockExt.ProductName + " could not unlock this database." +
                                " MasterKey must have changed. Delete " + WinHelloUnlockExt.ProductName + " data?";
                if (MessageService.AskYesNo(str, WinHelloUnlockExt.ProductName))
                    UWPLibrary.DeleteHelloData(dbPath);
                WinHelloUnlockExt.opened = true;
                WinHelloUnlockExt.Host.MainWindow.OpenDatabase(ioInfo, null, false);
            }
            else
            {
                string str = "A change in MasterKey has been detected. Do you want to update " +
                            WinHelloUnlockExt.ProductName + " data?";
                if (MessageService.AskYesNo(str, WinHelloUnlockExt.ProductName))
                {
                    UWPLibrary.DeleteHelloData(dbPath);
                    await UWPLibrary.CreateHelloData(dbPath);
                }
            }
        }

    }

    /// <summary>
    /// Object to pass arround that contains Masterkey information
    /// </summary>
    public class KeyList
    {
        private readonly string[] _kName;
        private readonly ProtectedString[] _pName;
        public string[] KeyName
        {
            get { return _kName; }
        }
        public ProtectedString[] Pass
        {
            get { return _pName; }
        }
        public KeyList(string[] e, ProtectedString[] p)
        {
            _kName = e;
            _pName = p;
        }

    }

}