# DroidDeck deployment

Revamped 1.0.0 targets the Windows Steam build 12679152 (Unity Mono x64). A DroidDeck 0.5.0 installation of that build passed comparison of all 142 supported original files against the release installer contract. The installed PC payload also matched its 538-file ownership manifest and published source commit `cead67c93aecc48012cdaf8847b760f8eccae998`.

Compatibility and preservation inspection is complete. Phone installation and actual Proton activation are pending; matching Windows files alone does not prove successful runtime loading.

DroidDeck exposes its private Linux filesystem through its Android Files document provider. Its library cache identifies the game's directory and its own `compatdata/1966900/pfx` Proton prefix. Preserve that prefix and its existing saves; do not copy the PC profile. Configure `winhttp` as `native,builtin` only for `MinutesTillDawn.exe` in the game's prefix so Wine loads the Windows BepInEx bootstrap. Existing overrides and other games must remain unchanged.

The same published 1.0.0 payload is being deployed. This does not change gameplay or create another software release. Device identifiers, recovery registry copies, game artwork and profile evidence remain local and are not published.

References: [DroidDeck Files provider](https://github.com/Droid-Deck/DroidDeck/blob/main/app/src/main/java/com/droiddeck/launcher/files/AppFilesProvider.kt), [agent control](https://github.com/Droid-Deck/DroidDeck/blob/main/docs/agent-control.md).
