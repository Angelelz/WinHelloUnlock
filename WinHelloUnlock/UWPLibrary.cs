using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using KeePassLib.Utility;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using System.Reflection;
using System.Runtime.InteropServices;
using KeePassLib.Security;
using System.Linq;

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
        /// <returns>Bool representing Windows Hello availability.</returns>
        internal static async Task<bool> IsHelloAvailable()
        {
            return await KeyCredentialManager.IsSupportedAsync();
        }

        /// <summary>
        /// Request the creation of a Microsoft Key credential.
        /// </summary>
        /// <param name="credentialName">Name given to the credential to be created.</param>
        /// <param name="op">Available options to request the credential creation (Fail if exists or Replace existing.</param>
        /// <returns>KeyCredentialRetrievalResult object with all the information.</returns>
        internal static async Task<KeyCredentialRetrievalResult> CreateCredential(string credentialName, KeyCredentialCreationOption op)
        {
            return await KeyCredentialManager.RequestCreateAsync(credentialName, op);
        }

        /// <summary>
        /// Opens a Microsoft Key credential.
        /// </summary>
        /// <param name="credentialName">Name of the credential to be opened.</param>
        /// <returns>KeyCredentialRetrievalResult object with all the information.</returns>
        internal static async Task<KeyCredentialRetrievalResult> OpenCredential(string credentialName)
        {
            return await KeyCredentialManager.OpenAsync(credentialName);
        }

        /// <summary>
        /// Requests the deletion of a Microsoft Key credential.
        /// </summary>
        /// <param name="credentialName">Name of the credential to be deleted.</param>
        internal static async void DeleteCredential(string credentialName)
        {
            try
            {
                await KeyCredentialManager.DeleteAsync(credentialName);
            }
            catch(Exception ev)
            {
                MessageService.ShowWarning("Credential not deleted: " + ev.Message);
                //Debug.Write("Expected exception: " + ev.Message);
            }
        }

        /// <summary>
        /// Requests the deletion of all saved WinHelloUnlock data (Microsoft Key Credential, password vault, password credential).
        /// </summary>
        /// <param name="db">Path of the database of which to delete the WinHelloUnlock Data.</param>
        internal static void DeleteHelloData(string dbPath)
        {
            DeleteCredential(dbPath);
            PasswordVault myVault = new PasswordVault();
            try
            {
                PasswordCredential newCredential = myVault.Retrieve(dbPath, WinHelloUnlockExt.ProductName);
                myVault.Remove(newCredential);
            }
            catch (Exception ev)
            {
                //MessageService.ShowWarning("Data not deleted: " + ev.Message);
                MessageService.ShowWarning("Credential not deleted: " + ev.Message);
            }
            
        }

        /// <summary>
        /// Post warnings according to the results of credential operations.
        /// </summary>
        /// <param name="verif">Status of credential operation result to post.</param>
        internal static void WinHelloErrors(KeyCredentialStatus verif, string initialString)
        {
            switch (verif)
            {
                case (KeyCredentialStatus.CredentialAlreadyExists):
                    MessageService.ShowWarning(initialString + "The credential already exists.");
                    break;
                case (KeyCredentialStatus.NotFound):
                    MessageService.ShowWarning(initialString + "The credential could not be found.");
                    break;
                case (KeyCredentialStatus.SecurityDeviceLocked):
                    MessageService.ShowWarning(initialString + "The security device was locked.");
                    break;
                case (KeyCredentialStatus.UnknownError):
                    MessageService.ShowWarning(initialString + "An unknown error occurred.");
                    break;
                case (KeyCredentialStatus.UserCanceled):
                    MessageService.ShowWarning(initialString + "The request was cancelled by the user.");
                    break;
                case (KeyCredentialStatus.UserPrefersPassword):
                    MessageService.ShowWarning(initialString + "The user prefers to enter a password.");
                    break;
                default:
                    MessageService.ShowWarning(initialString + "An Error prevented Windows Hello from completing the operation.");
                    break;
            }
        }

        /// <summary>
        /// Saves an encrypted string containing the CompositeKey information to the Password vault.
        /// </summary>
        /// <param name="dbPath">Database Path. This is the identification of the database, if database is moved or name is changed,
        /// New credentials must be created.</param>
        /// <param name="keyList">KeyList object containing the composite key information.</param>
        /// <param name="rResult">KeyCredential object used to sign a key to encrypt the compositekey information.</param>
        /// <returns>String representing the result of the operation. Success or the error thrown.</returns>
        internal static async Task<string> SaveKeys(string dbPath, KeyList keyList, KeyCredentialRetrievalResult rResult)
        {
            try
            {
                PasswordVault myVault = new PasswordVault();
                String encrypted = await Encrypt(Library.ConvertToPString(keyList), rResult);
                PasswordCredential newCredential = new PasswordCredential(dbPath, WinHelloUnlockExt.ProductName, encrypted);
                newCredential.RetrievePassword();
                myVault.Add(newCredential);
                return "Success";
            }
            catch (Exception ev)
            {
                return ev.Message;
            }
        }

        /// <summary>
        /// Retrieves the CompositeKey information from the Password vault.
        /// </summary>
        /// <param name="dbPath">Database Path. This is the identification of the database, if database is moved or name is changed,
        /// New credentials must be created.</param>
        /// <param name="rResult">KeyCredential object used to sign a key to decrypt the compositekey information.</param>
        /// <returns>KeyList object with all the information to compose the CompositeKey.</returns>
        internal async static Task<KeyList> RetrieveKeys(string dbPath, KeyCredentialRetrievalResult rResult)
        {
            PasswordVault myVault = new PasswordVault();
            var newCredential = new PasswordCredential();
            try
            {
                newCredential = myVault.Retrieve(dbPath, WinHelloUnlockExt.ProductName);
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
                ProtectedString decrypted = await Decrypt(encrypted, rResult);
                if (decrypted != null)
                {
                    KeyList Keys = Library.ConvertKeyList(decrypted);
                    decrypted = ProtectedString.EmptyEx;
                    return Keys;
                }
                else return new KeyList(null, null);
            }
            catch (Exception ev)
            {
                MessageService.ShowInfo("Credential not retrieved: " + ev.Message);
                Debug.Write(ev.Message);
                return new KeyList(null, null);
            }
        }

        /// <summary>
        /// Encypts a string using a key signed by a KeyCredential.
        /// </summary>
        /// <param name="strClearText">Text to encrypt.</param>
        /// <param name="rResult">KeyCredential object used to sign a key to encrypt the text.</param>
        /// <returns>Encrypted text.</returns>
        internal static async Task<string> Encrypt(ProtectedString ps, KeyCredentialRetrievalResult rResult)
        {
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            var id = attribute.Value; // Any text can be used, it will be signed with the KeyCredential to encrypt the string
            IBuffer buffMsg = CryptographicBuffer.ConvertStringToBinary(id, encoding); // converted to an IBuffer

            // The actual Signing of the string
            KeyCredentialOperationResult opResult = await rResult.Credential.RequestSignAsync(buffMsg);
            IBuffer signedData = opResult.Result;

            // Creation of the key with the signed string
            SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            CryptographicKey myKey = provider.CreateSymmetricKey(signedData);

            // Encryption of the data using the key created (mKey)
            var pb = ps.ReadUtf8();
            IBuffer buffClear = CryptographicBuffer.CreateFromByteArray(pb);
            IBuffer buffProtected = CryptographicEngine.Encrypt(myKey, buffClear, null);
            buffClear = null;
            MemUtil.ZeroByteArray(pb);
            return CryptographicBuffer.EncodeToBase64String(buffProtected);
        }

        /// <summary>
        /// Decypts a string using a key signed by a KeyCredential.
        /// </summary>
        /// <param name="strProtected">Text to decrypt.</param>
        /// <param name="rResult">KeyCredential object used to sign a key to encrypt the text.</param>
        /// <returns>Decrypted text.</returns>
        internal static async Task<ProtectedString> Decrypt(string strProtected, KeyCredentialRetrievalResult rResult)
        {
            // The same text must be used to decrypt the data
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            var id = attribute.Value;
            IBuffer buffMsg = CryptographicBuffer.ConvertStringToBinary(id, encoding);

            // Start a background thread to ensure Windows Security prompt is opened in foreground
            var _ = Task.Factory.StartNew(() => EnsureForeground());

            // The actual Signing of the string
            KeyCredentialOperationResult opResult = await rResult.Credential.RequestSignAsync(buffMsg);

            if (opResult.Status == KeyCredentialStatus.Success)
            {
                IBuffer signedData = opResult.Result;

                // Creation of the key with the signed string
                SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
                CryptographicKey myKey = provider.CreateSymmetricKey(signedData);

                // Decryption of the data using the key created (mKey)
                IBuffer buffProtected = CryptographicBuffer.DecodeFromBase64String(strProtected);
                CryptographicBuffer.CopyToByteArray(CryptographicEngine.Decrypt(myKey, buffProtected, null), out var ba);
                ProtectedString ps = new ProtectedString(true, ba);
                MemUtil.ZeroByteArray(ba);
                return ps;
            }
            else
            {
                WinHelloUnlockExt.opened = true;
                WinHelloErrors(opResult.Status, "Error decrypting the data: ");
                return null;
            }
        }

        /// <summary>
        /// Must be executed as background process, right before calling Windows Security Prompt.
        /// </summary>
        internal static void EnsureForeground()
        {
            IntPtr ptrFF = Library.FindWindow("Credential Dialog Xaml Host", null);
            while(true)
            {
                if (ptrFF != IntPtr.Zero)
                {
                    do
                    {
                        Library.SetForegroundWindow(ptrFF);
                        Library.ShowWindow(ptrFF, 5);
                        Thread.Sleep(100);
                    } while (Library.FindWindow("Credential Dialog Xaml Host", null) != IntPtr.Zero);
                    
                    break;
                }
                else ptrFF = Library.FindWindow("Credential Dialog Xaml Host", null);
                Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Check if WinHelloUnlock data exists for the given Database path.
        /// </summary>
        /// <param name="dbPath">Database path.</param>
        /// <returns>True if a KeyCredential and a PasswordCredential exist for the Database path.</returns>
        internal static async Task<bool> FirstTime(string dbPath)
        {
            try
            {
                KeyCredentialRetrievalResult result = await OpenCredential(dbPath);
                PasswordVault myVault = new PasswordVault();
                PasswordCredential cred = myVault.Retrieve(dbPath, WinHelloUnlockExt.ProductName);
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

        /// <summary>
        /// Creates the data for WinHelloUnlock to work.
        /// 1. A Key Credential to sign a cryptographic key.
        /// 2. A Password vault to save the data into
        /// 3. A Password Credential in which to save the encrypted data (using the signed cryptographic key).
        /// </summary>
        /// <param name="dbPath">Database path. This is the identity of the database, if Database is moved or renamed,
        /// WinHelloUnlock will not work and new data needs to be created.
        /// </param>
        /// <returns>True if all the data was saved successfully.</returns>
        internal static async Task<bool> CreateHelloData(string dbPath)
        {
            
            bool isHelloAvailable = await UWPLibrary.IsHelloAvailable();
            if (isHelloAvailable)
            {

                KeyCredentialCreationOption optionNew = KeyCredentialCreationOption.ReplaceExisting;
                KeyCredentialRetrievalResult retrievalResult = await UWPLibrary.CreateCredential(dbPath, optionNew);

                if (retrievalResult.Status == KeyCredentialStatus.Success)
                {
                    KeyList keyList = Library.GetKeys(WinHelloUnlockExt.database);
                    string resultSave = await UWPLibrary.SaveKeys(dbPath, keyList, retrievalResult);
                    if (resultSave == "Success")
                    {
                        MessageService.ShowInfo("Database Keys saved successfuly");
                        return true;
                    }
                    else MessageService.ShowWarning("Error saving the composite key: " + resultSave);
                }
                else
                {
                    WinHelloErrors(retrievalResult.Status, "Error creating the credential: ");
                }
            }
            else
            {
                MessageService.ShowWarning("Windows Hello is NOT Available");
            }
            return false;
        }

        /// <summary>
        /// Performs the actual unlock of the database
        /// </summary>
        /// <param name="ioInfo">IOConnectionInfo that represents the Database.</param>
        internal static async void UnlockDatabase(IOConnectionInfo ioInfo)
        {
            string dbPath = Library.CharChange(ioInfo.Path);
            
            KeyCredentialRetrievalResult retrievalResult = await UWPLibrary.OpenCredential(dbPath);
            if (retrievalResult.Status == KeyCredentialStatus.Success)
            {
                KeyList keyList = await RetrieveKeys(dbPath, retrievalResult);

                CompositeKey compositeKey = Library.ConvertToComposite(keyList);
                WinHelloUnlockExt.opened = true;
                WinHelloUnlockExt.Host.MainWindow.OpenDatabase(ioInfo, compositeKey, true);
                string oPath = Library.CharChange(WinHelloUnlockExt.Host.Database.IOConnectionInfo.Path);
                if (oPath != dbPath)
                {
                    string str = WinHelloUnlockExt.ProductName + " could not unlock this database." +
                        " MasterKey may have changed. Delete " + WinHelloUnlockExt.ProductName + " data?";
                    if (MessageService.AskYesNo(str, WinHelloUnlockExt.ShortProductName))
                        DeleteHelloData(dbPath);
                    WinHelloUnlockExt.Host.MainWindow.OpenDatabase(ioInfo, null, false);
                }
                compositeKey = null;

                keyList = new KeyList(null, null);
            }
            else
            {
                WinHelloErrors(retrievalResult.Status, "Error unlocking database: ");
                WinHelloUnlockExt.Host.MainWindow.OpenDatabase(ioInfo, null, false);
            }
        }

    }
}