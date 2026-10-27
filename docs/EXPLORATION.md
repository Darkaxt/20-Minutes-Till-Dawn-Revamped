# Exploration chests

Mint-tinted chests appear beyond the visible playfield on reachable ground. They are separate from gold boss chests and have no ambush or penalty mechanic. A run can have **at most three uncollected exploration chests**; boss rewards keep their existing lifecycle.

The system draws a new wave every **60–90 seconds of combat**. Pauses and upgrade menus stop this clock. Common waves can contain one to three chests in different directions. Placement varies across **5–30 seconds of travel at the player's current walking speed**, mixing nearer and farther targets instead of a narrow distant ring. Viewport exclusion sets a minimum visible-edge distance; compact arenas such as Temple cap the journey at reachable walls. New placements also account for exploration chests already on the ground. Blocked placement sectors are skipped; the game does not put rewards through walls or outside map boundaries.

| Primary wave draw | Weight | Reward |
|---|---:|---|
| Experience |25|30% of the current level's full XP requirement, with earned XP bonuses and the native level-up flow |
| Credits |20|1,000 credits added to the run score and normal payout |
| Healing |15|One red heart; the same percentage XP reward instead when health is full |
| Magnet |12|Existing XP pickups fly to the player using their normal collection behavior |
| Freeze |10|Three-second native enemy freeze, retaining boss resistance and immunities |
| Frenzy |8|+25% bullet damage, summon damage and fire rate for15 combat seconds; another refreshes its duration |
| Upgrade |8|An eligible normal upgrade shown in the native Take/Leave menu |
| Character upgrade |2|An eligible character upgrade shown in the same native menu |

The first four outcomes form the common pool. Extra chests in a common wave draw only from that pool; uncommon and rare waves contain one chest. These are **wave weights**, not the final distribution of individual chests.

Permanent upgrade rewards respect current prerequisites and remaining repetitions. Their exact upgrade is chosen at spawn. If it has become unavailable before collection, the chest grants the percentage XP reward rather than rerolling or opening an empty menu. XP is calculated when collected from the current full level requirement, not the remaining XP gap. Native XP multipliers still apply. There is no content-unlock bypass. Custom retains its obtained-content and no-achievement rules.

## Shared pointers

Boss and exploration chests use the same marker layout: fixed-size native chest artwork, a directional arrow and the rounded distance in game world units. Exploration markers retain the mint tint. Markers disappear when their targets are visible, collected or removed, and separate when several targets share a direction. The layout accounts for the safe area and existing HUD graphics.

![Three exploration chests spawned in different directions](images/exploration-spread.png)

This is a controlled native Windows spawn in different directions, with distances45,71 and76 game units. A48-seed real-geometry sample includes journeys from5.1 to29.85 seconds at the fixture's walking speed. These are placement checks, not natural full-run balance evidence. It is distinct from the deliberate all-east overlap stress test.

## Continue and credits

The existing checkpoint stores chest positions, fixed reward types/upgrades, the separate exploration random sequence, spawn time remaining, frenzy time remaining and exploration credits. Continue restores those values without another reward roll or another frenzy stack. Old chest positions and already collected credit totals are retained; unclaimed chests use the current reward amounts when opened. Checkpoints predating exploration initialize an empty exploration state.

Exploration credits appear in the run's normal total and reach the wallet through the existing end-of-run payout. They are not written to the wallet separately on collection. This keeps Continue and payout accounting together.

Runtime and delivery evidence is recorded in [PROGRESS.md](PROGRESS.md). Tests use isolated Windows profiles, controlled clock/random operands and synthetic controller/pointer devices. They do not establish natural full-run balance or phone presentation.
