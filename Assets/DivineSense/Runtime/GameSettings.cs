using UnityEngine;
namespace DivineSense {
[CreateAssetMenu(menuName="Divine-Sense/Game Settings")]
public sealed class GameSettings : ScriptableObject {
 public float RoundDuration=180, GodInterval=5, RespawnDuration=3;
 [Min(.1f)] public float SceneFlipInterval=16;
 public int FollowerPickupValue=100, StartingFollowers=0;
 [Range(0,1)] public float DeathLossFraction=.2f;
 public float SightDuration=2, SightCooldown=20, SoundCooldown=20;
 public float GrowthDuration=3, AbilitySwapDuration=3, ReverseControlsDuration=3;
 public float SightRadius=7, SoundRadius=4, SoundImpulse=12;
 public float MoveSpeed=6, JumpSpeed=12, Gravity=3, GlideSpeed=2, KnockbackDecay=6;
 [Tooltip("Taste's airborne jump height relative to the first jump. Landing restores full jump height.")]
 [Range(.1f,1f)] public float TasteSecondJumpHeightMultiplier=.8f;
 public float ScrollSpeed=.8f, PlatformSpacing=2f, PlatformWidth=4.2f, CrumbleDelay=1.5f;
 [Header("Platform scatter")]
 [Min(1)] public int PlatformLaneCount=3;
 public float PlatformMinimumWidth=2.8f, PlatformSpacingVariation=.15f;
 public float PlatformHorizontalJitter=1.1f, PlatformVerticalJitter=.12f, PlatformEdgeInset=.7f;
 [Range(0,1)] public float PlatformConnectorChance=.5f;
 public float PlatformConnectorHeight=.7f, PlatformLaneStagger=.75f;
 [Range(0,1)] public float CrumbleChance=.25f, FollowerDensity=.8f;
 [Tooltip("Jump height multiplier while Demeter growth is active, including Taste jumps.")]
 [Min(1f)] public float GrowthJumpHeightMultiplier=1.5f;
 public float GrowthScale=1.5f, GrowthSlowdown=.65f, MarkerSpeed=6, PickupFallSpeed=2.5f;
 public int GodPickupCount=1;
 public float BoundaryMargin=0, CullMargin=4, PickupSpawnInset=1;
 public SceneMode InitialSceneMode=SceneMode.Heaven;
 public Sprite PlatformSprite, CircleSprite;
 public GameObject PlatformPrefab, CrumblePrefab, GodPickupPrefab;
 public GameObject HermesPickupPrefab, DemeterPickupPrefab, AthenaPickupPrefab, DionysusPickupPrefab;
 public GameObject PickupPrefabFor(GodKind kind) {
 var prefab=kind==GodKind.Hermes?HermesPickupPrefab:kind==GodKind.Demeter?DemeterPickupPrefab:kind==GodKind.Athena?AthenaPickupPrefab:DionysusPickupPrefab;
 return prefab?prefab:GodPickupPrefab;
 }
 void OnValidate() {
 SceneFlipInterval=Mathf.Max(.1f,SceneFlipInterval);
 GrowthJumpHeightMultiplier=Mathf.Max(1f,GrowthJumpHeightMultiplier);
 RoundDuration=Mathf.Max(1,RoundDuration); GodInterval=Mathf.Max(.1f,GodInterval);
 RespawnDuration=Mathf.Max(.1f,RespawnDuration); SightRadius=Mathf.Max(SoundRadius+.1f,SightRadius);
 TasteSecondJumpHeightMultiplier=Mathf.Clamp(TasteSecondJumpHeightMultiplier,.1f,1f);
 PlatformLaneCount=Mathf.Clamp(PlatformLaneCount,1,5);
 PlatformWidth=Mathf.Max(1,PlatformWidth);
 PlatformMinimumWidth=Mathf.Clamp(PlatformMinimumWidth,1,PlatformWidth);
 PlatformSpacingVariation=Mathf.Clamp(PlatformSpacingVariation,0,.3f);
 PlatformHorizontalJitter=Mathf.Max(0,PlatformHorizontalJitter);
 PlatformVerticalJitter=Mathf.Clamp(PlatformVerticalJitter,0,.15f);
 PlatformLaneStagger=Mathf.Clamp(PlatformLaneStagger,0,.75f);
 PlatformEdgeInset=Mathf.Max(0,PlatformEdgeInset);
 PlatformSpacing=Mathf.Clamp(PlatformSpacing,.5f,3); ScrollSpeed=Mathf.Max(0,ScrollSpeed);
 }
} }