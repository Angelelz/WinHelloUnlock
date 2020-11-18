# [WinHelloUnlock v1.6](https://github.com/Angelelz/WinHelloUnlock/releases/tag/v1.6)

- PR [#50](https://github.com/Angelelz/WinHelloUnlock/pull/50). Disable plugin after declining first time configuration. User has to now manually enable the plugin via options menu.
- Fix [#48](https://github.com/Angelelz/WinHelloUnlock/issues/48). If user cancels the Windows Hello Prompt, return directly to the regular database unlock dialog.
- Fixed database loading taking long time if user cancels the Windows Hello Prompt (see [#48](https://github.com/Angelelz/WinHelloUnlock/issues/48)) and the Secure Desktop option was enabled.