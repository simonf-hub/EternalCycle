using UnityEngine;
namespace DivineSense { public sealed class PlayerContext : MonoBehaviour {
 public int Id; public GameSession Session; public PlayerRegistry Registry;
 public PlayerInputReader Input; public PlayerMotor2D Motor; public PlayerLifecycle Life;
 public FollowerCounter Score; public RespawnController Respawn; public PlayerStatusEffects Status;
 public PlayerAbilityController Abilities; public PlayerVisuals Visuals;
 public RuntimeLoadout HomeLoadout, EquippedLoadout;
 public GameSettings Settings => Session.Settings;
 public bool CanAct => Session.IsPlaying && Life.State==LifeState.Alive;
 public Vector2 Endpoint => Life.State==LifeState.Alive ? Motor.Body.position : Respawn.Position;
 public void SetEndpoint(Vector2 p) { if(Life.State==LifeState.Alive) Motor.Teleport(p,false); else Respawn.SetPosition(p); }
} }