# Exploration chests

Mint-tinted chests appear beyond the visible playfield on reachable ground. They are separate from gold boss chests and have no ambush or penalty mechanic. A run can have **at most three uncollected exploration chests**; boss rewards keep their existing lifecycle.

**Uncollected exploration chests do not expire.** They remain until collected or the run ends, including across later spawn waves. Save & return / Continue preserves their fixed positions and rewards.

The system draws a new wave every **60–90 seconds of combat**. Pauses and upgrade menus stop this clock. Common waves can contain one to three chests in different directions. Placement varies across **5–30 seconds of travel at the player's current walking speed**, mixing nearer and farther targets instead of a narrow distant ring. Viewport exclusion sets a minimum visible-edge distance; compact arenas such as Temple cap the journey at reachable walls. New placements also account for exploration chests already on the ground. Blocked placement sectors are skipped; the game does not put rewards through walls or outside map boundaries.

| Primary wave draw | Weight | Reward |
|---|---:|---|
| Experience |45|30% of the current level's full XP requirement, with earned XP bonuses and the native level-up flow |
| Healing |15|One red heart; the same percentage XP reward instead when health is full |
| Magnet |12|Existing XP pickups fly to the player using their normal collection behavior |
| Freeze |10|Three-second native enemy freeze, retaining boss resistance and immunities |
| Frenzy |8|+25% bullet damage, summon damage and fire rate for15 combat seconds; another refreshes its duration |
| Upgrade |8|An eligible normal upgrade shown in the native Take/Leave menu |
| Character upgrade |2|An eligible character upgrade shown in the same native menu |

The first three outcomes form the common pool. Extra chests in a common wave draw only from that pool; uncommon and rare waves contain one chest. These are **wave weights**, not the final distribution of individual chests. Exploration chests no longer award credits.

Permanent upgrade rewards respect current prerequisites and remaining repetitions. Their exact upgrade is chosen at spawn. If it has become unavailable before collection, the chest grants the percentage XP reward rather than rerolling or opening an empty menu. XP is calculated when collected from the current full level requirement, not the remaining XP gap. Native XP multipliers still apply. There is no content-unlock bypass. Custom retains its obtained-content and no-achievement rules.

## Shared pointers

Boss and exploration chests use the same marker layout: a content icon, directional arrow and rounded distance in game world units. Exploration shows the XP gem, healing, magnet, freeze, frenzy or exact selected upgrade artwork; its arrow uses the reward colour. Boss treasure retains its gold chest icon. Markers disappear when their targets are visible, collected or removed, and separate when several targets share a direction. The layout accounts for the safe area and existing HUD graphics.

World exploration chests retain the normal chest's red/blue fog-reveal sprites, so their artwork is visible through the shadows when the edge pointer hides. These mask colors are internal lighting channels and are independent of the content colors below.

### Nearby content colors

A small, steady glow fades in within **eight world units**, reaching full strength within **two**. Distant world chests remain mint; their edge icons already show the stored contents. The glow uses the game's existing circle artwork and visible-in-fog rendering, with no pulse, panel or new setting. It works with Original and Sprites Reborn.

| Primary content | Nearby glow |
|---|---|
| Experience | Mint / teal |
| Healing | Rose |
| Magnet | Violet |
| Freeze | Ice blue |
| Frenzy | Orange |
| Normal upgrade | Lime |
| Character upgrade | Pink |

Color indicates the fixed primary reward category. Existing full-health or exhausted-upgrade XP replacements still apply; this does not change the reward roll or amount. Boss chests retain their original artwork and lighting.

![Three exploration chests spawned in different directions](images/exploration-spread.png)

This is a controlled native Windows spawn in different directions, with distances45,71 and76 game units. A48-seed real-geometry sample includes journeys from5.1 to29.85 seconds at the fixture's walking speed. These are placement checks, not natural full-run balance evidence. It is distinct from the deliberate all-east overlap stress test.

## Continue and credits

The existing checkpoint stores chest positions, fixed reward types/upgrades, the separate exploration random sequence, spawn time remaining, frenzy time remaining and previously earned exploration credits. Continue restores those values without another reward roll or another frenzy stack. Already-saved unopened credit chests become XP chests before their world objects and pointers appear; their positions and random sequence stay unchanged. Checkpoints predating exploration initialize an empty exploration state.

Previously collected exploration credits remain in the run's normal total and reach the wallet through the existing end-of-run payout. No new exploration credits are added. Native boss rewards and ordinary end-of-run credit calculations are unchanged.

Runtime and delivery evidence is recorded in [PROGRESS.md](PROGRESS.md). Tests use isolated Windows profiles, controlled clock/random operands and synthetic controller/pointer devices. They do not establish natural full-run balance or phone presentation.
