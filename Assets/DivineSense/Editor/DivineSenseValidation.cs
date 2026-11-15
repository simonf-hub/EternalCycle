using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DivineSenseEditor {
public static class DivineSenseValidation {
 static GameSession session; static bool initialized,respawnChecked; static double start; static int gods,last=-1,flips; static bool failed;
 [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("DivineValidation",false)){start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
 public static void Run(){
 SessionState.SetBool("DivineValidation",true);
 Check(FollowerCounter.AfterDeath(1000,.2f)==800,"score 1000 -> 800");Check(FollowerCounter.AfterDeath(1,.2f)==0,"rounded score 1 -> 0");
 for(int previous=0;previous<4;previous++)for(int roll=0;roll<3;roll++)Check(GodDirector.ExcludingPrevious(roll,previous)!=previous,"god exclusion");
 EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity");start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
 }
 static void Check(bool ok,string name){if(!ok)throw new Exception("VALIDATION FAILED: "+name);}
 static void Tick(){try{
 if(EditorApplication.timeSinceStartup-start>100)throw new Exception("validation timeout");
 if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
 if(!initialized && EditorApplication.timeSinceStartup-start<12)return;
 if(!initialized){session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session||!session.Registry.Players[0].Input)return;
 var setup=UnityEngine.Object.FindFirstObjectByType<MatchSetupController>();setup.Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);
 var a=session.Registry.Players[0];var b=session.Registry.Players[1];
 a.Motor.Teleport(new Vector2(-1,-3),true);b.Motor.Teleport(new Vector2(1,-3),true);a.Abilities.TryActivateActive();Check(b.Status.Hidden&&a.Abilities.Cooldown==20,"Sight visibility and cooldown");b.Status.HiddenUntil=0;b.Status.GrownUntil=3;a.Abilities.Sight.Activate(a);Check(!b.Status.Hidden,"Growth immunity to Sight");b.Status.Clear();
 a.Score.Add(1000);a.Life.TryDie();a.Life.TryDie();Check(a.Score.Total==800,"idempotent death");
 Vector2 marker=a.Endpoint,body=b.Endpoint;UnityEngine.Object.FindFirstObjectByType<PositionSwapEffect>().Apply();Check(Vector2.Distance(a.Endpoint,body)<.01f&&Vector2.Distance(b.Endpoint,marker)<.01f,"marker position swap");
 a.HomeLoadout.ActiveReadyAt=12;b.HomeLoadout.ActiveReadyAt=4;session.Swap.Apply();Check(a.EquippedLoadout==b.HomeLoadout&&a.Abilities.Cooldown==4,"bundle reference and cooldown transfer");a.EquippedLoadout.ActiveReadyAt=20;session.Swap.Restore();Check(b.EquippedLoadout.ActiveReadyAt==20,"used cooldown returns home");
 var world=session.World;var platforms=UnityEngine.Object.FindObjectsByType<PlatformScroller>(FindObjectsSortMode.None);var before=platforms[0].transform.position;float sign=world.ScrollVelocity;world.ToggleMode();Check(world.ScrollVelocity==-sign&&platforms[0].transform.position==before,"mode reversal preserves layout");
 world.ModeChanged+=mode=>{flips++;if(session.Now<flips*session.Settings.SceneFlipInterval || session.Now>flips*session.Settings.SceneFlipInterval+.5f)failed=true;};
 var director=UnityEngine.Object.FindFirstObjectByType<GodDirector>();director.Intervened+=k=>{if((int)k==last || k==GodKind.Zeus)failed=true;last=(int)k;gods++;var gifts=director.PickupsRoot.GetComponentsInChildren<GodPickup>();if(gifts.Length<session.Settings.GodPickupCount)failed=true;};Time.timeScale=30;initialized=true;Debug.Log("DIVINE_INITIAL_CHECKS_OK");return;}
 if(session.Now>=3.2f&&!respawnChecked){Check(session.Registry.Players[0].Life.State==LifeState.Alive,"automatic respawn");respawnChecked=true;}
 if(session.State==MatchState.Finished){Check(session.Now==180,"exact round deadline");Check(gods==35&&!failed,"35 nonconsecutive god power-up drops before expiry");Check(flips==11,"eleven automatic 16-second flips, none at round end");var a=session.Registry.Players[0];int score=a.Score.Total;a.Score.Add(100);a.Life.TryDie();Check(score==a.Score.Total,"finished score frozen");Debug.Log("DIVINE_VALIDATION_OK: arithmetic, death guard, marker swap, cooldown ownership, scene reversal, respawn, 180s match, 35 god drops, eleven 16s automatic flips, frozen result");Time.timeScale=1;EditorApplication.update-=Tick;SessionState.SetBool("DivineValidation",false);EditorApplication.Exit(0);}
 }catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;SessionState.SetBool("DivineValidation",false);EditorApplication.Exit(1);}}
}
}
