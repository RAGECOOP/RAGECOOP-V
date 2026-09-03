

# 🌐 RAGECOOP

[![Downloads][downloads-shield]][downloads-url]
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]


# 🧠 That's it

RAGECOOP brings multiplayer experience to the story mode, you can complete missions together with your friends, use mods without any restriction/getting banned, or just mess around with your fella!

# 👁 Requirements
- ScriptHookV
- ScriptHookVDotNet 3.7.0
- .NET Framework 4.8 Runtime or SDK

# 📋 Building the project

You'll need:
- .NET 6.0 SDK
- .NET Framework 4.8 SDK

Recommended IDE:
- Visual Studio Code
- Visul Studio 2022

Then run `dotnet build` in the solution directory, built binaries are in the `bin` folder

# 📚 Third-party libraries
- [ScriptHookVDotNet3](https://github.com/crosire/scripthookvdotnet)
- [LemonUI.SHVDN3](https://github.com/justalemon/LemonUI)
- Lidgren Network Custom
- - No new features (only improvements)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [ClearScript](https://github.com/microsoft/ClearScript)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)
- [DotNetCorePlugins](https://github.com/natemcmaster/DotNetCorePlugins)

# 👋 Features

1. Synchronized bullets
2. Synchronized vehicle/player/NPC
3. Synchronized projectiles
4. Simple ragdoll sync
5. Decent compatibility with other mods, set up a private modded server to have some fun!
6. Weaponized vehicle sync(WIP).
7. Optimization for high-Ping condition, play with friends around the world!
8. Powerful scripting API and resource system, easily [add multiplayer functionality to your mod](HTTPS://docs.ragecoop.com).

# ⚠ Known issues

See [Bugs](https://github.com/RAGECOOP/RAGECOOP-V/issues/33)

# 🐛 Important: SHVDN Timeout Configuration

If RageCoop terminates on startup with the following error in the SHVDN console:

```
[ERROR] Blocking script! Script RageCoop.Client.Main was terminated because it caused the game to freeze too long.
```

You need to increase the SHVDN script timeout threshold.

## Fix

1. Navigate to your **GTA V root directory** (where `GTA5.exe` is located)
2. Open **`ScriptHookVDotNet.ini`** in a text editor
3. Find the line:
   ```
   ScriptTimeoutThreshold=5000
   ```
4. Change it to:
   ```
   ScriptTimeoutThreshold=15000
   ```
5. Save the file and restart GTA V

## Why does this happen?

RageCoop initializes several systems on first load — including voice chat and network components — which can take longer than SHVDN's default 5-second timeout allows. Increasing the threshold to 15 seconds gives RageCoop enough time to finish initializing without being terminated.

# 🔫 Installation
Refer to the [wiki](https://github.com/RAGECOOP/RAGECOOP-V/wiki)

# 🧨 Downloads

Download latest release [here](https://github.com/RAGECOOP/RAGECOOP-V/releases/latest)

You can also download nightly builds [here](https://github.com/RAGECOOP/RAGECOOP-V/releases/nightly), which includes latest features and bug-fixes, but has not been thoroughly tested.

Please note that this is incompatible with all previous versions of ragecoop, remove old files before installing.



# 🦆 Special thanks to

- [Makinolo](https://github.com/Makinolo), [oldnapalm](https://github.com/oldnapalm)
- - For testing, ideas, contributions and the first modification with the API
- [crosire](https://github.com/crosire)
- - For the extensive work in ScriptHookVDotNet
- [justalemon](https://github.com/justalemon)
- - For the extensive work in LemonUI

# 📝 License

This project is licensed under [MIT license](https://github.com/RAGECOOP/RAGECOOP-V/blob/main/LICENSE)

[downloads-shield]: https://img.shields.io/github/downloads/RAGECOOP/RAGECOOP-V/total?style=for-the-badge
[downloads-url]: https://github.com/RAGECOOP/RAGECOOP-V/releases
[contributors-shield]: https://img.shields.io/github/contributors/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[contributors-url]: https://github.com/RAGECOOP/RAGECOOP-V/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[forks-url]: https://github.com/RAGECOOP/RAGECOOP-V/network/members
[stars-shield]: https://img.shields.io/github/stars/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[stars-url]: https://github.com/RAGECOOP/RAGECOOP-V/stargazers
[issues-shield]: https://img.shields.io/github/issues/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[issues-url]: https://github.com/RAGECOOP/RAGECOOP-V/issues


