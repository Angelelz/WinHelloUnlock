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


namespace WinHelloUnlock
{
    public class Library
    {
        

        internal static KeyList GetKeys(PwDatabase db)
        {
            CompositeKey dKey = db.MasterKey;
            var passwordKey = dKey.UserKeys.Where(k => k is KcpPassword).FirstOrDefault() as KcpPassword;
            var kFile = dKey.UserKeys.Where(k => k is KcpKeyFile).FirstOrDefault() as KcpKeyFile;
            var uAccount = dKey.UserKeys.Where(k => k is KcpUserAccount).FirstOrDefault() as KcpUserAccount;
            IEnumerable<IUserKey> kList = dKey.UserKeys;
            int kNumber = kList.Count();
            string[] pString = new string[kNumber];
            string[] nString = new string[kNumber];
            string[] tString = new string[kNumber];
            int i = 0;
            foreach (var uKey in kList)
            {
                switch (uKey.GetType().ToString())
                {
                    case "KeePassLib.Keys.KcpPassword":
                        nString[i] = WinHelloUnlockExt.ShortProductName + i.ToString();
                        pString[i] = passwordKey.Password.ReadString();
                        tString[i] = "KeePassLib.Keys.KcpPassword";
                        break;
                    case "KeePassLib.Keys.KcpKeyFile":
                        nString[i] = WinHelloUnlockExt.ShortProductName + i.ToString();
                        pString[i] = kFile.Path;
                        tString[i] = "KeePassLib.Keys.KcpKeyFile";
                        break;
                    case "KeePassLib.Keys.KcpUserAccount":
                        nString[i] = WinHelloUnlockExt.ShortProductName + i.ToString();
                        pString[i] = "WithUA";
                        tString[i] = "KeePassLib.Keys.KcpUserAccount";
                        break;
                }

                ++i;
            }
            return new KeyList(tString, pString);

        }

        

        internal static CompositeKey ConvertToComposite(KeyList kList)
        {
            if (kList.Pass == null || kList.KeyName == null) return new CompositeKey();
            int kNumber = kList.Pass.Count();
            int i = 0;
            CompositeKey mKey = new CompositeKey();
            for (i = 0; i < kNumber; ++i)
            {
                switch (kList.KeyName[i])
                {
                    case "KeePassLib.Keys.KcpPassword":
                        IUserKey mKeyPass = new KcpPassword(kList.Pass[i]);
                        mKey.AddUserKey(mKeyPass);
                        break;
                    case "KeePassLib.Keys.KcpKeyFile":
                        IUserKey mKeyFile = new KcpKeyFile(kList.Pass[i]);
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

        internal static string ConvertToString(KeyList keys)
        {
            string div2 = WinHelloUnlockExt.ProductName + ",";
            string div = WinHelloUnlockExt.ShortProductName + ",";
            if (keys.Pass == null || keys.KeyName == null || keys == null) return "";
            string pass = string.Join(div, keys.Pass);
            string key = string.Join(div, keys.KeyName);
            return key + div2 + pass;
        }
        internal static KeyList ConvertKeyList(string keyAndPass)
        {
            string div2 = WinHelloUnlockExt.ProductName + ",";
            string div = WinHelloUnlockExt.ShortProductName + ",";
            if (keyAndPass == null) return new KeyList(null, null);
            string[] keyPass = Split(keyAndPass,div2);
            if (keyPass[1] == null) return new KeyList(null, null);
            string[] keyName = Split(keyPass[0],div);
            string[] pass = Split(keyPass[1],div);
            return new KeyList(keyName, pass);
        }

        internal static string[] Split(string s, string separator)
        {
            return s.Split(new string[] { separator }, StringSplitOptions.None);
        }

        internal static void SetCompositeKey(KeyPromptForm keyPromptForm, CompositeKey compositeKey)
        {
            var fieldInfo = keyPromptForm.GetType().GetField("m_pKey", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo != null)
                fieldInfo.SetValue(keyPromptForm, compositeKey);
        }

        internal static void CloseFormWithResult(KeyPromptForm keyPromptForm, DialogResult result)
        {
            // Remove flushing
            keyPromptForm.Visible = false;
            keyPromptForm.Opacity = 0;

            keyPromptForm.DialogResult = result;
            keyPromptForm.Close();
        }

        internal static void UnlockDatabase(IOConnectionInfo ioInfo, string dbName, KeyPromptForm keyPromptForm, bool secureDesktop)
        {
            if (keyPromptForm.SecureDesktopMode && WinHelloUnlockExt.tries < 1)
            {
                UnlockWithoutSecure(keyPromptForm, ioInfo, secureDesktop);
            }
            else if (WinHelloUnlockExt.tries < 1 && !secureDesktop)
            {
                UWPLibrary.UnlockWithSecure(dbName, keyPromptForm, ioInfo);
            }
        }

        internal async static void UnlockWithoutSecure(KeyPromptForm keyPromptForm, IOConnectionInfo ioInfo, bool secureDesktop)
        {
            CloseFormWithResult(keyPromptForm, DialogResult.Cancel);

            await Task.Factory.StartNew(() =>
            {
                KeePass.Program.Config.Security.MasterKeyOnSecureDesktop = false;
                secureDesktop = true;
                Thread.Yield();
                MainForm mainForm = WinHelloUnlockExt.Host.MainWindow;
                Action action = () => mainForm.OpenDatabase(ioInfo, null, false);
                mainForm.Invoke(action);
            })
            .ContinueWith(_ =>
            {
                KeePass.Program.Config.Security.MasterKeyOnSecureDesktop = true;
                secureDesktop = false;
            });
        }

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

    }

    public class KeyList
    {
        private readonly string[] _kName;
        private readonly string[] _pName;
        public string[] KeyName
        {
            get { return _kName; }
        }
        public string[] Pass
        {
            get { return _pName; }
        }
        public KeyList(string[] e, string[] db)
        {
            _kName = e;
            _pName = db;
        }

    }

}