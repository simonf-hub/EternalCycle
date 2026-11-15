using UnityEngine;
namespace DivineSense { public sealed class MatchSetupController : MonoBehaviour {
 public GameSession Session;
 public void Begin(ActiveKind a,MovementKind m,ActiveKind b,MovementKind n) {
 if(Session.State!=MatchState.Setup)return;
 var ps=Session.Registry.Players;
 ps[0].HomeLoadout=new RuntimeLoadout(0,a,m); ps[1].HomeLoadout=new RuntimeLoadout(1,b,n);
 foreach(var p in ps) {p.EquippedLoadout=p.HomeLoadout;p.Score.Initialize();p.Motor.Begin();}
 Session.StartRound();
 }
} }