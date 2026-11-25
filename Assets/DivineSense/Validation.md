# Campaign validation — 21 November 2026

Unity 6000.6.7f1 compiled the campaign and generated its editable scenes and prefabs.

- `DivineSenseEditor.FinalCampaignValidation.Run` passed in batch Play Mode. It covered the exact build order, story transitions, simulated A/B and left stick input, D-pad exclusion, the four tutorial gifts, the arena’s Sight and Sound fixtures, the timed round and results, checkpoint recovery, co-op gates and exits, and replay.
- `DivineSenseEditor.DivineSenseValidation.Run` passed in batch Play Mode. It covered all four god gifts, shared ability effects and timings, Athena’s marker swap, automatic respawn, Heaven/Hell flips, and a full accelerated 180-second arena match.

The automated run moved players between puzzle stations to check their behavior. It did not measure traversal feel, timing, or difficulty with two human players.

## Archived prototype validation — 14 November 2026

Unity 6000.6.7f1; existing Input System 1.20.1 and URP 17.6.0 preserved.

## Actual checks

- PASS: Unity compilation and scene/prefab generation (`DIVINE_SENSE_BUILD_OK`).
- PASS: Unity PlayMode smoke run (`DIVINE_VALIDATION_OK`), including a full accelerated 180-second match.
- PASS: death arithmetic 1,000 → 800 and 1 → 0; repeated death calls apply only one penalty.
- PASS: all previous-god/selection combinations exclude the immediately previous god.
- PASS: Sight hides the opponent and starts a 20-second cooldown; Growth blocks a new Sight hit.
- PASS: live-body/dead-marker position exchange, followed by automatic respawn.
- PASS: equipped loadout exchanges by reference; different cooldowns transfer; updated cooldown returns to the original owner.
- PASS: Heaven/Hell reversal changes scroll direction while retaining current platform positions.
- PASS: exactly eight interventions before 180 seconds, with no adjacent repeat.
- PASS: exact 180-second finish; subsequent score/death requests leave final score unchanged.
- PASS: direct 1280×800 camera render inspected for platform/character visibility and arena layout. This render excludes IMGUI; it is not a visual check of the HUD.
- PASS: `git diff --check` for tracked changes.

The initial test run was interrupted by an editor assembly reload; it is not counted as a passing full match. The runner now resumes after assembly reload and waits for initialization. Final passing PlayMode log: `/tmp/divine-validation-final.log`; generation log: `/tmp/divine-build-approved.log`; preview log: `/tmp/divine-preview.log`.

## Remaining interactive checks

Human keyboard input, jump/glide feel, knockback on ledges, crumble timing under simultaneous players, simultaneous pickup contacts, HUD layout on different aspect ratios, and replay through the results menu need manual playtesting. The automated check invokes gameplay APIs; it does not simulate keyboard hardware. Placeholder art and text-only god cues remain intentional prototype limitations.

## Changed-file inventory

Prototype arena: `Assets/Scenes/Archive/DivineSense_Arena.unity`.
New tuning asset: `Assets/DivineSense/Settings/ArenaSettings.asset`.
New friction material: `Assets/DivineSense/Settings/PlayerPhysics.physicsMaterial2D`.
New prefabs: `Assets/DivineSense/Prefabs/{Angel,Devil,Platform,CrumblingPlatform,GodPickup}.prefab`.
Build Settings appends the arena and retains all four existing levels.
Unity `.meta` companions are generated for the new assets.

New C# files:

- `Assets/DivineSense/Editor/DivineSenseBuilder.cs`
- `Assets/DivineSense/Editor/DivineSensePreview.cs`
- `Assets/DivineSense/Editor/DivineSenseValidation.cs`
- `Assets/DivineSense/Runtime/AbilitySwapEffect.cs`
- `Assets/DivineSense/Runtime/CrumblingPlatform.cs`
- `Assets/DivineSense/Runtime/DoubleJumpAbility.cs`
- `Assets/DivineSense/Runtime/FallingPickupMover.cs`
- `Assets/DivineSense/Runtime/FollowerCollectible.cs`
- `Assets/DivineSense/Runtime/FollowerCounter.cs`
- `Assets/DivineSense/Runtime/GameSession.cs`
- `Assets/DivineSense/Runtime/GameSettings.cs`
- `Assets/DivineSense/Runtime/GameTypes.cs`
- `Assets/DivineSense/Runtime/GlideAbility.cs`
- `Assets/DivineSense/Runtime/GodDirector.cs`
- `Assets/DivineSense/Runtime/GodPickup.cs`
- `Assets/DivineSense/Runtime/GrowthEffect.cs`
- `Assets/DivineSense/Runtime/HUDController.cs`
- `Assets/DivineSense/Runtime/MatchSetupController.cs`
- `Assets/DivineSense/Runtime/PlatformScroller.cs`
- `Assets/DivineSense/Runtime/PlatformSpawner.cs`
- `Assets/DivineSense/Runtime/PlayerAbilityController.cs`
- `Assets/DivineSense/Runtime/PlayerContext.cs`
- `Assets/DivineSense/Runtime/PlayerInputReader.cs`
- `Assets/DivineSense/Runtime/PlayerLifecycle.cs`
- `Assets/DivineSense/Runtime/PlayerMotor2D.cs`
- `Assets/DivineSense/Runtime/PlayerRegistry.cs`
- `Assets/DivineSense/Runtime/PlayerStatusEffects.cs`
- `Assets/DivineSense/Runtime/PlayerVisuals.cs`
- `Assets/DivineSense/Runtime/PositionSwapEffect.cs`
- `Assets/DivineSense/Runtime/RespawnController.cs`
- `Assets/DivineSense/Runtime/ReverseControlsEffect.cs`
- `Assets/DivineSense/Runtime/RuntimeLoadout.cs`
- `Assets/DivineSense/Runtime/SceneModeController.cs`
- `Assets/DivineSense/Runtime/SightAbility.cs`
- `Assets/DivineSense/Runtime/SoundAbility.cs`

## Respawn regression fix

The original smoke check killed an onscreen character and checked only life state. It missed a physics/Transform desynchronization: teleporting a non-simulated Rigidbody2D changed its physics position, but enabling simulation restored the old Transform position. A character dying offscreen could therefore immediately die again at rebirth.

`PlayerMotor2D.Teleport` now updates both Transform and Rigidbody2D poses and resets interpolation history. `RespawnController` hides the marker immediately after completion.

PASS: `RespawnRegression.Run` in an isolated project copy, three consecutive deaths beyond the bottom camera boundary. Each rebirth occurred approximately 3.000 seconds after death, with both physics and visual positions at the selected marker; the player remained alive after physics resumed. Log: `/tmp/respawn-regression-fixed.log`. The copied Editor search index emitted unrelated initialization exceptions; the respawn assertions passed and Unity exited with code 0.

Repeat with Unity batch mode and `-executeMethod RespawnRegression.Run` against a project that is not already open. This runner exits the Editor when complete. It is not intended for the interactive Editor session.

## Platform scatter update

Replaced the narrow sine-wave platform path with three full-width routes, random widths (2.8–4.2), row spacing variation, horizontal/vertical jitter, route staggering, and optional connectors. The existing starting dais is retained. The preview now calls the actual platform generator rather than approximating it with a separate layout formula.

PASS: compilation and direct camera render in the isolated Unity copy (`/tmp/divine-scatter-final.log`, `DIVINE_PREVIEW_OK`). Final render visually inspected for distribution, readable platforms, and route separation. `git diff --check` passed. Single-jump reachability is supported by bounded within-route height differences and overlapping horizontal spans; it has not been manually playtested in this layout.

## Taste diminishing jump height

The first jump uses full JumpSpeed. Taste's airborne jump uses JumpSpeed × sqrt(TasteSecondJumpHeightMultiplier), default .8 height (approximately .894 velocity). Landing restores the full first jump and the existing extra-jump budget. The setting is exposed in ArenaSettings. Unity compilation passed in the isolated project copy (`/tmp/divine-taste-compile.log`, exit 0); interactive jump feel has not been checked.

## Independent scene-flip timer

Removed Zeus/scene-change from the god selection pool while retaining serialized enum IDs. Gods now always spawn one of the four existing power-up types at the existing 20-second interval. SceneModeController independently flips Heaven/Hell every configurable SceneFlipInterval (default 10 seconds), using the shared round clock. The HUD shows time until the next flip.

PASS: Unity compilation and full accelerated 180-second PlayMode round in the isolated copy (`/tmp/divine-auto-flip-validation.log`, `DIVINE_VALIDATION_OK`). Checked 17 automatic flips at 10-second boundaries, eight nonconsecutive god selections with spawned power-ups and no Zeus selection, and no flip or intervention at the 180-second round boundary. Existing score, respawn, bundle-transfer, and frozen-result checks also passed. Copied Editor search-index initialization exceptions are unrelated to gameplay; Unity completed the checks successfully. HUD text has not been visually checked for this update.


## EternalCycle prefab organization

Moved artwork and existing prefabs under `Assets/Prefabs/EternalCycle`, preserving existing GUIDs. Connected the arena players and Colosseum to prefab assets, nested a shared Follower prefab in both platform prefabs, and configured individual god pickup variants in ArenaSettings. Updated builder asset paths.

Verified Unity compilation and prefab migration in an isolated project copy (`/tmp/eternalcycle-prefab-finalize.log`). The complete accelerated gameplay regression passed after reconnecting the starting platform follower session (`/tmp/eternalcycle-prefab-playtest-final.log`, `DIVINE_VALIDATION_OK`): score/death, respawn, cooldown ownership, ability swaps, eight god drops, eleven 16-second flips, and frozen round results. No gameplay exceptions in the final run; copied editor search-index initialization exceptions remain unrelated.
