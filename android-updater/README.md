# Revamped Updater for DroidDeck

A small native Android client for updating an **already prepared** Revamped Windows game inside DroidDeck. It downloads normal game releases from this repository; no GitHub account is needed.

## Connect once

1. Open **Revamped Updater → Connect DroidDeck**.
2. Android Files opens at DroidDeck. Tap **Use this folder → Allow**. You do not need to create a folder or browse through Linux directories.
3. The updater reads Steam's library catalogue, finds `20MinuteTillDawn`, verifies the supported installation and remembers its location. If there are several installed copies, choose the intended one. The rune transition needs the DroidDeck/Steam-library grant so it can also reach that game's own Proton saves; a game-folder-only grant asks you to reconnect before making changes.
4. Tap **Check GitHub**, close the game in DroidDeck, then tap **Install**. After verification, **Open DroidDeck** returns to the launcher.

This uses DroidDeck's Files provider, not direct access to another app's private data. Root, Shizuku, all-files permission, USB and DroidDeck debugging are unnecessary for ordinary use. Android may require another grant if the app is reinstalled or its saved permission is revoked.

## What it updates

The client verifies GitHub's archive digest, the package inventory and its `UPDATE.json` contract. It checks the existing installed file set before changing the owned mod DLL and installation record. Already prepared artwork, loader, original game, settings and Wine registry are retained.

**Updater 0.2.0 is required for game 1.3.0 and later.** Crossing the character-rune transition resets this game's native progress, achievements/rune journal and Continue checkpoint once, including recognized partial/backup save copies. The button explicitly says **Reset saves & install**. The exact files are recorded before deletion, verified on recovery, and limited to this game's profile inside its own Steam app-1966900 prefix. Other games, unrelated profile files and mod settings remain. The completed epoch is stored with the installation; later and repeated updates preserve new progress. Close the game before updating and do not clear updater data while recovery is pending.

A release requiring different prepared assets or loader files reports that Windows preparation is needed **before changing anything**. The APK does not contain the proprietary game, extracted artwork or a universal Windows-to-Android converter. Prepare a new installation with [INSTALL.md](../INSTALL.md) first.

Updates are explicitly initiated in the foreground. An interrupted deployment retains its verified payload and recorded originals; reopen the client and use **Finish interrupted update**. Do not delete its app data during an unfinished update.

## Build

Requires Python 3.11+, JDK 21 and an existing Android SDK with platform 36 and build-tools 35.0.0. The native Java application has no Gradle or third-party runtime dependencies. Minimum Android version is 8.0 / API 26.

Use a persistent private PKCS12 signing key with alias `revamped-updater`. Supply its password through `REVAMPED_KEYSTORE_PASSWORD`; keep the same key for future APK updates. Signing keys and passwords must remain outside the repository.

```powershell
python -B android-updater/build.py --work D:/Temp/revamped-updater-build --output D:/Temp/revamped-updater-package --sdk C:/path/to/Android/Sdk --jdk C:/path/to/jdk --keystore D:/private/release.p12
```

`--work` must name a new directory. The output includes the signed APK, certificate verification and source-file digest receipt. Add `--tests` to build the separate instrumentation APK; it is never distributed with the client. Its recovery fixtures live only in the temporary test app's private storage. Native UI checks invoke the production buttons, while `delivery-audit` reads the connected game and preservation hashes without changing it.

## Design

The user's selected **B layout with A colors** uses opaque Revamped purple, coral, cream and sage surfaces. One main action follows connection, download, installation and completion. The original hourglass launcher mark belongs to this utility; the folder row uses DroidDeck's actual installed icon. [Approved interactive prototype](design/approved.html) · [Design contract](design/brand-spec.md).
