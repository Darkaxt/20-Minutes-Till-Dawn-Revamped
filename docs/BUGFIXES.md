# Revamped bug-correction contract

User authorization: on2026-10-24, after review of the23 likely Android bug mechanisms, the user instructed: "ok, then fix the bugs." This explicitly overrides exact Android-bug parity for the families below. Other Android gameplay changes, controls/progression/HUD/Custom overrides and original/profile preservation remain required. No unrelated balance changes, speculative framework or monetization is authorized.

This correction batch is verified and delivered in **v1.1.0**. The existing broader P2 parity audit remains the single **ACTIVE** stage because retained-body coverage outside the bounded comparisons is still unproved. This document is the authoritative correction/acceptance contract for this batch, linked from SPEC.md. The anomaly ledger remains evidence of original Android behavior; later correction dispositions supersede its old preserve-the-bug instructions for these families only.

Implementation sequence inside P2: (1) reconcile existing source/evidence and complete all23 corrections as one connected batch; (2) validate complete combat/lifecycle/current-and-legacy Continue/restart/Custom/native workflows with isolated windowless players, review every family against its row; (3) finish integration/specification reconciliation, commit/synchronize, produce one normal versioned final release and verify the delivered package/install/rollback/download. No per-bug release. No tracked deferrals. Existing unrelated source-audit coverage limits remain explicit and are not silently represented as verified equivalence.

| Family | Root owner | Required corrected behavior |
|---|---|---|
| A24/A79/A135 | Smite kill producer | Publish kill only for an actual living-to-dead result of this Smite hit; surviving/protected contacts never advance its consumers. |
| A43 | Shatter death filter | Explode only on a frozen victim death; preserve the percentage, damage category, repeated owners and art. |
| A58 | Elemental Barrage stats | Apply one Fire Rate and one Reload Rate contribution, then remove both at original expiry. |
| A73 | Might signed damage | Add two damage with the correct sign at the existing damage boundary, without turning healing/zero changes into damage. |
| A76 | Positional lightning | Apply negative signed Thunder damage; preserve target/radius/affix/event scope and visual-only versus targeted hitbox routing. |
| A87 | Pool maximum | Reject when allocated-minus-returned count reaches maximum, for every overload; returning/reacquiring an actor still works. |
| A89 | Hidden pooled renderer | Restore the visibility-evicted renderer when its exact actor is acquired again; preserve authored disabled renderer states. |
| A95 | Offer exhaustion | Select without replacement from the eligible finite candidates, preserving fallback preference; return fewer choices when insufficient, never retry indefinitely. Native menus hide unavailable entries, select an available entry, and return to combat/death when no choice exists; no stale/null reward is equipped. |
| A97 | Specialize lifetime | Assassin exclusion counts pending unused helpers only; consumption, destruction and Continue each maintain that same count without global reset of other owners. |
| A100 | Bulwark asset state | Fresh run/replay begins with clean Perpetual accumulator and Steel speed guard; repeated acquisitions within one run keep their documented accumulation. |
| A122 | Arena edge reuse | Re-enable a cached edge when its live segment is rebuilt after Wind Shield releases it. |
| A125 | Darkness damage owner | Execute the existing D10 late-run +1 damage rule through the existing source scheduler/clock owner using original activation threshold and values. |
| A138 | Pulsing Summons configuration | Store acquired configuration before notifying existing listeners so later eligible summons get the same configuration. |
| A143/A256 | Buff contribution and Continue | Track live contributions rather than lifetime applications. Save permanent earned stacks, omit temporary/conditional active state and reconstruct conditions; expiry/repeated Continue cannot create permanent bonuses. Preserve owned old saves on read and distinguish legacy fields. |
| A158 | Growth restore award | Restore level display/XP without granting another heart for that level; actual newly gained qualifying levels still reward once. |
| A168 | LargeXP renderer | Bind the existing XP sprite renderer for LargeXP before initialization; no art replacement or new XP producer. |
| A181 | Burn reused tick phase | Reset elapsed and interval on release/new-target reuse; refreshing the same active burn keeps its existing gameplay refresh cadence. |
| A182 | Invalid burn registration | Validate target/Health and positive duration count before registering a new buff/FX or posting successful burn; release invalid/zero-count states. |
| A199 | Fire Edge teardown | Remove both impact observers during destruction; never add one during teardown. |
| A202 | Soul Powered repeats | Remove the tracked previous contribution before resetting/rebinding and recomputing it; repeats cannot leak an untracked permanent bonus. |
| A205 | Whirlwind run range | Reset to the same original baseline1.960000038 on destruction/new run, preserving explicitly acquired range upgrades within a run. |
| A221 | Non-saving ownership inversion | Verify absent Android-only branch and correct native/M10 ownership behavior; fix only a present equivalent defect, without importing a dormant inverted owner. |
| A265 | Hidden Blade knockback layer | Put only the existing child knockback hitbox on a native layer that contacts NPCs; preserve its original force/radius and other game-wide collision rules. |

Acceptance requires real native acquisition/callback/physics where the defect has a reached gameplay path, focused deterministic lifecycle/edge assertions for conditional failures, repeated current Continue and ordinary restart for state fixes, retained selected legacy-save read preservation, Custom no-unlock/no-achievement ownership, and appropriate final packaged native smoke. Dormant/absent Windows branches receive an explicit evidence-backed disposition, not invented activation. A source edit or compile alone does not close a row. Record actual failures and verification boundaries in the maintained anomaly/progress ledgers.

## Verified gameplay disposition

All22 corrected families pass the affected Windows runtime contracts. A221 has the absent-equivalent disposition above. Source and original binaries are pinned in the private acceptance readback; each run records its exact production/validation DLLs, events, input checkpoint and exit.

| Families | Windows acceptance |
|---|---|
| A24/A79/A135, A43, A73, A76 | S4/410/412/415: real surviving/protected/lethal Smite and threshold consumers; frozen-only Shatter; signed/positive Might damage with healing/zero preserved; damaging positional Thunder. Continued Burn/Thunder include the corrected Might bonus. |
| A87/A89/A168/A181/A182 |410/412/415: all three pool overloads reject the exact cap, return/reacquire works, evicted renderer restores while an independently disabled renderer stays disabled; original LargeXP renderer initializes; burn reuse resets tick phase and invalid/zero-duration targets cannot register or announce success. Actual frozen-death Shatter caps at50. |
| A95 |410/412/415: duplicate/exhausted candidate sets terminate; native short/empty level-up, chest and devil menus select valid rewards or return to combat without null/stale rewards. |
| A97 |440–442: native reroll, ordinary/Split Fire Specialize, mixed used/pending Continue, Halo flow and restart; only pending helpers exclude Assassin. |
| A100/A205 |418/419: repeated Bulwark acquisitions, source Continue and a new Bulwark run preserve within-run accumulation and reset run-local guards. Upgraded Gale restarts at baseline1.960000038. |
| A122 |438/439: actual Wind Shield contact releases an edge, geometry reactivates it, and real boss/arena pool reuse retains active colliders; current Continue/native movement/laser/restart pass. |
| A125 |415/417/448: baseline1, strict late threshold, one+1 activation, reconstructed live owner on Continue, no duplicate/rewind removal; a real source-spawned NPC receives damage2 and physical contact subtracts two hearts. |
| A138 |410: original action stores configuration with no listener; a later native summon gets50 damage/2-second interval/range3.412/415 repeat reconstruction. |
| A143/A256 |410/412/415 Generator;420–431 Diamond/Scarlett/Lilith/Abby;445–447 active low-HP Strong Will;450/451 old saves/current roundtrips. Live counts balance at expiry/removal; permanent Berserk/Bulk Up survive; temporary/conditional phases are omitted and conditions rebuild from actual HP. |
| A58/A158 |432–434: one Barrage Fire Rate/Reload Rate contribution and balanced expiry.435–437: genuine level5 reward, two no-award same-level Continues, then genuine level10 reward and native restart. |
| A199/A202 |410/412/415: Fire Edge destruction removes both event roots; two actual Soul Powered acquisitions retain one tracked contribution, and subsequent soul changes remove it without a leak. |
| A265 |443/444: native Hina skill/exclusives and the existing knockback child make actual NPC contact with force4; original dash damage/clone projectile/current Continue/restart remain. |
| Shared preservation |449 reads41 historical checkpoints byte-identically.450/451 rebuild selected old buff/soul and labelled derived D15/Custom cases through actual Continue/save/input/restart.452/453 pass native Custom controls/input/spawn/XP/save/Continue, credits-only victory and unchanged ownership. |

The normal1.1.0 package passes installed-player S6/run-15, all15 character entries/12 base weapons, mouse/controller fire/reload and ordinary scene exits. Its exact614-file copied rollback restores142 originals. The actual Steam update verifies the same614 files while preserving142 originals and3 real profile files. The published normal release and independently downloaded archive/DLL match source49f861f. [Delivery receipt and digests](PROGRESS.md). No correction-family acceptance or delivery item remains pending. Gameplay evidence does not claim natural full-run balance, every build combination or Android device observation.
