# Install 1.2.0

Supported inputs: Windows Steam build 12679152 (Unity 2019.4.40f1 Mono x64), and Android 8.6 (1093) APKM with SHA-256 `c2336baac8f71d9c974e6e7b120225de0fb441463aecf4c1c6bfd7e2f6021d8f`.

Use your own Windows installation and Android package. Game artwork/data is prepared locally and is not included in this download. The installer does not upload either input. Python 3.14 x64 and .NET 10 are the tested prerequisites. The first preparation downloads pinned Python libraries, Il2CppDumper, BepInEx and the published Sprites Reborn texture source from their authors. No third-party mod DLL or AssetReplacer is installed.

Extract the release ZIP to a normal folder, close the game, and run from that folder in PowerShell, replacing the Android path:

```powershell
python installer/install.py install --game "D:/Steam/steamapps/common/20MinuteTillDawn" --android "D:/Downloads/your-8.6-1093.apkm" --work-dir "D:/Temp/20MTD-Revamped-Install"
python installer/install.py verify --game "D:/Steam/steamapps/common/20MinuteTillDawn"
```

Start the game normally through Steam. The installer checks all 142 original game files, creates an exact owned-file manifest, and rejects conflicting existing loader/mod files before changing them. Compatible existing loader files are reused and remain owned by their previous installation. Original game files, saves and settings are preserved. No test/validation assembly is included.

F2/Options cycles Off/Little/Extended metrics. Collected skills stay visible in centered rows, one per acquired upgrade path; no scrolling or count labels are needed. Original artwork is the default; Options can enable the compatible Sprites Reborn treatment and its palette extension. Pause → Save & return suspends a supported run. Custom runs award credits only and use obtained content. See [controls and limits](docs/CUSTOM.md) and [migration decisions](docs/MIGRATION.md).

## Upgrade from an earlier Revamped installation

Close the game. Use the old extracted release installer to uninstall its unchanged owned files, then run the new release installation command above. This also applies to earlier unpublished 1.0.1 checkpoints: matching version labels do not mean their source revision and payload match. Keep your existing profile and mod configuration. The installer deliberately rejects overwriting a different installed release; it does not silently replace shared or modified loader files.

## Uninstall / rollback

Keep this extracted release folder, then close the game and run:

```powershell
python installer/install.py uninstall --game "D:/Steam/steamapps/common/20MinuteTillDawn"
```

Only unchanged files explicitly owned by this installation are removed. Saves, companion progression/checkpoints, configuration, other mods and reused loader files remain. Modified owned files are reported and preserved for inspection. Empty directories created by the installer are removed only when they remain empty. Stock game files are never rollback targets.

An interrupted matching installation retains its pending file manifest; rerun the same install command to finish it. An incomplete asset preparation is preserved for diagnosis; use a new exact work directory to prepare again. Scratch contains dependencies and extracted local assets and is not required after installation/verification; installed assets are independent copies. Preserve it while diagnosing a failed preparation.

## Build from source

Prepare a copied development player with the supported owned inputs/BepInEx and set `MODERNIZATION_SCRATCH` to its root. Build against its `game/MinutesTillDawn_Data/Managed` and `game/BepInEx/core`. Asset preparation can run separately with `prepare` in place of `install`. `release/build.py` packages the Release configuration from committed source and a verified prepared-assets directory; extracted assets never enter the public ZIP.
