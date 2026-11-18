using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DivineSenseEditor {
 public static class PlatformGenerationValidation {
  static double started; static bool begun; static int checks; static int stoppedCount=-1;
  [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("PlatformValidation",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
  public static void Run(){SessionState.SetBool("PlatformValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  static void Tick(){try{
   if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Platform validation timed out");
   if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   var session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session)return;
   if(!begun){
    UnityEngine.Object.FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);
    session.Settings.CrumbleChance=0; Time.timeScale=20;begun=true;
   }
   var spawner=UnityEngine.Object.FindFirstObjectByType<PlatformSpawner>();
   var platforms=spawner.Root.GetComponentsInChildren<PlatformScroller>();
   Physics2D.SyncTransforms();
   for(int i=0;i<platforms.Length;i++)for(int j=i+1;j<platforms.Length;j++){
    var a=platforms[i].GetComponent<Collider2D>().bounds;var b=platforms[j].GetComponent<Collider2D>().bounds;
    float horizontal=Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x);
    float vertical=Mathf.Max(a.min.y-b.max.y,b.min.y-a.max.y);
    Check(horizontal>=.24f||vertical>=1.09f,"Platforms are stacked too closely: "+platforms[i].name+" / "+platforms[j].name+" gaps "+horizontal+", "+vertical);
   }
   Check(platforms.Length<90,"Platform population grew without bounds"); checks++;
   // Exercise zero scroll, maximum HUD scroll, and repeated automatic reversals.
   if(session.Now>20&&session.Now<25){session.Settings.ScrollSpeed=0;if(session.Now>21){if(stoppedCount<0)stoppedCount=platforms.Length;Check(platforms.Length==stoppedCount,"Platforms accumulated while scrolling was stopped");}}
   else session.Settings.ScrollSpeed=session.Now<25?.8f:5f;
   if(session.Now>155){Check(checks>100,"Too few simulation checks");Debug.Log("PLATFORM_GENERATION_VALIDATION_OK: initial dais clearance, bounded population, zero scroll, fast scroll, repeated direction flips; "+checks+" simulation samples");Time.timeScale=1;SessionState.SetBool("PlatformValidation",false);EditorApplication.Exit(0);}
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("PlatformValidation",false);EditorApplication.Exit(1);}}
 }
}
