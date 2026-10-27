# 1.1.9 — Results focus and useful exploration rewards

**Fixed:** controller focus belongs to the visible menu through native presentation and input events. Victory/death results keep directional navigation on Try Again and Quit To Menu. The redundant end-of-run skill list is removed; scores, loadout, progression and clear counts remain. Shared roster controls remain part of the loadout menu, including Runes.

**Improved:** exploration placements vary across5–30 seconds of walking instead of a narrow20–30-second distant band, subject to viewport exclusion and reachable walls. Credit chests now give1,000 credits. XP chests give30% of the current full level requirement at collection, with earned native XP bonuses and the normal upgrade-choice flow. Full-health and exhausted-upgrade replacements use the same XP rule. Old positions and already earned credits are preserved; unclaimed chests use the new amounts when opened.

**Clarified:** a target with both positive and negative stat effects appears once under Mixed (+ / −), instead of appearing in both Benefits and Penalties. Double Shot's extra projectile and reduced bullet damage are a real trade-off; its recipe is unchanged. Pure benefit, penalty, summon-support and synergy indications remain.

**Ported:** no new Android behavior. These are requested Revamped playtest corrections; the completed Android8.6 gameplay integration remains.

Native Windows verification covers Forest boss/victory, controller Retry/death/Quit and every directional navigation input, pointer/controller switching, complete retained menus, all scenarios and Custom, percentage XP at levels1/20/45/75, earned XP bonuses, actual level-up,1,000-credit pickup/payout, current and original1.1.8/1.1.7 checkpoints, and actual Mixed/path layout at square/4:3/16:9. Fixtures use controlled clocks/random draws; they do not establish natural full-run balance or phone runtime behavior. Final package/public-download evidence is recorded in [PROGRESS.md](../docs/PROGRESS.md).

The existing Android updater remains compatible: only the mod DLL changes among614 managed targets. Release publication does not automatically deploy to Steam or DroidDeck.
