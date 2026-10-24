# Revamped Updater design contract

The product is a small Android utility for the already installed Windows mod inside DroidDeck. Its structure follows one task: connect game folder, inspect the available release, deploy, then return to the game. It is not a launcher or a store.

Use the actual game's established purple `#242032`, coral `#FC525F`, parchment `#FFE3C3`, slate `#2B374C` and muted sage `#A9B5AA`. Use opaque surfaces, not blur. The updater's hourglass vector is original project artwork; do not present it as Flanne's official logo. The Android folder row uses the actual installed DroidDeck icon through PackageManager. GitHub is a functional source label, not an invented product logo. No extracted game artwork is added to the APK.

Native Android sans typography: 28sp main heading,22sp release numbers,17sp main/body controls,15sp secondary copy, minimum14sp supporting labels. All controls have at least48dp hit areas. Main content is centered at a maximum640dp on unfolded phones/tablets; scroll the content while keeping the primary action visible. Honor system insets, font scaling, controller focus and Android Back. Reduced-motion behavior is the normal behavior; progress communicates actual worker states.

Chosen direction: **B with A colors**, the user's explicit selection. Use B's calm status banner and two grouped version cards, with the existing Revamped palette above. Exactly one emphasized action depends on state: Choose game folder → Check GitHub → Install1.1.3 → Open DroidDeck. Folder Change and Refresh are secondary controls. Show installed/available version, concise changes and explicit failure/recovery status. Preserve a visible folder identity. No credentials, fabricated download figures or decorative photos.

Acceptance: actual native phone screenshots and measured view hierarchy after folder selection, release availability, update completion and an interrupted-update state; portrait narrow and unfolded layouts. Browser concept checks supplement native acceptance.
