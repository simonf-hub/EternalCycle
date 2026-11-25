using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RespawnRegression {
 static GameSession session;static PlayerContext p;static double start;static int stage;static float deathAt;static int rebirths;static float rebornAt;static bool wrongPosition;static Vector2 expected;static int cycle;
 [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("RespawnCheck",false)){start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
 public static void Run(){SessionState.SetBool("RespawnCheck",true);EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;}
 static void Tick(){try{
 if(EditorApplication.timeSinceStartup-start>90)throw new Exception("timeout");
 if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup-start<12)return;
 if(stage==0){session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session)return;UnityEngine.Object.FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);p=session.Registry.Players[0];p.Life.Reborn+=()=>{rebirths++;rebornAt=session.Now;wrongPosition=Vector2.Distance(p.Motor.Body.position,expected)>.1f || Vector2.Distance(p.Motor.BodyVisual.position,expected)>.1f;Debug.Log("REBORN now="+session.Now+" body="+p.Motor.Body.position+" transform="+p.transform.position+" marker="+p.Respawn.Position);};Vector2 outside=new Vector2(0,session.World.VisibleBounds.yMin-2);p.transform.position=outside;p.Motor.Body.position=outside;stage=1;return;}
 if(stage==1&&p.Life.State==LifeState.RespawnPlacement){deathAt=session.Now;p.Respawn.SetPosition(new Vector2(session.World.VisibleBounds.xMin+1,0));expected=p.Respawn.Position;Debug.Log("DEAD at="+deathAt+" remaining="+p.Respawn.Remaining+" marker="+p.Respawn.Position);stage=2;}
 if(stage==2&&session.Now>deathAt+3.3f){Debug.Log("AFTER: life="+p.Life.State+" body="+p.Motor.Body.position+" visual="+p.Motor.BodyVisual.position+" marker="+p.Respawn.Position+" remaining="+p.Respawn.Remaining+" rebirths="+rebirths);if(wrongPosition||rebirths!=1||p.Life.State!=LifeState.Alive||!session.World.Contains(p.Motor.BodyVisual.position))throw new Exception("offscreen death failed visible respawn");if(rebornAt-deathAt<2.9f||rebornAt-deathAt>3.1f)throw new Exception("wrong duration");cycle++;if(cycle<3){rebirths=0;wrongPosition=false;Vector2 outside=new Vector2(0,session.World.VisibleBounds.yMin-2);p.transform.position=outside;p.Motor.Body.position=outside;stage=1;return;}Debug.Log("RESPAWN_REGRESSION_PASS: 3 offscreen deaths, 3 timed respawns at marker");Finish(0);}
 }catch(Exception e){Debug.LogException(e);Finish(1);}}
 static void Finish(int exit){SessionState.SetBool("RespawnCheck",false);EditorApplication.update-=Tick;EditorApplication.Exit(exit);}
}
