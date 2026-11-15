using UnityEngine;
namespace DivineSense { public sealed class AbilitySwapEffect : MonoBehaviour {
 public GameSession Session; public bool Active {get;private set;} float deadline;
 public void Apply(){if(!Session.IsPlaying)return;var ps=Session.Registry.Players;if(!Active){ps[0].EquippedLoadout=ps[1].HomeLoadout;ps[1].EquippedLoadout=ps[0].HomeLoadout;Active=true;}deadline=Session.Now+Session.Settings.AbilitySwapDuration;}
 void Update(){if(Active&&Session.IsPlaying&&Session.Now>=deadline)Restore();}
 public void Restore(){foreach(var p in Session.Registry.Players)p.EquippedLoadout=p.HomeLoadout;Active=false;}
} }