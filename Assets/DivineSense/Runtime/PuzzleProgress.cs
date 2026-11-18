using UnityEngine;
namespace DivineSense {
 public static class PuzzleProgress {
  static ActiveKind[] active; static MovementKind[] movement;
  public static void Capture(GameSession session) {
   active=new ActiveKind[2]; movement=new MovementKind[2];
   for(int i=0;i<2;i++){active[i]=session.Registry.Players[i].HomeLoadout.ActiveKind;movement[i]=session.Registry.Players[i].HomeLoadout.MovementKind;}
  }
  public static void Begin(MatchSetupController setup) {
   setup.Begin(active==null?ActiveKind.Sight:active[0],movement==null?MovementKind.Touch:movement[0],active==null?ActiveKind.Sound:active[1],movement==null?MovementKind.Taste:movement[1]);
  }
 }
}
