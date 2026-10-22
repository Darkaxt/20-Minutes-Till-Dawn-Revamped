# DroidDeck deployment

Revamped 1.0.0 targets the Windows Steam build 12679152 (Unity Mono x64). A DroidDeck 0.5.0 installation of that build passed comparison of all 142 supported original files against the release installer contract. The installed PC payload also matched its 538-file ownership manifest and published source commit `cead67c93aecc48012cdaf8847b760f8eccae998`.

The exact 1.0.0 payload was installed and launched successfully: BepInEx loaded the production mod, registered Charge Cannon/evolutions, displayed the maintenance footer and reached the native title and character menus. All original files and five existing phone profile files were unchanged at installation. This is a menu/bootstrap smoke test, not full gameplay or balance acceptance.

DroidDeck exposes its private Linux filesystem through its Android Files document provider. Its library cache identifies the game's directory and its own `compatdata/1966900/pfx` Proton prefix. Preserve that prefix and its existing saves; do not copy the PC profile. Configure `winhttp` as `native,builtin` only for `MinutesTillDawn.exe` in the game's prefix so Wine loads the Windows BepInEx bootstrap. Existing overrides and other games must remain unchanged.

The user clarified that the credit belongs in the actual pixel artwork. The integrated 1.0.1 artwork/ad-removal update and its final phone verification are currently in progress. Gameplay tuning remains unchanged. Device identifiers, recovery registry copies, game artwork and profile evidence remain local and are not published.

References: [DroidDeck Files provider](https://github.com/Droid-Deck/DroidDeck/blob/main/app/src/main/java/com/droiddeck/launcher/files/AppFilesProvider.kt), [agent control](https://github.com/Droid-Deck/DroidDeck/blob/main/docs/agent-control.md).
