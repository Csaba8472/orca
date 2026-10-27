# Orca plugin for Logitech MX Creative Console

A [Logi Actions SDK](https://logitech.github.io/actions-sdk-docs) plugin. On the keypad's LCD keys it shows Orca worktrees with live agent status, and it adds keys for Orca navigation.

## Actions

**Worktrees › Worktree 1–15.** Each key is one sidebar position, with pinned worktrees first and then Orca's order. The key shows the repo and worktree name under a status band:

| Band   | Meaning                       |
| ------ | ----------------------------- |
| Yellow | Agent working                 |
| Orange | Agent waiting for input       |
| Green  | Agent done or terminal active |
| Red    | Agent failed                  |
| Grey   | Idle                          |

A white outline marks the worktree Orca is showing. Pressing a key runs `orca worktree activate`. The plugin polls `orca worktree ps --json` every 4 seconds while Orca is reachable and every 15 seconds while it isn't.

**Navigation.** New Worktree, Switch Worktree, Previous or Next Worktree, Worktree History Back or Forward, Toggle Sidebar, Toggle Right Sidebar, Source Control, Checks, Floating Terminal, New Terminal Tab and Close Tab. These keys send Orca's **default** shortcuts, so if you rebind an action in Orca Settings › Keyboard, its key stops working.

Orca's **Show Checks** has no default shortcut. Before using the Checks key, bind Show Checks to ⌘⌥⇧K (Ctrl+Alt+Shift+K on Windows) in Orca Settings › Keyboard.

## Requirements

- Logi Options+ (it installs Logi Plugin Service, which provides `PluginApi.dll`)
- .NET 10 SDK
- The Orca CLI. To install it, open Orca Settings › Orca CLI › Shell command and choose **Register**. The plugin also looks for it inside the Orca app bundle on macOS and in `%LOCALAPPDATA%\Programs\Orca` on Windows. To use a CLI in another location, set `ORCA_CLI_PATH`.

## Develop

```bash
cd integrations/logi-actions-plugin/src
dotnet build            # Debug: links bin/ into Logi Plugin Service and reloads the plugin
dotnet watch build      # rebuild on save
```

Plugin logs are written by Logi Plugin Service: `~/Library/Application Support/Logi/LogiPluginService/Logs/` on macOS and `%LOCALAPPDATA%\Logi\LogiPluginService\Logs\` on Windows.

## Package

```bash
dotnet tool install --global LogiPluginTool
dotnet build -c Release
logiplugintool pack ../bin/Release ./Orca.lplug4
logiplugintool verify ./Orca.lplug4
```
