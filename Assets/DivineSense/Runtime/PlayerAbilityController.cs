using System;
using UnityEngine;
namespace DivineSense { public sealed class PlayerAbilityController : MonoBehaviour {
 public PlayerContext Owner; public SightAbility Sight; public SoundAbility Sound;
 public event Action<ActiveKind> Activated;
 public float Cooldown=>Owner.EquippedLoadout==null?0:Mathf.Max(0,Owner.EquippedLoadout.ActiveReadyAt-Owner.Session.Now);
 public void TryActivateActive(){if(!Owner.CanAct||Owner.Status.Frozen||Cooldown>0)return;var b=Owner.EquippedLoadout;b.ActiveReadyAt=Owner.Session.Now+(b.ActiveKind==ActiveKind.Sight?Owner.Settings.SightCooldown:Owner.Settings.SoundCooldown);if(b.ActiveKind==ActiveKind.Sight)Sight.Activate(Owner);else Sound.Activate(Owner);Activated?.Invoke(b.ActiveKind);}
} }