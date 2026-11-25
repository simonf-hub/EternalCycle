using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DivineSenseEditor {
 public static class GlideDashValidation {
  [InitializeOnLoadMethod] static void Resume(){if(!SessionState.GetBool("GlideDashValidation",false))return;
   InputSystem.settings=UnityEngine.Object.Instantiate(InputSystem.settings);
   InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   EditorApplication.update+=Attach;
  }
  public static void Run(){SessionState.SetBool("GlideDashValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Attach(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;EditorApplication.update-=Attach;new GameObject("Glide Dash Validation").AddComponent<GlideDashValidationRunner>();}
 }
 public sealed class GlideDashValidationRunner : MonoBehaviour {
  Gamepad pad;PlayerContext player;int stage,frame;float releaseY,releaseAt;double started;GameObject ceiling;
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  void Start(){started=EditorApplication.timeSinceStartup;
   foreach(var device in InputSystem.devices)if(device is Gamepad||device is Joystick)InputSystem.DisableDevice(device);
   pad=InputSystem.AddDevice<Gamepad>();
   var session=FindFirstObjectByType<GameSession>();FindFirstObjectByType<HUDController>().enabled=false;
   FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);
   FindFirstObjectByType<GodDirector>().enabled=false;FindFirstObjectByType<PlatformSpawner>().enabled=false;
   foreach(var platform in FindObjectsByType<PlatformScroller>(FindObjectsSortMode.None))Destroy(platform.gameObject);
   foreach(var follower in FindObjectsByType<FollowerCollectible>(FindObjectsSortMode.None))Destroy(follower.gameObject);
   player=session.Registry.Players[0];player.Life.enabled=false;player.Motor.enabled=false;player.Motor.Body.simulated=false;
   session.Registry.Players[1].Motor.enabled=false;session.Registry.Players[1].Motor.Body.simulated=false;session.Registry.Players[1].Life.enabled=false;
  }
  void LateUpdate(){try{
   if(EditorApplication.timeSinceStartup-started>30)throw new Exception("Glide dash validation timeout");
   if(++frame%4!=0)return;
   switch(stage++){
    case 0:
     player.Motor.Teleport(new Vector2(0,4),true);
     InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.left}.WithButton(GamepadButton.South));break;
    case 1:
     var v=new Vector2(0,-6);player.Motor.Glide.ApplyMotion(player,ref v,false,false);
     Check(Mathf.Abs(v.y+player.Settings.GlideSpeed)<.01f&&!player.Motor.Glide.Dashing,"Holding glide should limit falling without dashing");
     Check(player.Motor.Glide.Charging&&player.Motor.Glide.Charge01>0,"Glide should begin charging");
     int halfSteps=Mathf.Max(1,Mathf.RoundToInt(player.Settings.GlideDashChargeTime*.5f/Time.fixedDeltaTime)-1);
     for(int i=0;i<halfSteps;i++){v=Vector2.down;player.Motor.Glide.ApplyMotion(player,ref v,false,false);}
     Check(Mathf.Abs(player.Motor.Glide.Charge01-.5f)<.03f,"Half hold should produce half charge");
     int fullSteps=Mathf.CeilToInt(player.Settings.GlideDashChargeTime/Time.fixedDeltaTime)*3;
     for(int i=0;i<fullSteps;i++){v=Vector2.down;player.Motor.Glide.ApplyMotion(player,ref v,false,false);}
     Check(player.Motor.Glide.Charge01==1,"Charge must stop at full after a long hold");
     InputSystem.QueueStateEvent(pad,new GamepadState());break;
    case 2:
     v=new Vector2(0,-2);player.Motor.Glide.ApplyMotion(player,ref v,false,player.Input.TakeGlideRelease());
     Check(Mathf.Abs(player.Motor.Glide.LastDashDistance-player.Settings.GlideDashMaxDistance)<.01f&&player.Motor.Glide.Charge01==0,"Full release should use max distance and spend charge");
     Check(v.y>10&&Mathf.Abs(v.x)<.01f,"Release must dash upward without changing horizontal velocity");
     float distance=Mathf.Abs(v.y)*Time.fixedDeltaTime;
     for(int i=0;i<30&&player.Motor.Glide.Dashing;i++){v=new Vector2(0,-2);player.Motor.Glide.ApplyMotion(player,ref v,false,false);distance+=Mathf.Abs(v.y)*Time.fixedDeltaTime;}
     Check(Mathf.Abs(distance-player.Motor.Glide.LastDashDistance)<.01f,"Dash distance differs from configured distance");
     v=new Vector2(0,30);player.Motor.Glide.ApplyMotion(player,ref v,false,false);Check(v.y<=0,"Upward momentum exceeded dash duration");
     v=Vector2.down;player.Motor.Glide.ApplyMotion(player,ref v,false,true);Check(v.y<=0,"Release without another glide repeated the dash");
     InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));break;
    case 3:
     v=Vector2.down*4;player.Motor.Glide.ApplyMotion(player,ref v,false,false);
     Check(!player.Motor.Glide.Available&&!player.Motor.Glide.Charging&&player.Motor.Glide.Charge01==0,"Second airborne hold must not recharge");
     Check(Mathf.Abs(v.y+player.Settings.GlideSpeed)<.01f,"Normal slow-fall glide should remain available after dash");
     player.Motor.Glide.Cancel();
     Check(!player.Motor.Glide.Available,"Cancelling input must not refund the airborne dash");
     player.Motor.Teleport(new Vector2(0,4),false);
     Check(!player.Motor.Glide.Available,"Midair teleport must not refund the dash");
     player.Status.ApplyFreeze();InputSystem.QueueStateEvent(pad,new GamepadState());break;
    case 4:
     player.Status.ClearFreeze();player.Input.SendMessage("Update");v=Vector2.down;player.Motor.Glide.ApplyMotion(player,ref v,false,true);
     Check(v.y<=0&&!player.Motor.Glide.Dashing,"Medusa cancellation generated a dash");
     Check(!player.Motor.Glide.Available,"Medusa must not refund the dash");
     v=Vector2.zero;player.Motor.Glide.ApplyMotion(player,ref v,true,false);
     Check(player.Motor.Glide.Available,"Ground contact must restore airborne dash");
     // Finish with a real motor/physics release and a solid obstacle.
     player.Motor.Teleport(new Vector2(0,4),true);player.Motor.Begin();player.Motor.Body.linearVelocity=Vector2.down*4;player.Motor.enabled=true;releaseAt=player.Session.Now;
     ceiling=new GameObject("Dash Collision Ceiling");ceiling.transform.position=new Vector3(0,5.3f,0);ceiling.AddComponent<BoxCollider2D>().size=new Vector2(10,.2f);
     InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));break;
    case 5:
     if(player.Session.Now-releaseAt<.15f){stage--;break;}
     Check(Mathf.Abs(player.Motor.Body.linearVelocity.y+player.Settings.GlideSpeed)<1,"Real glide did not slow descent");
     player.Visuals.SendMessage("LateUpdate");
     var track=player.transform.Find("Glide Charge Track").GetComponent<LineRenderer>();
     var fill=player.transform.Find("Glide Charge Fill").GetComponent<LineRenderer>();
     Check(track.enabled&&fill.enabled,"Charge bar must be visible while gliding");
     Check(track.GetPosition(0).y<player.Motor.Collider.bounds.min.y,"Charge bar must sit under player");
     float fillRatio=(fill.GetPosition(1).x-fill.GetPosition(0).x)/(track.GetPosition(1).x-track.GetPosition(0).x);
     Check(Mathf.Abs(fillRatio-player.Motor.Glide.Charge01)<.01f,"Charge bar fill does not match charge");
     for(int i=0;i<200;i++){var chargingVelocity=Vector2.down;player.Motor.Glide.ApplyMotion(player,ref chargingVelocity,false,false);}
     releaseY=player.Motor.Body.position.y;releaseAt=player.Session.Now;
     InputSystem.QueueStateEvent(pad,new GamepadState());break;
    case 6:
     if(player.Session.Now-releaseAt<.2f){stage--;break;}
     Check(player.Motor.Body.position.y>releaseY+.05f,"Real input release failed to trigger dash");
     Check(player.Motor.Body.position.y<4.72f,"Dash passed through a solid ceiling");
     Check(Mathf.Abs(player.Motor.Body.position.x)<.02f,"Vertical dash added horizontal movement");
     Check(player.Motor.Body.linearVelocity.y < -player.Settings.GlideSpeed,"Gravity did not resume after upward dash");
     Check(!player.Motor.Glide.Available,"Real upward dash did not consume airborne use");
     Destroy(ceiling);
     var floor=new GameObject("Dash Reset Floor");floor.transform.position=new Vector3(0,player.Motor.Body.position.y-1.5f,0);floor.AddComponent<BoxCollider2D>().size=new Vector2(10,.3f);
     releaseAt=player.Session.Now;break;
    case 7:
     if(!player.Motor.Grounded){Check(player.Session.Now-releaseAt<3,"Character failed to land on reset floor");stage--;break;}
     Check(player.Motor.Glide.Available,"Real physics landing failed to restore dash");
     Debug.Log("GLIDE_DASH_VALIDATION_OK: partial/full capped charge, full dash distance, charge spending, bar position/fill, vertical motion, no stun dash, upward dash cap, gravity resumption, full-charge ceiling collision, one dash per airtime and real landing reset");SessionState.SetBool("GlideDashValidation",false);EditorApplication.Exit(0);break;
   }
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("GlideDashValidation",false);EditorApplication.Exit(1);}}
 }
}
