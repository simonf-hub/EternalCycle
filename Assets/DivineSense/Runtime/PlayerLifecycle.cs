using System;
using UnityEngine;
namespace DivineSense { public sealed class PlayerLifecycle : MonoBehaviour {
 public PlayerContext Owner; public LifeState State {get;private set;}
 public event Action Died,Reborn;
 void Update(){if(!Owner.Session.IsPuzzle&&Owner.CanAct&&!Owner.Session.World.Contains(Owner.Motor.Body.position))TryDie();}
 public void TryDie(){if(!Owner.CanAct)return;State=LifeState.RespawnPlacement;Owner.Status.ClearFreeze();Owner.Score.ApplyDeathPenalty(Owner.Settings.DeathLossFraction);Owner.Motor.Freeze();Owner.Input.Placement(true);Owner.Respawn.Begin();Died?.Invoke();}
 public void CompleteRespawn(Vector2 p){if(!Owner.Session.IsPlaying||State!=LifeState.RespawnPlacement)return;Owner.Motor.Teleport(Owner.Motor.ClearPosition(p),true);State=LifeState.Alive;Owner.Motor.Begin();Owner.Input.Placement(false);Reborn?.Invoke();}
} }