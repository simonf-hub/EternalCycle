using UnityEngine;
namespace DivineSense { public sealed class GlideAbility : MonoBehaviour {
 public float Limit(PlayerContext p,float velocity) => p.EquippedLoadout.MovementKind==MovementKind.Touch && p.Input.Held && velocity<0 ? Mathf.Max(velocity,-p.Settings.GlideSpeed):velocity;
} }