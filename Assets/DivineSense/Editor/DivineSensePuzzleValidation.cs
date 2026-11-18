using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DivineSenseEditor {
 public static class DivineSensePuzzleValidation {
  static int stage; static double started;
  [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("PuzzleValidation",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
  public static void Run(){SessionState.SetBool("PuzzleValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
  static void Tick(){try{
   if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Puzzle integration validation timed out");
   if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   var session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session)return;
   if(stage==0&&!session.IsPuzzle){
    var setup=UnityEngine.Object.FindFirstObjectByType<MatchSetupController>();if(!setup)return;
    setup.Begin(ActiveKind.Sight,MovementKind.Taste,ActiveKind.Sound,MovementKind.Touch);
    Check(session.Settings.RoundDuration==180,"Arena must last 180 seconds");Time.timeScale=100;stage=1;
   }
   if(stage==1&&session.IsPuzzle&&session.IsPlaying){
    Time.timeScale=1;
    var a=session.Registry.Players[0];var b=session.Registry.Players[1];
    Check(a.HomeLoadout.MovementKind==MovementKind.Taste&&b.HomeLoadout.MovementKind==MovementKind.Touch,"Original movement loadouts were not retained");
    Check(a.HomeLoadout.ActiveKind==ActiveKind.Sight&&b.HomeLoadout.ActiveKind==ActiveKind.Sound,"Original senses were not retained");
    var hidden=UnityEngine.Object.FindObjectsByType<MicroSense.HiddenPlatform>(FindObjectsSortMode.None);var walls=UnityEngine.Object.FindObjectsByType<MicroSense.BreakablePlatform>(FindObjectsSortMode.None);
    Check(hidden.Length>0&&walls.Length>0,"Level 1 puzzle geometry missing");
    var platform=hidden[0];var wall=walls[0];
    a.Motor.Teleport((Vector2)platform.transform.position+Vector2.up,true);a.Abilities.Sight.Activate(a);Check(platform.IsRevealed&&platform.IsSolid,"Sight failed to reveal a solid platform");
    b.Motor.Teleport((Vector2)wall.transform.position+Vector2.left,true);b.Abilities.Sound.Activate(b);Check(wall.IsBroken,"Sound failed to break a wall");
    var controller=UnityEngine.Object.FindFirstObjectByType<PuzzleLevelController>();
    a.Motor.Teleport(controller.Exit.position,true);b.Motor.Teleport(controller.Exit.position,true);stage=2;
   }
   if(stage==2&&session.State==MatchState.Finished){Debug.Log("DIVINE_PUZZLE_VALIDATION_OK: 180-second automatic transition, carried loadouts, platform reveal, wall break, cooperative exit");SessionState.SetBool("PuzzleValidation",false);EditorApplication.Exit(0);}
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("PuzzleValidation",false);EditorApplication.Exit(1);}}
 }
}
