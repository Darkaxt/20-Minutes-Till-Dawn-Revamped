# Attribution and local dependencies

20 Minutes Till Dawn and its original artwork are by Flanne. Android gameplay/assets are read from the player's supplied package; neither platform's game binaries nor extracted artwork/data are redistributed here.

Sprites Reborn is by Law, updated by Daramos, using textures by Darkly77 and original art by Flanne. The installer reads the authors' published texture source at revision `77d1a9a7316c1333ce7cd61e276da5ef5a277398`, checks the archive digest and prepares compatible sheets locally. The authors' mod DLL/AssetReplacer is not installed or redistributed. Additional deterministic palette substitutions are original work in this repository. [Author source and credits](https://github.com/YuriyDaramos/20MTD_Sprites-Reborn/tree/77d1a9a7316c1333ce7cd61e276da5ef5a277398).

BepInEx v5.4.23.5 (MIT, Bepis) and Il2CppDumper v6.7.46 (MIT, Perfare) are downloaded from pinned official releases for local installation/preparation. [BepInEx license](https://github.com/BepInEx/BepInEx/blob/v5.4.23.5/LICENSE), [Il2CppDumper license](https://github.com/Perfare/Il2CppDumper/blob/v6.7.46/LICENSE). Their archives/binaries are not part of this release ZIP. BepInEx's supplied Harmony/Cecil dependencies retain their upstream notices/source.

The isolated preparation runtime installs the exact versions in `installer/requirements.txt` from PyPI, including UnityPy, TypeTreeGeneratorAPI and Pillow with their dependencies. Their distributed license metadata remains in that local runtime; these libraries are not bundled in the mod ZIP. Tools are used only for local extraction, not as replacements for the game's engine.

Nexus mods 8/17/20/101/105/109 and the user's Isaac HUD example informed requirements. Restricted third-party menu/code/assets were not copied. Native auto-targeting remains; no new autofire mod is included. The title maintenance pixel glyphs, owned-logo composition and all native UI integration are original repository code; no title image from mod109 is distributed.

## Source-mod references and inspiration

These links credit the original work; **none requires a separate installation alongside Revamped**. The Nexus Requirements entries explicitly say "Reference only - NOT REQUIRED". Sprites Reborn's pinned texture source is fetched by Revamped's installer when preparing the optional palette treatment.

| Mod | Creator | Contribution or reference |
|---|---|---|
| [Colorful Overhaul](https://www.nexusmods.com/20minutestildawn/mods/8) | Darkly77 | Original colour-overhaul work, credited texture lineage and visual inspiration. |
| [Better UI](https://www.nexusmods.com/20minutestildawn/mods/17) | sloverlord | UI/build-information inspiration; Revamped supplies its own native HUD and menus. |
| [Sprites Reborn (Updated for 1.0)](https://www.nexusmods.com/20minutestildawn/mods/20) | Law; updated source by Daramos | Optional texture source, with Darkly77 and Flanne artwork, prepared locally from the pinned credited source. |
| [Autofire](https://www.nexusmods.com/20minutestildawn/mods/101) | laredson | Reviewed during controls/QoL planning; its DLL and autofire implementation are not included. |
| [Mod Menu - 20MinutesTillChaos](https://www.nexusmods.com/20minutestildawn/mods/105) | akentrus | Custom-run settings inspiration; Revamped implements its own native Custom menus. |
| [Honest Title and Fixed Pixel Scaling](https://www.nexusmods.com/20minutestildawn/mods/109) | owlantler | Presentation/title/zoom inspiration; Revamped creates its own maintenance lettering and settings. |
