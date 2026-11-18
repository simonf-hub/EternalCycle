using System;
using System.Reflection;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DivineSenseEditor {
 public static class DoubleJumpValidation {
  static double started; static int stage; static PlayerContext player;
  static readonly FieldInfo Pending=typeof(PlayerInputReader).GetField("jumpPending",BindingFlags.Instance|BindingFlags.NonPublic);
  [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("DoubleJumpValidation",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
  public static void Run(){SessionState.SetBool("DoubleJumpValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  static void PressJump(){Pending.SetValue(player.Input,true);player.Motor.SendMessage("FixedUpdate");}
  static void CheckTwoJumps(){
   PressJump();Check(Mathf.Abs(player.Motor.Body.linearVelocity.y-player.Settings.JumpSpeed)<.01f,"First jump must have full height");
   PressJump();Check(Mathf.Abs(player.Motor.Body.linearVelocity.y-player.Settings.JumpSpeed*Mathf.Sqrt(player.Settings.TasteSecondJumpHeightMultiplier))<.01f,"Second jump must use reduced height");
  }
  static void Tick(){try{
   if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Double jump validation timed out");
   if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   var session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session)return;
   if(stage==0){
    UnityEngine.Object.FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Taste,ActiveKind.Sound,MovementKind.Touch);
    UnityEngine.Object.FindFirstObjectByType<PlatformSpawner>().enabled=false;
    UnityEngine.Object.FindFirstObjectByType<GodDirector>().enabled=false;
    session.Settings.ScrollSpeed=0;
    foreach(var platform in UnityEngine.Object.FindObjectsByType<PlatformScroller>(FindObjectsSortMode.None)){
     if(platform.name!="Starting Dais")UnityEngine.Object.Destroy(platform.gameObject);else platform.enabled=false;
    }
    foreach(var follower in UnityEngine.Object.FindObjectsByType<FollowerCollectible>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(follower.gameObject);
    player=session.Registry.Players[0];player.Input.enabled=false;player.Motor.enabled=false;
    player.Motor.Teleport(new Vector2(0,5),true);player.Motor.Body.linearVelocity=Vector2.down;
    CheckTwoJumps();player.Motor.Body.linearVelocity=Vector2.down;PressJump();
    Check(player.Motor.Body.linearVelocity.y<0,"Third airborne jump must be rejected");
    player.Motor.Teleport(new Vector2(0,-4),false);player.Motor.Body.linearVelocity=Vector2.down;player.Motor.enabled=true;stage=1;
   }
   else if(stage==1&&player.Motor.Grounded){
    player.Motor.enabled=false;CheckTwoJumps();
    Debug.Log("DOUBLE_JUMP_VALIDATION_OK: two jumps after falling, full first jump, reduced second jump, third rejected, landing restores both jumps");SessionState.SetBool("DoubleJumpValidation",false);EditorApplication.Exit(0);
   }
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("DoubleJumpValidation",false);EditorApplication.Exit(1);}}
 }
}
