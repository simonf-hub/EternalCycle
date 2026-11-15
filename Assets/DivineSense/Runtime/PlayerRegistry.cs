using UnityEngine;
namespace DivineSense { public sealed class PlayerRegistry : MonoBehaviour {
 public PlayerContext[] Players;
 void Start(){Physics2D.IgnoreCollision(Players[0].Motor.Collider,Players[1].Motor.Collider);}
 public PlayerContext Opponent(PlayerContext p) => Players[1-p.Id];
} }