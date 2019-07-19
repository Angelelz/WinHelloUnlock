using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using KeePassLib.Utility;
using KeePass.Forms;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WinHelloUnlock
{
    public static class UWPLibrary
    {
        internal static KeyCredentialCreationOption option = KeyCredentialCreationOption.FailIfExists;
        internal static BinaryStringEncoding encoding = BinaryStringEncoding.Utf8;
        private static Assembly assembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// Checks to see if Passport is ready to be used.
        /// 
        /// Passport has dependencies on:
        ///     1. Having a connected Microsoft Account
        ///     2. Having a Windows PIN set up for that _account on the local machine
        /// </summary>
        internal static async Task<bool> IsHelloAvailable()
        {
            return await KeyCredentialManager.IsSupportedAsync();
        }

        internal static async Task<KeyCredentialRetrievalResult> CreateCredential(string credentialName)
        {
            return await KeyCredentialManager.RequestCreateAsync(credentialName, option);
        }

        internal static async Task<KeyCredentialRetrievalResult> CreateCredential(string credentialName, KeyCredentialCreationOption op)
        {
            return await KeyCredentialManager.RequestCreateAsync(credentialName, op);
        }

        internal static async Task<KeyCredentialRetrievalResult> OpenCredential(string credentialName)
        {
            return await KeyCredentialManager.OpenAsync(credentialName);
        }

        internal static async void DeleteCredential(string credentialName)
        {
            try
            {
                await KeyCredentialManager.DeleteAsync(credentialName);
            }
            catch(Exception ev)
            {
                //MessageService.ShowWarning("Credential not deleted: " + ev.Message);
                Debug.Write("Expected exception: " + ev.Message);
            }
        }

        internal static void DeleteHelloData(string db)
        {
            
            PasswordVault myVault = new PasswordVault();
            try
            {
                DeleteCredential(db);
                PasswordCredential newCredential = myVault.Retrieve(db, WinHelloUnlockExt.ProductName);
                myVault.Remove(newCredential);
            }
            catch (Exception ev)
            {
                //MessageService.ShowWarning("Data not deleted: " + ev.Message);
                MessageService.ShowWarning("Credential not deleted: " + ev.Message);
            }
            
        }

        internal static void WinHelloErrors(KeyCredentialRetrievalResult verif)
        {
            switch (verif.Status)
            {
                case (KeyCredentialStatus.CredentialAlreadyExists):
                    MessageService.ShowWarning("The credential already exists.");
                    break;
                case (KeyCredentialStatus.NotFound):
                    MessageService.ShowWarning("The credential could not be found.");
                    break;
                case (KeyCredentialStatus.SecurityDeviceLocked):
                    MessageService.ShowWarning("The security device was locked.");

                    break;
                case (KeyCredentialStatus.UnknownError):
                    MessageService.ShowWarning("An unknown error occurred.");
                    break;
                case (KeyCredentialStatus.UserCanceled):
                    MessageService.ShowWarning("The request was cancelled by the user.");
                    break;
                case (KeyCredentialStatus.UserPrefersPassword):
                    MessageService.ShowWarning("The user prefers to enter a password.");
                    break;
                default:
                    MessageService.ShowWarning("An Error prevented Windows Hello from completing the operation.");
                    break;
            }
        }

        internal static async Task<string> SaveKeys(string db, KeyList list, KeyCredentialRetrievalResult rResult)
        {
            PasswordVault myVault = new PasswordVault();
            string encrypted = await Encrypt(Library.ConvertToString(list), rResult);
            try
            {
                PasswordCredential newCredential = new PasswordCredential(db, WinHelloUnlockExt.ProductName, encrypted);
                newCredential.RetrievePassword();
                myVault.Add(newCredential);
                return "Success";
            }
            catch (Exception ev)
            {
                //MessageService.ShowInfo("Library.SaveKeys Exception: " + ev.Message);
                return ev.Message;
            }
        }

        internal async static Task<KeyList> RetrieveKeys(string db, KeyCredentialRetrievalResult rResult)
        {
            
            PasswordVault myVault = new PasswordVault();
            var newCredential = new PasswordCredential();
            try
            {
                newCredential = myVault.Retrieve(db, WinHelloUnlockExt.ProductName);
                newCredential.RetrievePassword();
            }
            catch (Exception ev)
            {
                MessageService.ShowInfo("Not able to retrieve Composite Key from Microsoft Password Vault. " +
                    "Maybe saving again will solve the problem. Message: " + ev.Message);
                return new KeyList(null, null);
            }
            try
            {
                string encrypted = newCredential.Password;
                string decrypted = await Decrypt(encrypted, rResult);
                KeyList Keys = Library.ConvertKeyList(decrypted);
                return Keys;
            }
            catch (Exception ev)
            {
                MessageService.ShowInfo("Credential not retrieved");
                Debug.Write(ev.Message);
                return new KeyList(null, null);
            }
        }

        internal static async Task<string> Encrypt(string strClearText, KeyCredentialRetrievalResult rResult)
        {
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            var id = attribute.Value;
            IBuffer buffMsg = CryptographicBuffer.ConvertStringToBinary(id, encoding);
            KeyCredentialOperationResult opResult = await rResult.Credential.RequestSignAsync(buffMsg);
            IBuffer signedData = opResult.Result;
            SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            CryptographicKey myKey = provider.CreateSymmetricKey(signedData);
            IBuffer buffClear = CryptographicBuffer.ConvertStringToBinary(strClearText, encoding);
            IBuffer buffProtected = CryptographicEngine.Encrypt(myKey, buffClear, null);
            return CryptographicBuffer.EncodeToBase64String(buffProtected);
        }

        internal static async Task<String> Decrypt(string strProtected, KeyCredentialRetrievalResult rResult)
        {
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            var id = attribute.Value;
            IBuffer buffMsg = CryptographicBuffer.ConvertStringToBinary(id, encoding);
            KeyCredentialOperationResult opResult = await rResult.Credential.RequestSignAsync(buffMsg);
            IBuffer signedData = opResult.Result;
            SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            CryptographicKey myKey = provider.CreateSymmetricKey(signedData);
            IBuffer buffProtected = CryptographicBuffer.DecodeFromBase64String(strProtected);
            IBuffer buffUnprotected = CryptographicEngine.Decrypt(myKey, buffProtected, null);
            return CryptographicBuffer.ConvertBinaryToString(encoding, buffUnprotected);
        }

        internal static async Task<bool> FirstTime(string res)
        {

            KeyCredentialRetrievalResult result = await OpenCredential(res);
            PasswordVault myVault = new PasswordVault();

            try
            {
                PasswordCredential cred = myVault.Retrieve(res, WinHelloUnlockExt.ProductName);
                if (result.Status == KeyCredentialStatus.Success) return false;
                else return true;
            }
            catch(Exception e)
            {
                //MessageService.ShowInfo("(First TIme) Exception: " + e.Message);
                Debug.Write(e.Message);
                return true;
            }

        }

        internal static async Task<bool> CreateHelloData(string dbName)
        {
            if (await FirstTime(dbName))
            {
                bool yesOrNo = MessageService.AskYesNo("Do You want to set " +
                    WinHelloUnlockExt.ProductName + " for " + dbName + " now?", WinHelloUnlockExt.ShortProductName, true);

                if (yesOrNo)
                {
                    bool isHelloAvailable = await UWPLibrary.IsHelloAvailable();
                    if (isHelloAvailable)
                    {

                        KeyCredentialCreationOption optionNew = KeyCredentialCreationOption.ReplaceExisting;
                        KeyCredentialRetrievalResult retrievalResult = await UWPLibrary.CreateCredential(dbName, optionNew);

                        if (retrievalResult.Status == KeyCredentialStatus.Success)
                        {
                            KeyList keyList = Library.GetKeys(WinHelloUnlockExt.database);
                            string resultSave = await UWPLibrary.SaveKeys(dbName, keyList, retrievalResult);
                            if (resultSave == "Success")
                            {
                                MessageService.ShowInfo("Database Keys saved successfuly");
                                return true;
                            }
                            else MessageService.ShowWarning("Error saving the composite key: " + resultSave);
                        }
                        else
                        {
                            UWPLibrary.WinHelloErrors(retrievalResult);
                        }
                    }
                    else
                    {
                        MessageService.ShowWarning("Windows Hello is NOT Available");
                    }

                }
                else
                {
                    //MessageService.ShowWarning("You will be asked again next time you open this Database");
                }
            }
            else
            {

            }
            return false;
        }

        internal static async void UnlockWithSecure(string dbName, KeyPromptForm keyPromptForm, IOConnectionInfo ioInfo)
        {
            KeyCredentialRetrievalResult retrievalResult = await UWPLibrary.OpenCredential(dbName);
            if (retrievalResult.Status == KeyCredentialStatus.Success)
            {
                KeyList keyList = await UWPLibrary.RetrieveKeys(dbName, retrievalResult);

                if (keyList.KeyName != null)
                {
                    CompositeKey compositeKey = Library.ConvertToComposite(keyList);
                    Library.SetCompositeKey(keyPromptForm, compositeKey);
                    Library.CloseFormWithResult(keyPromptForm, DialogResult.OK);
                }
                else
                {
                    Library.CloseFormWithResult(keyPromptForm, DialogResult.Cancel);
                    ++WinHelloUnlockExt.tries;
                    await Task.Factory.StartNew(() => {
                        MainForm mainForm = WinHelloUnlockExt.Host.MainWindow;
                        Action action = () => mainForm.OpenDatabase(ioInfo, null, false);
                        mainForm.Invoke(action);
                    });
                }
            }
            else
            {
                UWPLibrary.WinHelloErrors(retrievalResult);
                Library.CloseFormWithResult(keyPromptForm, DialogResult.Cancel);
                ++WinHelloUnlockExt.tries;
                await Task.Factory.StartNew(() => {
                    MainForm mainForm = WinHelloUnlockExt.Host.MainWindow;
                    Action action = () => mainForm.OpenDatabase(ioInfo, null, false);
                    mainForm.Invoke(action);
                });
            }
        }

    }
}