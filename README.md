# AutoAqua

**Automatic water-filter pitcher handling for *Probably Stolen*.**

Every morning, AutoAqua finds the water-filter pitchers that have finished purifying (99% or better), pours them into your collection jug, and refills each pitcher from the faucet so the purification cycle starts again. No clicking, no scanning, no walking back and forth between machines.

> **Tested with:** Probably Stolen build 0.46D (Unity 2021.3.45f2, Mono), MelonLoader 0.7.3, Windows 11.
> Other game versions may work, but a game update can break any mod. See [Compatibility](#compatibility-and-limitations).

---

## Contents

- [What it does](#what-it-does)
- [What it will never do](#what-it-will-never-do)
- [Requirements](#requirements)
- [Installation](#installation)
- [Setting up your jugs (read this once)](#setting-up-your-jugs-read-this-once)
- [Settings](#settings)
- [The F9 hotkey](#the-f9-hotkey)
- [Pure-water setups](#pure-water-setups)
- [Reading the log](#reading-the-log)
- [Troubleshooting](#troubleshooting)
- [How it works](#how-it-works)
- [Compatibility and limitations](#compatibility-and-limitations)
- [Uninstalling](#uninstalling)
- [Building from source](#building-from-source)
- [Credits](#credits)
- [Disclaimer](#disclaimer)

---

## What it does

If you run water purifiers, you know the loop: sleep, check each pitcher, pour the finished ones into a big jug, refill the pitchers from the faucet, sleep again. AutoAqua does the middle part for you.

When a new day starts, for every water pitcher sitting in one of your purifier machines:

1. **Is the water finished?** The pitcher must hold high-quality water (99.00% to 99.89% purity).
2. **Is there a suitable jug with room?** The jug must already hold high-quality water and have space for the whole pitcher.
3. If both are true, the pitcher is **poured into the jug**, then **refilled from the faucet**, using the game's own pour and faucet actions.

Pitchers that are still purifying (rust, ghost or base water) are left alone to keep filtering. The whole routine takes a few hundredths of a second.

If you have two or more collection jugs, AutoAqua fills the fuller one until the next pitcher no longer fits, then moves on to the next, so a second jug works as overflow automatically.

### Water tiers

Purity in the game is stored in hundredths of a percent (9900 = 99.00%). AutoAqua uses the game's own tiers:

| Tier | Purity | What AutoAqua does |
|---|---|---|
| Pure | 99.90% and above (9990+) | **Never touched**, in pitchers or jugs |
| High-Quality | 99.00% to 99.89% (9900 to 9989) | **Collected**: poured into a collection jug |
| Base | 98.00% to 98.99% | Left in the machine to keep filtering |
| Ghost | 96.00% to 97.99% | Left in the machine to keep filtering |
| Rust | 90.00% to 95.99% | Left in the machine to keep filtering |
| Gutterflow | below 90% | Left alone |

---

## What it will never do

AutoAqua is built to be conservative with your water:

- It **never moves or mixes Pure water.** A pitcher at 99.9%+ and a jug holding Pure water are skipped.
- It **never pours into an empty jug on its own.** An empty jug could be your Pure Water jug, and the mod cannot tell. A jug counts as a collection jug only when it already holds 99%+ water (or you list it yourself, see [Settings](#settings)).
- It **never mixes tiers.** High-quality water only goes into a jug that already holds high-quality water (or one you list yourself).
- It **never pours into a jug that cannot hold the whole pitcher.** If nothing has room, the pitcher stays full, is not refilled, and the log tells you.
- It **never refills a pitcher it did not pour.** Empty pitchers, such as a hand-fed pure-water pitcher, are skipped.
- It **ignores spare pitchers** that are not in a machine slot (in storage, bags or on the counter), unless you change `OnlyInMachines`.
- It **does nothing if there is no faucet** in the room (unless you change `RequireFaucet`).

---

## Requirements

- *Probably Stolen* on Steam (Windows, 64-bit).
- [MelonLoader](https://github.com/LavaGang/MelonLoader) **0.7.3** (the Open-Beta that was tested).
- The faucet in the room. It is a scene fixture (the centrifuge-looking device on the left side of the room); AutoAqua needs it to refill pitchers.

---

## Installation

### 1. Install MelonLoader

Download the MelonLoader installer from the [MelonLoader releases](https://github.com/LavaGang/MelonLoader/releases), point it at `Probably Stolen.exe`, choose version 0.7.3 and install. Launch the game once so MelonLoader can set itself up (the first launch takes longer).

### 2. Fix the Harmony conflict (if MelonLoader fails to start)

*Probably Stolen* ships its own, older `0Harmony.dll`. On the tested setup this stopped MelonLoader from starting. If `MelonLoader\Latest.log` shows

```
Failed to initialize MelonLoader
System.TypeLoadException: ... HarmonyLib.Tools.Logger ... 0Harmony, Version=2.10.2.0
```

rename the game's copy so MelonLoader's newer one is used. In PowerShell, from the game folder:

```powershell
Rename-Item "Probably Stolen_Data\Managed\0Harmony.dll" "0Harmony.dll.bak"
```

Notes:

- This renames a game file. In testing the game ran normally afterwards.
- Steam's **Verify integrity of game files** will restore the original. If you run it, rename the file again.
- To undo it, rename `0Harmony.dll.bak` back to `0Harmony.dll`.

### 3. Install AutoAqua

Extract the release zip into the game folder (the one that contains `Probably Stolen.exe`), so you end up with:

```
Probably Stolen\
  Mods\
    AutoAqua.dll
```

Or just copy `AutoAqua.dll` into the `Mods` folder by hand.

### 4. First launch

Start the game. In `MelonLoader\Latest.log` you should see:

```
AutoAqua v1.0.0
[AutoAqua] AutoAqua 1.0.0 loaded (DryRun=False, hotkey=F9)
```

MelonLoader writes the settings file (`UserData\MelonPreferences.cfg`) when the game closes, so the `[AutoAqua]` section appears after your first exit.

---

## Setting up your jugs (read this once)

AutoAqua only pours into a jug that **already holds high-quality water** (99.00% to 99.89%). This is deliberate, so it can never fill your Pure Water jug by accident.

- **Your main jug** probably qualifies already.
- **A second jug for overflow:** pour a little 99%+ water into it by hand, once. From then on it counts as a collection jug.
- **Do not pour 99% water into your Pure Water jug.** Water above 99.90% stays Pure and is never touched, and AutoAqua cannot undo mixing you do by hand.

If you prefer to name your jugs explicitly, use `HqJugUids` (see below). Use the `Report` setting to see the uid of every jug.

---

## Settings

Settings live in `Probably Stolen\UserData\MelonPreferences.cfg`, in the `[AutoAqua]` section. **Edit the file while the game is closed**: MelonLoader rewrites it on exit. Text values go in quotes.

| Setting | Default | What it does |
|---|---|---|
| `DryRun` | `false` | `true` = only log what *would* happen and change nothing. Good for a first look. |
| `Verbose` | `false` | `true` = log every pitcher and why it was skipped. The default log is one summary plus one line per pitcher handled. |
| `Report` | `false` | `true` = print a setup report (all pitchers and jugs with their uids) 45 seconds after the shop loads. |
| `RequireFaucet` | `true` | `true` = do nothing if the faucet is not in the room. |
| `OnlyInMachines` | `true` | `true` = only handle pitchers sitting in a machine slot. Set `false` to also handle loose pitchers. |
| `MinPurity` | `9900` | Pour threshold in hundredths of a percent. `9900` = 99.00%. |
| `PureAt` | `9990` | Purity at which water counts as Pure and is never touched. `9990` = 99.90%. |
| `HqJugUids` | `""` | Optional override: comma-separated jug uids, in priority order (first = main, rest = overflow). Empty = automatic. |
| `IgnoreUids` | `""` | Optional: comma-separated uids of pitchers **or machines** to leave alone. |
| `TriggerKey` | `"F9"` | Key that runs the routine immediately. Any Unity `KeyCode` name, such as `"F8"`. Empty = disabled. |

### Finding uids

Set `Report = true`, load a save, wait about a minute, and quit. `MelonLoader\Latest.log` then contains lines like:

```
[report] water_pitcher x7
[report]   id=water_pitcher uid=1608 purity=9900 ml=3000 free_uL=0 inMachine=True machine=water_purifier#2185
[report] water_tank x3
[report]   id=water_tank uid=2887 purity=9900 ml=8200 free_uL=11800000 inMachine=False machine=storage_bay_large#22808
```

The number after `uid=` identifies the item, and the number after `#` identifies the machine or container it sits in. Either can go in `IgnoreUids`; only jug uids go in `HqJugUids`. Uids are specific to your save.

---

## The F9 hotkey

Pressing the trigger key (default **F9**) runs the exact same routine as the start-of-day hook, immediately. Useful to see what the mod will do, or to collect water mid-day.

- It makes **real changes** unless `DryRun` is `true`.
- In testing, the game only wrote its save when a new day began, so quitting without sleeping discards anything done with F9. If you want the result kept, play on and sleep as usual.
- Set `TriggerKey = ""` to turn the hotkey off, for example if F9 clashes with another mod.

---

## Pure-water setups

AutoAqua is built to coexist with a hand-fed pure-water setup:

- Pitchers and jugs at **99.90% or higher are never touched**.
- **Empty pitchers are skipped.** A pure-water pitcher that you empty and refill by hand is left alone.
- Your **Pure Water jug is never a target**, even when empty, because empty jugs are never used automatically.

One case needs a setting: if a machine of yours takes water through the 99.00% to 99.89% range *overnight and finishes pure later*, AutoAqua cannot know the pitcher is not done. Put that machine's uid in `IgnoreUids`. (On the tested save the pure machine went from 99.00% to 99.90% in a single night, so this never came up.)

---

## Reading the log

AutoAqua writes to the MelonLoader console and to `MelonLoader\Latest.log`. A normal morning looks like this:

```
[AutoAqua] [morning] pitchers=7 jugs=3 faucet=True dry=False mode=auto
[AutoAqua] [morning] pitcher uid=1608: poured 3000 ml (purity 9900) into jug uid=2887, refilled from the faucet
[AutoAqua] [morning] pitcher uid=2518: poured 3000 ml (purity 9900) into jug uid=2887, refilled from the faucet
[AutoAqua] [morning] summary: 2 poured
```

Useful things to search for:

- `[morning]` is the start-of-day routine (also printed when you press the hotkey, preceded by `[manual]`).
- `WOULD pour` appears in dry-run mode.
- `WARNING` lines mean something needs your attention, such as jugs that are full.
- `[report]` is the optional setup report.

Everything is quiet by default. Turn on `Verbose` to see why each pitcher was skipped.

---

## Troubleshooting

**Nothing happens in the morning.**
Open `MelonLoader\Latest.log` and search for `AutoAqua`.

- No AutoAqua lines at all: MelonLoader did not load the mod. Check that `AutoAqua.dll` is in the game's `Mods` folder and that MelonLoader started (see the Harmony fix above).
- `no faucet found in the room`: the faucet is not in your room, or the mod could not find it. Set `RequireFaucet = false` to pour without refilling, which is usually not what you want.
- `waiting for jug space` or `no collection jug has room`: your jugs are full, or none holds 99%+ water yet. See [Setting up your jugs](#setting-up-your-jugs-read-this-once).
- A summary of `0 poured` with no warnings: your pitchers are still filtering, or not yet 99%. Turn on `Verbose` for the reason for each pitcher.

**A pitcher was not poured.**
It must be at 9900 or higher and below 9990, inside a machine slot, not in `IgnoreUids`, and a jug must have room. `Verbose` prints the reason per pitcher.

**A jug I wanted was not used.**
It has to already hold high-quality water, unless you list it in `HqJugUids`. Check its purity with the in-game scanner or `Report`.

**MelonLoader will not start / `TypeLoadException` about Harmony.**
Rename the game's `0Harmony.dll` as described in [Installation](#2-fix-the-harmony-conflict-if-melonloader-fails-to-start).

**The game does not exit cleanly after I quit.**
Not seen with this release in testing. Early development builds that scanned game objects at startup were followed by hangs on quit (the exact cause was never pinned down), and the release does not do that. If it happens to you, remove `AutoAqua.dll` and check whether it still happens, then report it with your `Latest.log`.

---

## How it works

- A Harmony postfix on the game's `PlayerStore.BeginDay` runs the routine when a new day starts, after the overnight purification.
- Items are found through `PlayerStore.FindAllItem`. Pitchers and jugs are recognised by their game identifiers (`water_pitcher`, `water_tank`), so nothing depends on your save.
- Purity, volume and free space come from the game's own `WaterHelper` functions.
- Pouring and refilling call the game's own `GameItem.Target` action (the same thing dragging an item onto another does), after checking the game's `CanTarget`.
- The faucet is read from the room's `EmporiumEntry` object.
- The mod runs once per new day (and when you press the hotkey) and takes a few hundredths of a second.

**A note on honesty about information:** AutoAqua reads the exact internal purity value rather than operating the in-game purity scanner. It does not use or consume a scanner.

---

## Compatibility and limitations

- Developed and tested on build **0.46D**. The mod depends on internal names (`PlayerStore.BeginDay`, `WaterHelper`, `EmporiumEntry`, `GameItem.Target`), so a game update that renames them can break it. If it stops working after an update, check the log and report the game version.
- Only the tested water items are handled: `water_pitcher` pitchers and `water_tank` jugs.
- It does not look at the labels you put on containers. It decides from the actual water.
- Pitchers that are not in a machine slot are ignored by default.
- Single-player only.
- The automatic morning changes are part of the day's normal save. Changes made with the hotkey follow the same rule: they are kept if you sleep, discarded if you quit first.

---

## Uninstalling

Delete `Mods\AutoAqua.dll`. Water already moved stays where it is. You can also delete the `[AutoAqua]` section from `UserData\MelonPreferences.cfg`.

---

## Building from source

You need the .NET SDK (8.0 or newer is fine) and a copy of the game with MelonLoader installed (the project references the game's and MelonLoader's assemblies, which are **not** included in this repository).

```powershell
dotnet build -c Release
```

If your game is not at `C:\Games\Probably Stolen`:

```powershell
dotnet build -c Release -p:GamePath="D:\Games\Probably Stolen"
```

The build copies `AutoAqua.dll` into the game's `Mods` folder when it exists. To make a release zip:

```powershell
.\package.ps1 -Version 1.0.0
```

The result is `dist\AutoAqua-1.0.0.zip`, laid out so it can be extracted straight into the game folder.

---

## Credits

- **Ev**: concept, in-game testing, and the long logs that made it work.
- Code written with AI assistance from **Claude** (Anthropic), working step by step from the game's own logs and from Ev's testing.
- [MelonLoader](https://github.com/LavaGang/MelonLoader) by LavaGang and [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike.

---

## Disclaimer

AutoAqua is an unofficial fan modification. It is not affiliated with or endorsed by Questing Goose Studio or tinyBuild. A game running MelonLoader is modified; **do not report issues with a modded game to the game's developers.** Report problems with this mod to its author. Use at your own risk, and consider backing up your saves (the game keeps them under `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen`).
