using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DivineSenseEditor {
 public static class MedusaStareValidation {
  static bool runtime; static double started; static int stage; static float hitAt; static PlayerContext player;
  static Vector2 hitPosition;
  static RigidbodyConstraints2D originalConstraints;
  static RigidbodyInterpolation2D originalInterpolation;
  static bool sawFall, landedWhileStared;
  [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("StareValidation",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
  public static void Run(){SessionState.SetBool("StareValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  static void CheckHorizontalLock(Rigidbody2D body)
  {
   Check(body.simulated&&body.constraints==(originalConstraints|RigidbodyConstraints2D.FreezePositionX)&&
    body.interpolation==originalInterpolation,"Stare should lock only horizontal movement");
   Check(Mathf.Abs(body.position.x-hitPosition.x)<.001f&&Mathf.Abs(body.linearVelocity.x)<.001f,
    "The stunned mortal moved horizontally");
  }
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
    Rigidbody2D body=player.Motor.Body;
    hitAt=session.Now;
    hitPosition=body.position;
    originalConstraints=body.constraints;
    originalInterpolation=body.interpolation;
    body.linearVelocity=Vector2.down*4f;
    player.Status.ApplyFreeze();
    player.Status.ClearFreeze();
    Check(Mathf.Approximately(body.linearVelocity.y,-4f),"Clearing Stare interrupted a fall");
    body.linearVelocity=new Vector2(4,3);
    player.Motor.QueueKnockback(new Vector2(2,5));
    player.Status.ApplyFreeze();
    Check(player.Status.Frozen,"Stare did not take effect on hit");
    CheckHorizontalLock(body);
    Check(Mathf.Approximately(body.linearVelocity.y,3f),"Stare interrupted existing vertical momentum");
    if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.A,Key.W,Key.S));InputSystem.Update();
    stage=1;
   }
   if(stage==1){
    float elapsed=session.Now-hitAt;
    Rigidbody2D body=player.Motor.Body;
    if(player.Status.Frozen){
     Check(elapsed<3.05f,"Stare did not expire after three seconds");
     CheckHorizontalLock(body);
     if(!sawFall&&elapsed>.35f){
      Check(body.position.y<hitPosition.y-.5f&&body.linearVelocity.y<-1f,"Gravity did not pull the stared mortal down");
      sawFall=true;
     }
     if(player.Motor.Grounded&&body.position.y<hitPosition.y-1f)landedWhileStared=true;
     player.Motor.QueueKnockback(new Vector2(2,5));
     Check(player.Input.Horizontal==0&&!player.Input.Held&&!player.Input.TakeJump()&&!player.Input.TakeAbility(),"Frozen input was accepted");
     Check(player.Abilities.Cooldown==0,"Frozen ability input activated Sight");
     return;
    }

    Check(elapsed>=2.99f,"Stare expired prematurely");
    Check(sawFall&&landedWhileStared,"The stared mortal did not fall and land before Stare expired");
    if(elapsed<3.2f)return;
    Check(player.CanAct&&body.constraints==originalConstraints&&body.interpolation==originalInterpolation,
     "Normal physics did not resume after Stare");
    Check(player.Input.Horizontal<-.5f&&Mathf.Abs(body.linearVelocity.x+player.Settings.MoveSpeed)<.02f,
     "Movement did not resume cleanly after Stare");
    Check(!player.Input.Held&&!player.Input.TakeJump()&&!player.Input.TakeAbility()&&player.Abilities.Cooldown==0,
     "Held jump or ability replayed on expiry");
    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
    Debug.Log("MEDUSA_STARE_VALIDATION_OK: horizontal lock, preserved vertical momentum, gravity and landing during Stare, input and knockback suppressed, three-second expiry");
    SessionState.SetBool("StareValidation",false);EditorApplication.Exit(0);
   }
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("StareValidation",false);EditorApplication.Exit(1);}}
 }
 public sealed class MedusaStareValidationRunner : MonoBehaviour { void Update(){MedusaStareValidation.Tick();} }
}
