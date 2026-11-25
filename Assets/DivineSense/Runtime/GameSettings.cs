using UnityEngine;
using UnityEngine.Serialization;
namespace DivineSense {
[CreateAssetMenu(menuName="Divine-Sense/Game Settings")]
public sealed class GameSettings : ScriptableObject {
 public float RoundDuration=180, GodInterval=5, RespawnDuration=3;
 [Min(.1f)] public float SceneFlipInterval=16;
 public int FollowerPickupValue=100, StartingFollowers=0;
 [Range(0,1)] public float DeathLossFraction=.2f;
 public float SightDuration=1.5f, SightCooldown=8, SoundCooldown=4;
 public float GrowthDuration=3, AbilitySwapDuration=3, ReverseControlsDuration=3;
 public float SightRadius=7, SoundRadius=4, SoundImpulse=16;
 public float MoveSpeed=6, JumpSpeed=12, Gravity=3, GlideSpeed=.5f, KnockbackDecay=6;
 [Header("Glide release dash")]
 [Tooltip("Minimum upward distance travelled when releasing an active glide.")]
 [Min(0)] public float GlideDashDistance=1.5f;
 [Min(.01f)] public float GlideDashDuration=.12f;
 [Tooltip("Maximum upward dash distance at full glide charge.")]
 [Min(0)] public float GlideDashMaxDistance=4f;
 [Tooltip("Seconds spent gliding to reach full dash charge.")]
 [Min(.01f)] public float GlideDashChargeTime=1.5f;
 [Tooltip("Taste's second jump height relative to the first jump. Landing restores full jump height.")]
 [Range(.1f,1f)] public float TasteSecondJumpHeightMultiplier=.8f;
 [Header("Taste horizontal dash")]
 [Tooltip("Maximum time in seconds between two presses in the same direction.")]
 [Min(.01f)] public float TasteDashTapWindow=.25f;
 [Min(0)] public float TasteDashDistance=2f;
 [Min(.01f)] public float TasteDashDuration=.12f;
 public float ScrollSpeed=1.5f, PlatformSpacing=2f, PlatformWidth=4.2f, CrumbleDelay=1.5f;
 [Header("Platform scatter")]
 [Min(1)] public int PlatformLaneCount=3;
 public float PlatformMinimumWidth=2.8f, PlatformSpacingVariation=.15f;
 public float PlatformHorizontalJitter=1.1f, PlatformVerticalJitter=.12f, PlatformEdgeInset=.7f;
 [Range(0,1)] public float PlatformConnectorChance=.5f;
 public float PlatformConnectorHeight=.7f, PlatformLaneStagger=.75f;
 [Range(0,1)] public float CrumbleChance=.25f, FollowerDensity=.8f;
 [Tooltip("Jump height multiplier while Zeus's Agility is active, including Taste jumps.")]
 [Min(1f)] public float GrowthJumpHeightMultiplier=1.5f;
 public float GrowthScale=1.5f, GrowthSlowdown=.65f, MarkerSpeed=6, PickupFallSpeed=2.5f;
 [Header("God power-ups")]
 [Min(0)] public float PullImpulse=14;
 [Min(.1f)] public float HomingBombSpeed=7, HomingBombLifetime=6;
 public HomingBomb HomingBombPrefab;

 public int GodPickupCount=1;
 public float BoundaryMargin=0, CullMargin=4, PickupSpawnInset=1;
 public SceneMode InitialSceneMode=SceneMode.Heaven;
 public Sprite PlatformSprite, CircleSprite;
 public GameObject PlatformPrefab, CrumblePrefab, GodPickupPrefab;
 public GameObject AthenaPickupPrefab, AresPickupPrefab;
 [FormerlySerializedAs("DemeterPickupPrefab")] public GameObject ZeusPickupPrefab;
 [FormerlySerializedAs("PoseidonPickupPrefab")] public GameObject HeraPickupPrefab;
 public GameObject PickupPrefabFor(GodKind kind) {
 var prefab = kind switch {
     GodKind.Athena or GodKind.Hermes => AthenaPickupPrefab,
     GodKind.Zeus or GodKind.Demeter => ZeusPickupPrefab,
     GodKind.Hera or GodKind.Poseidon => HeraPickupPrefab,
     GodKind.Ares => AresPickupPrefab,
     _ => null
 };
 return prefab ? prefab : GodPickupPrefab;
 }
 void OnValidate() {
 GlideDashDistance=Mathf.Max(0,GlideDashDistance);GlideDashDuration=Mathf.Max(.01f,GlideDashDuration);
 GlideDashMaxDistance=Mathf.Max(GlideDashDistance,GlideDashMaxDistance);GlideDashChargeTime=Mathf.Max(.01f,GlideDashChargeTime);
 SceneFlipInterval=Mathf.Max(.1f,SceneFlipInterval);
 GrowthJumpHeightMultiplier=Mathf.Max(1f,GrowthJumpHeightMultiplier);
 RoundDuration=Mathf.Max(1,RoundDuration); GodInterval=Mathf.Max(.1f,GodInterval);
 RespawnDuration=Mathf.Max(.1f,RespawnDuration); SightRadius=Mathf.Max(SoundRadius+.1f,SightRadius);
 TasteSecondJumpHeightMultiplier=Mathf.Clamp(TasteSecondJumpHeightMultiplier,.1f,1f);
 TasteDashTapWindow=Mathf.Max(.01f,TasteDashTapWindow);
 TasteDashDistance=Mathf.Max(0,TasteDashDistance);
 TasteDashDuration=Mathf.Max(.01f,TasteDashDuration);
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
