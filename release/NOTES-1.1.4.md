# Controller menus and native Options

- Challenge and Custom are reachable through native controller navigation, with working difficulty, Play/Back and existing unlock guards.
- Stats HUD and visual style are native Options rows in both title and pause. An opaque background covers the previous menu; explicit links keep controller focus inside Options.
- Custom controller navigation no longer enters numeric text fields accidentally. Minus/Plus remain available; pointer typing is retained, and Cancel leaves an edit before closing the panel.
- Failed Continue supports B/Cancel and A on Back, preserving the checkpoint.

Native Windows checks pass at720p and1080p, including re-entry, controller/pointer activation, all Custom tabs/pages, all24 rune slots, settings across a fresh process, the15-character/12-weapon roster regression and both restoration-error exits. Legacy TMP typing uses its native key-event processor after actual pointer focus in the windowless fixture. Validation-only fault injection and profiles are excluded from the release.

Existing progress, settings and prepared artwork are retained. The614-file updater contract changes only the mod DLL; the existing Android updater supports this release. No gameplay balance changes are included.
