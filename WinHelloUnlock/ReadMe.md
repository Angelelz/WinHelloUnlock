Unlock KeePass 2 Databases with Windows Hello
=============================================

This plugin for [KeePass 2][KeePass] password manager is intended to conveniently Unlock your database using biometrics with [Windows Hello technology][WinHello].

[KeePass]: https://keepass.info/
[WinHello]: https://support.microsoft.com/en-us/help/17215/windows-10-what-is-hello

This plugin is heavily based on [KeePassWinHello][KeePassWinHello] and [KeePassQuickUnlock][QuickUnlock]. I am not a programmer, so I copied most of the code from them, but implemented a different way of storing the MasterKey data to allow plugin functionality, even afer rebooting your computer, using Password Vault, Pasword Credentials and Key Credentials from Windows UWP APIs.

Usage
-----

With this plugin you may:

Unlock your database using Biometric via Windows Hello. Even after completly closing KeePass or rebooting your PC.

Systems Requirements
--------------------

This plugin relies on Windows Hello API and its [requirements][WinHelloReq].

Tested on HP Spectre x360 with KeePass 2.42.1.

[WinHelloReq]: https://www.microsoft.com/en-US/windows/windows-10-specifications

How to Install
--------------

Place [WinHelloUnlock.dll][binLink] into `Plugins` folder in your KeePass installation
*(by default is `C:\Program Files (x86)\KeePass Password Safe 2`)*.

[binLink]: https://github.com/sirAndros/KeePassWinHello/releases "Plugin Releases"

Setup
-----

After instalation, open your database and unlock it using your composite key. Unlocking with any combination of Password/KeyFile/WindowsUserAccount is supported.

<img src="https://github.com/sirAndros/KeePassWinHello/blob/master/Screenshots/KeePassLockOptions.png?raw=true" width=500/>

When your database is unlocked, you will be asked if you want to set up WinHelloUnlock.

<img src="https://github.com/sirAndros/KeePassWinHello/blob/master/Screenshots/KeePassLockOptions.png?raw=true" width=500/>

A Windows Hello prompt will be shown to crypographically sign and encrypt your Master Key data.

<img src="https://github.com/sirAndros/KeePassWinHello/blob/master/Screenshots/KeePassLockOptions.png?raw=true" width=500/>

You should recieve a confirmation after a successfull set up.

<img src="https://github.com/sirAndros/KeePassWinHello/blob/master/Screenshots/KeePassLockOptions.png?raw=true" width=500/>

Options
-------

The plugin integrates itself into the KeePass settings dialog.

<img src="https://github.com/sirAndros/KeePassWinHello/blob/master/Screenshots/Options.png?raw=true" width=600/>

Available settings:

* Enable or disable the plugin for this particular database. If you disable it, you will not be asked to set WinHelloUnlock everytime you unlock your database.
* Create or delete WinHelloUnlock data for this particular database.

Notes
-----

No sensitive information including master passwords for databases are stored by the plugin in a plain text. A database key is encrypted and decrypted using Windows Hello API in order to unlock the database.
KeePass Composite Key data is *Encrypted* with a *Cryptographic Key* signed with a *Windows Hello Key Credential* and saved to a *Password Vault*.
To decrypt this data, you need to:
* Have access to the Password Vault
* Have access to the Cryptographic Key
* Be able to Cryptographicaly sign the Cryptographic Key with Windows Hello

So basically, you need to have access to the data, the hardware and the biometrics (or pin).

Credits
-------

* _Microsoft_ for [Windows Hello][WinHello] technology
* _JanisEst_ and his [KeePassQuickUnlock](https://github.com/JanisEst/KeePassQuickUnlock) for inspiration