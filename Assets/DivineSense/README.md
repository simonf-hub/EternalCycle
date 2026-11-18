# Divine-Sense playable pivot

Open `Assets/Scenes/DivineSense_Arena.unity`, press Play, select each contestant's disruptive and movement power, then choose **Enter the Colosseum**. The four original levels and MicroSense scripts are preserved. The arena is appended to Build Settings; the original opening scene is unchanged.

Angel uses A/D to move, W to jump, S to cast. Devil uses Left/Right, Up to jump, Down to cast. Touch glides while holding jump during descent. Taste allows two jumps between landings, including when walking or falling off a platform before the first jump. Release and press jump again for the second jump. Its second jump reaches 80% of the first jump’s height (adjust `TasteSecondJumpHeightMultiplier`); landing restores the full first jump. With Taste equipped, tap the same movement direction twice within 0.25 seconds to dash on the ground or in the air. This works with the movement keys, D-pad, or stick. While dead, those same four keys move a cross for three seconds before automatic rebirth.

## Implemented rules

- Fixed camera; Heaven scrolls platforms down and Hell scrolls them up. Existing platforms and crumble deadlines survive reversals. New platforms fill three routes across the arena, with random widths, horizontal/vertical jitter, staggered route heights, and occasional offset connectors. Each lane continues between rows to support single-jump progression.
- Shared followers worth 100; death loses ceiling(20% of current score), once per death. Bodies outside the camera rectangle die. Respawn markers are clamped to the view and overlap resolution searches nearby free space at rebirth.
- 180-second round, larger score wins, equal scores draw. Match expiry runs before other gameplay updates. Input, score changes, scrolling, spawning, and rebirth stop at completion. Play Again reloads the scene.
- Sight hides body renderers for two seconds without disabling gameplay. Sound queues knockback through the motor. Both consume their own 20-second cooldown even on a missed target. Growth prevents opposing Sight/Sound hits.
- Heaven/Hell flips automatically every 16 seconds, independently of god interventions (`SceneFlipInterval`). Every 5 seconds, a god drops a random power-up, excluding the previous selection: Hermes drops position swap, Demeter growth with 1.5× jump height, Athena ability exchange, Dionysus reverses the opponent’s horizontal controls for 3 seconds when collected. Scene change is no longer a god power. Gifts fall down through platforms in either mode. A text warning precedes interventions.
- Ability exchange transfers actual complete runtime bundle references and ready-at timestamps for three seconds. Repeated effects refresh deadlines. Airborne extra-jump consumption persists through exchanges.
- Temporary growth changes collider and visual size, maintains foot height where possible, resolves solid overlap, and reduces movement speed. Player bodies do not block each other. Markers remain independent of growth.

## Assets and wiring

`Settings/ArenaSettings.asset` exposes duration, score, ability ranges/cooldowns, motion, generation, crumble, growth, marker, and god-drop tuning. Session clones it at runtime so play does not mutate the authoring asset. Provisional tuning: speed 6, jump 12, gravity scale 3, glide 2, scroll .8, platform width 2.8–4.2, row spacing 2 ± .15, three lanes, horizontal jitter 1.1, vertical jitter .12, connector chance .5, crumble delay 1.5, Sight radius 7, Sound radius 4, impulse 12, growth 1.5x at .65 speed. These are playtest starting points. Platform scatter settings are exposed together in the Inspector. Default maximum adjacent-row height difference is 2.39 units, below the base jump apex of approximately 2.45; consecutive same-lane horizontal spans overlap. Interactive tuning is still needed for jump feel.

`Prefabs/` contains Angel, Devil, Platform, CrumblingPlatform, and GodPickup. The scene's Managers object wires GameSession, PlayerRegistry, setup, world, generator, gods, and effect services. PlayerContext exposes the local motor/input/lifecycle/score/status/ability/visual/respawn components. Player prefabs need their scene Session and Registry references assigned when instantiated outside this scene. Generators initialize platform Session, follower Session, and god pickup callbacks explicitly.

Platforms use layer 9 (the project's existing Ground layer), kinematic bodies, solid colliders, and one-way effectors. Followers and god gifts are triggers. God gifts have their own kinematic downward mover under a stationary root; followers are platform children. Each player's marker is a separate child outside Body Visual.

The new namespace `DivineSense` keeps the revised abilities independent of the existing `MicroSense` classes. `Runtime/` contains the component files named for their responsibilities. Input System maps P1Gameplay/P1Respawn/P2Gameplay/P2Respawn are constructed and owned by the individual input readers; there is no new global input asset or package change. The HUD uses scaled IMGUI for this prototype. Art uses the existing circle/rounded-square sprites and procedural Angel/Devil features, columns, and audience silhouettes.

Rebuild generated assets with **Divine-Sense > Build New Arena**. Rebuild replaces the new scene and generated prefabs; save manual customizations elsewhere before using it. Normal play does not need rebuilding.

## Verification

Unity 6000.6.7f1 compiled and generated the scene/prefabs. `DivineSenseEditor.DivineSenseValidation.Run` is a repeatable batch PlayMode smoke check: score arithmetic, all god exclusion choices, idempotent death, marker/body position exchange, bundle/cooldown transfer and return, scene reversal retaining platform positions, automatic rebirth, accelerated full 180-second round, 35 nonconsecutive interventions, and frozen final scores. Actual final run results are recorded in `Validation.md`.

Human keyboard feel, visual presentation at different aspect ratios, all jump/glide/knockback interactions, simultaneous trigger races, and a second manually played round still need interactive playtesting. This is a functional prototype with placeholder art and text cues; it does not include campaign chapters, final character animation, music, or voice acting.


Controller support: the first connected gamepad controls Angel / Player 1, the second controls Devil / Player 2. Keyboard input remains available alongside gamepads. Left stick or D-pad moves; the bottom face button (A / Cross) jumps and is held to glide; the right face button (B / Circle) casts the active sense. Left stick or D-pad moves the respawn cross. In the original Level_1–4 scenes, the left face button (X / Square) activates ability slot 2 and Back / Select resets the level; bottom / Start dismisses tutorials.

Arena setup: D-pad left/right changes the selected player's sense, up/down changes movement; the top face button (Y / Triangle) switches which player is being edited. LB/RB adjusts follower death loss, right stick up/down adjusts scroll speed, and bottom / Start begins. Bottom / Start also replays after completion. One gamepad can be used with the other player on keyboard. Disconnecting a gamepad leaves that player's keyboard controls available; the other assigned gamepad keeps its player slot.

Wireless HID support: 8BitDo controllers exposed by Unity as Joystick devices now share the same two player slots as recognized Gamepads. Generic HID stick, hat, trigger/button 1 (jump) and button 2 (ability) are accepted; the arena setup and puzzle display the assigned device names. Additional generic HID buttons use 3/4 for left/top, 5/6 for shoulders, and 9/10 for Select/Start. Device-specific labels can differ between Bluetooth modes.

Touch glide now grants a short upward release dash: while descending, hold jump to glide and release to dash 1.5–4 units over 0.12 seconds, depending on glide charge. The burst goes straight upward and stops its upward momentum at the end to respect the distance cap. Gravity and collisions remain active. Landing, Medusa, respawn, teleport and controller cancellation reset the glide/dash state. Tune GlideDashDistance, GlideDashMaxDistance, GlideDashChargeTime and GlideDashDuration in ArenaSettings.

Glide release dash now charges during descending glide: distance increases from GlideDashDistance (1.5 units) to GlideDashMaxDistance (4 units) over GlideDashChargeTime (1.5 seconds), then remains capped. A cyan-to-gold bar under the character shows charge while gliding. Release spends the charge; landing, stun, teleport and device cancellation clear it.

The charged upward dash is limited to one use per airtime. After releasing it, normal slow-fall glide remains available, but charge and dash remain spent until landing on a solid support. Stun, controller reconnect, ability swaps and midair teleports do not refund the use. A fresh life starts with the dash available.
