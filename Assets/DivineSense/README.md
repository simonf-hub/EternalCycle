# Eternal Cycle · Divine Sense

Open [Level_0](../Scenes/Level_0.unity) in Unity and press **Play** to start the campaign. The [controller diagram](Resources/Controller-Guide.png) appears before the opening narration. Press **A**, **Space**, or **Enter** to continue. The build includes these three scenes, in order.

| Scene | Trial |
| --- | --- |
| [Level_0](../Scenes/Level_0.unity) | Co-op tutorial with a movement course, a Sight bridge, a Sound wall, and a practice station for each of the four god gifts. Both mortals must reach the exit. |
| [Level_1](../Scenes/Level_1.unity) | Competitive Heaven/Hell Colosseum. Collect followers, stay on the scrolling platforms, and use god gifts to gain an advantage. |
| [Level_2](../Scenes/Level_2.unity) | Athena’s co-op trial. Reveal stepping stones, break walls, and split up to stand on paired pressure plates. Both mortals must reach the final exit. |

Use the **left stick** to move and place a respawn cross, **A** to jump, and **B** to cast. A on either controller also advances story and results screens. On keyboard, Angel uses **A/D**, **W**, and **S**. Devil uses **←/→**, **↑**, and **↓**. **Space** or **Enter** advances the story.

Angel carries **Sight** and **Touch** throughout the campaign. Sight reveals hidden platforms and hides the other mortal for 1.5 seconds. Hold jump while falling to glide and release to dash upward once per airtime. Devil carries **Sound** and **Taste**. Sound breaks cracked walls and knocks the other mortal away. Press jump again in midair for a second jump or tap the same direction twice quickly to dash.

## Edit the levels and import art

- **Level_0 and Level_2.** Move and resize the prefab instances under `Layout` in the Hierarchy. The `CooperativeLevelController` on `Trial` has Inspector references for spawns, camera limits, checkpoints, exit volume, required objects, and the hint text and zones. When adding a gate, assign its two pressure plates and add the gate to the level’s required list if it should block completion.
- **Level_1.** The `PlatformSpawner` on `Managers` builds a steady route through the middle in the starting platform’s muted light blue. Sound shatters cracked cyan platforms beside the steady route. Narrow orange platforms crumble 0.5 seconds after landing. Sight reveals hidden shortcuts and their followers. Adjust **Row Spacing Override** on the spawner, **Crumble Delay Override** on `GameSession`, or the shared lane and scrolling values in [Game Settings](Settings/ArenaSettings.asset). For a hand-authored layout, turn off **Generate Platforms** and place arena platform prefab instances under `PlatformsRoot`.
- **Shared visuals.** Character, arena, and god gift prefabs are under [Arena](../Prefabs/EternalCycle/Arena). Hidden platforms and breakable walls are under [Environment](../Prefabs/EternalCycle/Environment). Exit, checkpoint, pressure plate, and paired gate prefabs are under [Cooperative](../Prefabs/EternalCycle/Cooperative). Edit the Sprite Renderers or visual children in Prefab Mode to update their instances across the levels. Keep colliders, trigger references, and player visual references connected.
- **Shared gameplay.** [Game Settings](Settings/ArenaSettings.asset) holds the same Sight and Sound ranges and cooldowns for all three levels. Zeus’s Agility lasts 3 seconds and scales size to 1.5×, jump height to 1.5×, and movement speed to 65%, with immunity to Sight and Sound. In the arena, the director drops Athena’s Position, Zeus’s Agility, Hera’s Pull, and Ares’s Stare. In the tutorial, the same gifts are fixed at their practice stations.
- **Narration and dialogue.** Edit the opening, Zeus transition, and Athena transition in the [Trial Story asset](Resources/TrialStory.asset). A dialogue line with an empty speaker is displayed as a stage direction.

Prior scenes remain under [Scenes/Archive](../Scenes/Archive). **Divine-Sense > Build Starter Co-op Layouts** regenerates the starter Level_0 and Level_2 scene files. Use the Hierarchy and Inspector to make normal layout edits.
