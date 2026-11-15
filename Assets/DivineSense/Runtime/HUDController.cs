using UnityEngine;
using UnityEngine.SceneManagement;
namespace DivineSense { public sealed class HUDController : MonoBehaviour {
 public GameSession Session; public MatchSetupController Setup; public GodDirector Gods;
 int a,b,c=1,d=1; GUIStyle title,text,small; Texture2D panel;
 void OnGUI(){
 if(title==null){title=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};text=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter};small=new GUIStyle(text){fontSize=14};panel=Texture2D.whiteTexture;}
 var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/800f,1));
 GUI.color=new Color(.025f,.035f,.065f,.92f);GUI.DrawTexture(new Rect(0,0,1280,105),panel);GUI.color=Color.white;
 GUI.Label(new Rect(0,8,1280,36),"DIVINE-SENSE   /   "+Session.World.CurrentMode,title);
 GUI.Label(new Rect(480,49,320,35),Mathf.CeilToInt(Mathf.Max(0,Session.Settings.RoundDuration-Session.Now))+"s   •   FLIP IN "+Mathf.CeilToInt(Session.World.FlipRemaining)+"s",text);
 for(int i=0;i<2;i++){var p=Session.Registry.Players[i];float x=i==0?25:890;GUI.color=i==0?new Color(1,.88f,.5f):new Color(1,.46f,.46f);GUI.Label(new Rect(x,12,365,30),(i==0?"ANGEL":"DEVIL")+"   "+p.Score.Total+" followers",text);GUI.color=Color.white;if(p.EquippedLoadout!=null)GUI.Label(new Rect(x,44,365,28),p.EquippedLoadout.ActiveKind+" "+(p.Abilities.Cooldown>0?Mathf.CeilToInt(p.Abilities.Cooldown)+"s":"READY")+" / "+p.EquippedLoadout.MovementKind,small);GUI.Label(new Rect(x,72,365,22),p.Life.State==LifeState.RespawnPlacement?"PLACE CROSS: "+p.Respawn.Remaining.ToString("0.0")+"s":p.Status.Grown?"GROWTH • IMMUNE":p.Status.Reversed?"CONTROLS REVERSED":"",small);}
 GUI.color=new Color(.025f,.035f,.065f,.9f);GUI.DrawTexture(new Rect(0,730,1280,70),panel);GUI.color=Color.white;
 GUI.Label(new Rect(0,733,1280,26),Gods.Cue, text);GUI.Label(new Rect(0,765,1280,25),"ANGEL: A / D move · W jump · S power     |     DEVIL: ← / → move · ↑ jump · ↓ power     |     Gold spirits = +100",small);
 if(Session.State==MatchState.Setup){GUI.Box(new Rect(270,200,740,420),"");GUI.Label(new Rect(290,220,700,45),"SEPARATE SENSES. SHARED SURVIVAL.",title);GUI.Label(new Rect(290,275,700,45),"Win followers. Survive the scroll. Outplay your rival.",text);a=GUI.SelectionGrid(new Rect(310,350,300,40),a,new[]{"Angel: Sight","Angel: Sound"},2);b=GUI.SelectionGrid(new Rect(310,405,300,40),b,new[]{"Touch: glide","Taste: double jump"},2);c=GUI.SelectionGrid(new Rect(670,350,300,40),c,new[]{"Devil: Sight","Devil: Sound"},2);d=GUI.SelectionGrid(new Rect(670,405,300,40),d,new[]{"Touch: glide","Taste: double jump"},2);GUI.Label(new Rect(290,460,700,50),"Death costs 20%. Move your cross with your four keys to respawn.",small);if(GUI.Button(new Rect(490,535,300,50),"ENTER THE COLOSSEUM"))Setup.Begin((ActiveKind)a,(MovementKind)b,(ActiveKind)c,(MovementKind)d);}
 if(Session.State==MatchState.Finished){GUI.Box(new Rect(250,290,780,190),"");GUI.Label(new Rect(265,310,750,60),Session.Result,text);if(GUI.Button(new Rect(490,400,300,45),"PLAY AGAIN"))SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
 GUI.matrix=old;
 }
} }