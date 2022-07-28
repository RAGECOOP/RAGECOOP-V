

# 🌐 RAGECOOP
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]


# 🧠 That's it
RAGECOOP is a multiplayer mod to play story mode or some mods made for RAGECOOP or just drive around with your buddy.



# 📋 Requirements
- Visual Studio 2022
- .NET 6.0
- .NET Framework 4.8

# 📚 Libraries
- [ScriptHookVDotNet3](https://github.com/crosire/scripthookvdotnet/releases/tag/v3.4.0)
- [LemonUI.SHVDN3](https://github.com/justalemon/LemonUI/releases/tag/v1.6)
- Lidgren Network Custom (***PRIVATE***)
- - No new features (only improvements)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/13.0.1)
- [ClearScript](https://github.com/microsoft/ClearScript)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)
- [DotNetCorePlugins](https://github.com/natemcmaster/DotNetCorePlugins)

# Features

1. Synchronized bullets
2. Synchronized vehicle/player/NPC
3. Synchronized projectiles
4. Simple ragdoll sync
5. Smoother vehicle/ped movement.
6. Ownership based sync logic, carjacking is now working (sort of).
7. Latency compensation, vehicle movement should look identical from both sides even with high ping (300+ ms)
8. Code refactoring and namespace cleanup
9. Synchronized vehicle doors, brake and throttle.
10. Weaponized vehicle sync(WIP).
11. Other improvements

# Known issues

1. Weapon sounds are missing.
2. Cover sync is still buggy.
3. Framerate drop with high number of synchronized entities.


## Installation
Refer to the [wiki](https://github.com/RAGECOOP/RAGECOOP-V/wiki)

# Downloads

Download latest release [here](https://github.com/RAGECOOP/RAGECOOP-V/releases/latest)

You can also download nightly builds [here](https://github.com/RAGECOOP/RAGECOOP-V/releases/nightly), which includes latest features and bug-fixes, but has not been thoroughly tested.

Please note that this is incompatible with all previous versions of ragecoop, remove old files before installing.


# Support us

<a href="https://patreon.com/Sardelka"><img src="https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3DSardelka%26type%3Dpatrons&style=for-the-badge" /></a>

# 🦆 Special thanks to
- [Makinolo](https://github.com/Makinolo), [oldnapalm](https://github.com/oldnapalm)
- - For testing, ideas, contributions and the first modification with the API
- [crosire](https://github.com/crosire)
- - For the extensive work in ScriptHookVDotNet
- [justalemon](https://github.com/justalemon)
- - For the extensive work in LemonUI

# 📝 License
This project is licensed under [MIT license](https://github.com/RAGECOOP/RAGECOOP-V/blob/main/LICENSE)

[contributors-shield]: https://img.shields.io/github/contributors/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[contributors-url]: https://github.com/RAGECOOP/RAGECOOP-V/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[forks-url]: https://github.com/RAGECOOP/RAGECOOP-V/network/members
[stars-shield]: https://img.shields.io/github/stars/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[stars-url]: https://github.com/RAGECOOP/RAGECOOP-V/stargazers
[issues-shield]: https://img.shields.io/github/issues/RAGECOOP/RAGECOOP-V.svg?style=for-the-badge
[issues-url]: https://github.com/RAGECOOP/RAGECOOP-V/issues

