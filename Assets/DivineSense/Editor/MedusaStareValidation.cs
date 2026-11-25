using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DivineSenseEditor {
 public static class MedusaStareValidation {
  static bool runtime; static double started; static int stage; static float hitAt,initialY; static PlayerContext player;
  [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("StareValidation",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
  public static void Run(){SessionState.SetBool("StareValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  public static void Tick(){try{
   if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Stare validation timed out");
   if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   if(!runtime){runtime=true;new GameObject("Medusa Validation Runner").AddComponent<MedusaStareValidationRunner>();EditorApplication.update-=Tick;return;}
   var session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session)return;
   if(stage==0){
    InputSystem.settings=UnityEngine.Object.Instantiate(InputSystem.settings);
    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
    UnityEngine.Object.FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);
    UnityEngine.Object.FindFirstObjectByType<PlatformSpawner>().enabled=false;
    UnityEngine.Object.FindFirstObjectByType<GodDirector>().enabled=false;
    foreach(var platform in UnityEngine.Object.FindObjectsByType<PlatformScroller>(FindObjectsSortMode.None)){
     if(platform.name!="Starting Dais")UnityEngine.Object.Destroy(platform.gameObject);else platform.enabled=false;
    }
    foreach(var follower in UnityEngine.Object.FindObjectsByType<FollowerCollectible>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(follower.gameObject);
    session.Settings.SceneFlipInterval=1000;session.Settings.ScrollSpeed=0;
    player=session.Registry.Players[0];player.Motor.Teleport(new Vector2(-1,3),true);
    session.Registry.Players[1].Motor.Teleport(new Vector2(6,-3),true);
    player.Status.ApplyFreeze();player.Motor.QueueKnockback(new Vector2(1,0));
    if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.A,Key.W,Key.S));InputSystem.Update();
    hitAt=session.Now;initialY=player.Motor.Body.position.y;stage=1;
   }
   if(stage==1&&session.Now-hitAt>.35f){
    Check(player.Status.Frozen,"Stare expired prematurely");
    Check(player.Motor.Body.simulated&&player.Motor.Body.constraints==RigidbodyConstraints2D.FreezeRotation,"Stare locked physics");
    Check(player.Motor.Body.position.y<initialY-.5f&&player.Motor.Body.linearVelocity.y<0,"Gravity or held-jump glide remained blocked/enabled");
    Check(player.Motor.Body.linearVelocity.x>.5f,"Environmental knockback was blocked");
    Check(player.Input.Horizontal==0&&!player.Input.Held&&!player.Input.TakeJump()&&!player.Input.TakeAbility(),"Frozen input was accepted");
    Check(player.Abilities.Cooldown==0,"Frozen ability input activated Sight");stage=2;
   }
   if(stage==2&&session.Now-hitAt>2.5f){
    Check(player.Status.Frozen&&player.Motor.Body.position.y>-5&&player.Motor.Body.position.y<-4,"Frozen player did not land on solid environment");stage=3;
   }
   if(stage==3&&session.Now-hitAt>3.2f){
    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.A,Key.W,Key.S));InputSystem.Update();
    Check(!player.Status.Frozen&&player.CanAct,"Stare did not expire after three seconds");
    Check(!player.Input.Held&&player.Abilities.Cooldown==0,"Held jump/ability replayed on expiry");
    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
    Debug.Log("MEDUSA_STARE_VALIDATION_OK: input suppressed, gravity and knockback retained, solid landing, three-second expiry, no queued action replay");SessionState.SetBool("StareValidation",false);EditorApplication.Exit(0);
   }
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("StareValidation",false);EditorApplication.Exit(1);}}
 }
 public sealed class MedusaStareValidationRunner : MonoBehaviour { void Update(){MedusaStareValidation.Tick();} }
}
