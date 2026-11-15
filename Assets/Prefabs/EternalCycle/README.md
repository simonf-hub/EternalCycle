# EternalCycle artwork and prefabs

- `Arena/Angel.prefab` and `Arena/Devil.prefab`: arena characters, body artwork, and respawn markers.
- `Arena/Colosseum.prefab`: arena background artwork.
- `Arena/Platform.prefab` and `Arena/CrumblingPlatform.prefab`: generated platforms.
- `Arena/Follower.prefab`: shared follower collectible nested in both platform prefabs.
- `Arena/HermesPickup.prefab`, `DemeterPickup.prefab`, `AthenaPickup.prefab`, and `DionysusPickup.prefab`: individual god pickups. These variants inherit shared behavior from `GodPickup.prefab`.
- `Players/`, `Environment/`, and `Pickups/`: original level prefabs, including Sight, Sound, Taste, and Touch pickups.
- `Art/`: shared sprites, ability icons, background image, and line material.

Open a prefab in Prefab Mode to edit its appearance and save it. The arena characters and background are connected scene instances. Platforms and god pickups are spawned from the referenced prefabs when playing. Stop and restart Play Mode to see edits on newly spawned objects. Changes to a shared sprite affect all objects using that sprite.

Edit visual children or SpriteRenderer properties while retaining gameplay components and references. Instance overrides take precedence over prefab values; revert a visual override on a scene instance if you want it to inherit that property again. The Build New Arena menu recreates prototype artwork; it is not needed when editing prefabs.
