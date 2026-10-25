# 20 Minutes Till Dawn — Revamped

Android 8.6 gameplay brought to the Windows game, with a transparent stats HUD, milestone unlocks and a native Custom mode.

A community mod for **20 Minutes Till Dawn by Flanne**, maintained by **Darkaxt**.

[Download v1.1.3](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.1.3) · [Android updater](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/updater-v0.1.0) · [Installation](INSTALL.md) · [Changelog](#changelog) · [Gameplay ledger](docs/PARITY.md)

![Native title screen with Flanne's original credit and MAINTAINED BY DARKAXT pixel lettering](docs/images/title-maintained.png)

*Actual Windows test capture. The maintenance credit is part of the pixel artwork; the Emberpath promotional panel is removed.*

## What changes

- **Added for Revamped:** transparent stats and skill HUD; thirty milestone unlocks and notifications; expanded rune progression; Save & return / Continue; native Custom settings and mode-default loading; optional artwork recolours.
- **Ported from Android 8.6:** Charge Cannon and its three evolutions; Gale and Bulwark with their exclusive upgrades; existing character abilities, combat, status effects, summons, stacking, cooldowns, shooting, event ordering, scenario schedules and mode behavior.
- **Fixed after the anomaly review:** repeated Continue bonuses, damage signs, frozen-only Shatter, temporary-buff accounting, pooled-effect reuse, exhausted reward menus, encounter/contact defects, character/summon lifetimes and inaccurate descriptions. The changelog below separates each release's ports and corrections.

The gameplay reference is **Android 8.6 / versionCode 1093**. All identified required gameplay differences in the completed comparison are integrated. Ads, purchases and other monetization are excluded. Revamped's progression, HUD, Custom mode and desktop controls are intentional additions. [Exact parity scope and source quirks](docs/PARITY.md) · [Bug-correction contracts](docs/BUGFIXES.md).

## See it in game

### Play the Android additions with desktop controls

Charge Cannon brings three weapon evolutions. Gale and Bulwark bring their character skills, animations and exclusive upgrades to the native Windows selection and combat flow.

![Diamond - Bulwark in the native Windows character selector, with its dash skill and portrait](docs/images/android-bulwark.png)

*Windows selection test using an unlocked test profile. The displayed Bulwark description and starting HP match the prepared v1.1.2 character data.*

### Read your build without a panel

Attributes sit on the left; collected skill icons, tiers and counts sit on the right. Values show the effective attribute followed by its base and net bonus, before charge or conditional effects. Toggle the HUD with **F2** or in Options.

![Transparent HUD with coloured native attribute icons on the left and collected skill tiers and counts on the right](docs/images/hud-build.png)

*Windows 1080p integration capture with the optional recolour enabled and a deliberately configured test build.*

### Unlock content through play

Characters, weapons and scenarios use thematic milestones instead of currency purchases. Conditions combine minibosses or bosses with Darkness, scenario and build requirements. For example, Diamond requires a Forest miniboss defeat while holding at least seven maximum red hearts. Existing ownership is preserved; earned unlocks generate queued notifications.

![Diamond's character menu showing the miniboss and seven maximum red hearts unlock condition](docs/images/unlock-diamond.png)

### Spend credits on runes

Each rank costs 20% more than the preceding rank, rounded upward. Enable multiple purchased runes in the same row. To buy a lower rune, fully upgrade the runes above it **in the same column**. The native five-rank cap remains.

![Native rune menu showing multiple enabled runes and a same-column prerequisite](docs/images/runes-progression.png)

### Build a Custom run

Use the game's own menus to adjust the run, weapon, attributes, enemies, summons, upgrades and objects. **Load mode defaults** imports the selected mode's real settings. Only obtained content is available. Custom runs award **credits only**: no unlocks, achievements or leaderboard submissions.

![Native Custom game menu with Run, Weapon, Attributes, Enemies, Summons, Upgrades and Objects tabs and Load mode defaults](docs/images/custom-mode-defaults.png)

[Full Custom control inventory and save behavior](docs/CUSTOM.md).

## Install or upgrade

You need your own **Windows Steam build 12679152** (Unity 2019.4.40f1 Mono x64) and the supported **Android 8.6 / 1093 APKM**. Python 3.14 x64 and .NET 10 are the tested preparation prerequisites.

1. Download and extract the [v1.1.3 release ZIP](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.1.3).
2. Close the game and follow [INSTALL.md](INSTALL.md), including its upgrade instructions if Revamped is already installed.
3. Launch the game normally through Steam.

The release contains the mod and installer recipes. It prepares the required artwork/data locally from your owned inputs; extracted game assets and validation assemblies are not bundled. Installation preserves the original game files, saves and configuration and supports exact owned-file rollback.

**DroidDeck:** v1.1.3 is installed on the tested phone. The Android updater found the existing game automatically and installed the public release while preserving current progress and settings. This runs the Windows mod inside DroidDeck. [Phone delivery and launch notes](docs/DROIDDECK.md).

### Update directly on Android

Install **Revamped Updater**, connect DroidDeck once through Android Files, then check and install compatible GitHub releases. It finds the game through Steam's library catalogue and remembers the folder; no custom folder, Linux browsing, root, Shizuku or GitHub login is needed.

![Revamped Updater native Android interface showing installed and latest game versions, release notes and DroidDeck folder](docs/images/updater-native.png)

*Actual native Android view capture. The updater verified the real GitHub package and upgraded the tested phone from1.1.2 to1.1.3. Existing artwork and loader files must already match the release; incompatible changes report that Windows preparation is required before writing. [Updater instructions and limits](android-updater/README.md).*

## Controls

| Action | Control / location |
|---|---|
| Move, aim, shoot and character skill | Native keyboard/mouse/controller bindings |
| Toggle auto-targeting | Space; native controller equivalent |
| Show/hide the stats HUD | F2 or Options |
| Scroll collected skill icons | Page Up / Page Down |
| Suspend a run | Pause → Save & return; Continue resumes it |
| Configure a Custom run | Custom selection; live settings from Pause |
| Optional recolours | Options; original artwork is the default |

The HUD uses T1/T2/T3 for upgrade-tree tiers, S for a special upgrade and ×N for acquisition count. Native auto-targeting remains available; no separate autofire toggle was added. Continue reconstructs the supported build and runtime state; it is not a frozen snapshot of every live enemy, projectile or RNG state.

## Changelog

### Android updater 0.1.0

**Added:** automatic DroidDeck Steam-library detection after one folder grant, remembered installation, public GitHub release notes/download verification, compatible code deployment, installed-file read-back and interrupted-update recovery. Native Android UI follows the selected utility layout with Revamped colors. This is a separate APK; game release numbering remains1.1.3.

### v1.1.3 — Controller roster fixes

- Character name, portrait, HP and description initialize immediately on controller entry, including remembered selections and re-entry.
- Charge Cannon is reachable at the end of the second weapon row. Gale and Bulwark also have complete native controller links.
- Locked content remains previewable without being playable or spending credits. Valid remembered added characters are restored after the original roster initializes.

Verified with actual controller and mouse input in the isolated Windows player at 1280×720 and 1920×1080. Saves, ownership and gameplay balance are unchanged. [Release notes](release/NOTES-1.1.3.md).

### v1.1.2 — Android integration closure

**Ported:** complete Abby spray/final-shot and base/charged shooter flow; global Spark/Dragon Bond observers and target selection; source reload activation/audio, hit/burn/pickup filters, 0.03-second physics cadence and gameplay transition ownership. The retained-code and acquisition comparison is closed, with explicit dispositions for source quirks.

**Fixed:** Glass is rebuilt from acquisitions at run boundaries, preventing repeated warm Continue from multiplying its bonus. Working Hina, Holy Shield and Gun Glyph contacts are retained under the approved collision-defect corrections.

[Release notes](release/NOTES-1.1.2.md) · [Detailed parity ledger](docs/PARITY.md).

### v1.1.1 — Character, enemy and description fixes

**Fixed:** Hina's first detached Shadow Clone keeps its skill-origin position; Rhogog's reused explosion rearms its existing damage child; destroyed Thousand Blades helpers unsubscribe. Thunderspawns, Soul Knight, Cadence, Tenacity and Reckless descriptions now match their mechanics.

### v1.1.0 — Remaining ports and the anomaly correction batch

**Ported:** complete Crossbow-family auto-release, laser arming/rearming, Yog/Reaper spawn cues, source camera following and Blessing cooldown ownership.

**Fixed:** Smite kill reporting; frozen-only Shatter; Elemental Barrage contributions; Might and positional-lightning damage signs; exact pool caps and renderer reuse; finite reward-offer exhaustion; Specialize lifetime; Bulwark/Gale run resets; arena-edge reuse; late Darkness damage; Pulsing Summons configuration; temporary-buff and legacy Continue reconstruction; Growth reward duplication; LargeXP renderer binding; burn reuse/invalid targets; Fire Edge teardown; Soul Powered contribution leaks; and Hidden Blade knockback contacts. The reviewed ownership-inversion branch has no Windows equivalent to repair.

### Initial Revamped additions retained in v1.1.2

**Added:** transparent Isaac-inspired icon HUD; thirty milestone unlocks and persistent notifications; same-column rune progression, 20% rank pricing and multiple enabled row choices; Save & return / Continue; native Custom menus, owned-content limits, credits-only results and mode-default loading; reversible Sprites Reborn recolours with palette extensions; pixel-art maintenance credit; removal of the Emberpath promotion.

**Ported:** Charge Cannon and all three evolutions; Gale and Bulwark with their exclusive upgrades; existing character, summon, projectile/status, stat, perk/rune and scenario/mode behavior from Android 8.6. Later releases above complete and correct this integration rather than redefining its scope.

[Full anomaly history](docs/ANOMALIES.md) · [Correction-by-correction evidence](docs/BUGFIXES.md) · [Release and verification history](docs/PROGRESS.md).

## Verification and limits

**Known controller UI gaps:** Challenge and Custom cannot currently be reached through the campaign selector's controller navigation. The HUD and sprite settings still use a separate panel rather than native Options rows. An additional read-only audit records other candidates and their evidence separately; these fixes are pending. [Controller UI audit](docs/CONTROLLER-UI.md).

Version **1.1.3** is published and verified on the supported Windows target, with complete controller/pointer roster checks at720p and1080p, installed-package acceptance, exact rollback and preservation of originals/profile/configuration. The unchanged1.1.2 gameplay integration also has recorded acquisition, fire/reload, contact, lifecycle, Continue, restart, legacy and Custom checks. The Android updater additionally verifies a real GitHub download and phone installation; all614 delivered mod files and142 originals verify, and current progress/settings/registry are preserved.

These are bounded source and gameplay checks, not a natural full-run balance study or a test of every character × weapon × upgrade combination. The next step is user playtesting before broader promotion. [Exact delivery receipt](docs/PROGRESS.md).

The gameplay images above are unaltered Windows test screenshots, including earlier UI runs whose behavior remains in the release. They show test builds rather than claiming natural unlocks or full-run balance. The updater image is explicitly labelled as a native Android capture. [Screenshot provenance](docs/images/SOURCES.md).

## Development and credits

This is an independent mod repository; the proprietary game has no public source repository to fork. Game binaries, extracted artwork, saves and raw disassembly are not published here. [Development and migration notes](docs/MIGRATION.md) · [Redirected-code audit](docs/DEAD-CODE.md).

For an already prepared development player, set `MODERNIZATION_SCRATCH` to a root containing `game/MinutesTillDawn_Data/Managed` and `game/BepInEx/core`. Build the production project or the separate `validation/Validation.csproj` with the documented SDK; keep the validation assembly out of a normal installation. [Build instructions](INSTALL.md#build-from-source).

**Flanne** created the original game. **Darkaxt** maintains Revamped. Original repository code is MIT licensed; game assets and dependencies retain their owners' rights and licenses. Third-party mod binaries are not bundled. [Third-party notices and artwork credits](docs/THIRD-PARTY.md).
