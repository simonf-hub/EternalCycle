using UnityEngine;
namespace DivineSense { public sealed class PositionSwapEffect : MonoBehaviour {
 public GameSession Session;
 public void Apply(){if(!Session.IsPlaying)return;var ps=Session.Registry.Players;Vector2 a=ps[0].Endpoint,b=ps[1].Endpoint;ps[0].SetEndpoint(b);ps[1].SetEndpoint(a);foreach(var p in ps)if(p.CanAct&&!Session.World.Contains(p.Endpoint))p.Life.TryDie();}
} }