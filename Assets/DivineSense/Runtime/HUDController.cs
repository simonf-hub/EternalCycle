using EternalCycle;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EternalCycle.LocalGamepads;
namespace DivineSense { public sealed class HUDController : MonoBehaviour {
 public GameSession Session; public MatchSetupController Setup; public GodDirector Gods;
 int a,b,c=1,d=1; GUIStyle title,text,small; Texture2D panel;
 int[] setupTargets={0,1}; float nextSettingChange;
 float deathLossPercent,scrollSpeed,maximumScrollSpeed;
 internal static Rect FollowerLabelRect(int player) => new Rect(player==0?25:890,12,365,30);
 internal static Color FollowerColor(int player) => player==0?new Color(1,.88f,.5f):new Color(1,.46f,.46f);
 void Start(){
 deathLossPercent=Mathf.Clamp(Session.Settings.DeathLossFraction*100f,0,100);
 scrollSpeed=Mathf.Max(0,Session.Settings.ScrollSpeed);
 maximumScrollSpeed=Mathf.Max(5,scrollSpeed);
 }
 void BeginMatch(){
 Session.Settings.DeathLossFraction=deathLossPercent/100f;
 Session.Settings.ScrollSpeed=scrollSpeed;
 var angelSense=(ActiveKind)a; var angelMovement=(MovementKind)b;
 var devilSense=(ActiveKind)c; var devilMovement=(MovementKind)d;
 var tutorial=new ArenaTutorial(Session.Settings,(angelSense,angelMovement),(devilSense,devilMovement));
 Session.ShowNarrative(false,()=>Setup.Begin(angelSense,angelMovement,devilSense,devilMovement),tutorial);
 }
 void Update(){
 if(Session.Trial!=TrialLevel.Prototype)return;
 if(Session.IsShowingNarrative||Session.IsShowingResults)return;
 if(Session.State==MatchState.Finished){if(LocalGamepads.ConfirmPressed)SceneManager.LoadScene(SceneManager.GetActiveScene().path);return;}
 if(Session.State!=MatchState.Setup)return;
 for(int i=0;i<2;i++){
 var pad=LocalGamepads.ForPlayer(i);if(pad==null)continue;
 if(pad.buttonNorth.wasPressedThisFrame)setupTargets[i]=1-setupTargets[i];
 int target=setupTargets[i];
 if(pad.dpad.left.wasPressedThisFrame||pad.dpad.right.wasPressedThisFrame){if(target==0)a=1-a;else c=1-c;}
 if(pad.dpad.up.wasPressedThisFrame||pad.dpad.down.wasPressedThisFrame){if(target==0)b=1-b;else d=1-d;}
 if(pad.leftShoulder.wasPressedThisFrame)deathLossPercent=Mathf.Max(0,deathLossPercent-5);
 if(pad.rightShoulder.wasPressedThisFrame)deathLossPercent=Mathf.Min(100,deathLossPercent+5);
 float stick=pad.rightStick.y.ReadValue();
 if(Mathf.Abs(stick)>.5f&&Time.unscaledTime>=nextSettingChange){scrollSpeed=Mathf.Clamp(scrollSpeed+Mathf.Sign(stick)*.1f,0,maximumScrollSpeed);nextSettingChange=Time.unscaledTime+.2f;}
 if(pad.buttonSouth.wasPressedThisFrame||pad.startButton.wasPressedThisFrame){BeginMatch();return;}
 }
 }
 void OnGUI(){
 if(Session.IsShowingNarrative)return;
 if(title==null){title=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};text=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter};small=new GUIStyle(text){fontSize=14};panel=Texture2D.whiteTexture;}
 var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/800f,1));
 GUI.color=new Color(.025f,.035f,.065f,.92f);GUI.DrawTexture(new Rect(0,0,1280,105),panel);GUI.color=Color.white;
 GUI.Label(new Rect(0,8,1280,36),"DIVINE-SENSE   /   "+Session.World.CurrentMode,title);
 GUI.Label(new Rect(480,49,320,35),Mathf.CeilToInt(Mathf.Max(0,Session.Settings.RoundDuration-Session.Now))+"s   •   FLIP IN "+Mathf.CeilToInt(Session.World.FlipRemaining)+"s",text);
 if(Session.Trial==TrialLevel.Arena)
     GUI.Label(new Rect(390,81,500,20),$"Orange crumbles {Session.Settings.CrumbleDelay:0.##}s after landing · Sound breaks cyan",small);
 for(int i=0;i<2;i++){var p=Session.Registry.Players[i];var scoreRect=FollowerLabelRect(i);float x=scoreRect.x;if(!Session.IsShowingResults){GUI.color=FollowerColor(i);GUI.Label(scoreRect,ContestantName(i)+"   "+p.Score.Total+" followers",text);GUI.color=Color.white;}if(p.EquippedLoadout!=null)GUI.Label(new Rect(x,44,365,28),p.EquippedLoadout.ActiveKind+" "+(p.Abilities.Cooldown>0?Mathf.CeilToInt(p.Abilities.Cooldown)+"s":"READY")+" / "+p.EquippedLoadout.MovementKind,small);GUI.Label(new Rect(x,72,365,22),p.Life.State==LifeState.RespawnPlacement?"PLACE CROSS: "+p.Respawn.Remaining.ToString("0.0")+"s":p.Status.Grown?"AGILITY • SIGHT / SOUND IMMUNE":p.Status.Reversed?"CONTROLS REVERSED":"",small);}
 GUI.color=new Color(.025f,.035f,.065f,.9f);GUI.DrawTexture(new Rect(0,730,1280,70),panel);GUI.color=Color.white;
 string controls=Session.Trial==TrialLevel.Prototype?"Stick / D-pad · A jump · B cast":"Left stick move · A jump · B cast";
 string objective=string.IsNullOrEmpty(Gods.Cue) && Session.Trial==TrialLevel.Arena
     ?"Race for followers · Sight reveals bridges · Sound breaks cyan platforms":Gods.Cue;
 GUI.Label(new Rect(0,733,1280,26),objective,text);
 GUI.Label(new Rect(0,765,1280,25),$"Mortal 1  A/D move · W jump · S cast    |    Mortal 2  ←/→ move · ↑ jump · ↓ cast    |    {controls}",small);
 if(Session.Trial==TrialLevel.Prototype && Session.State==MatchState.Setup){
 GUI.Label(new Rect(40,150,1200,35),LocalGamepads.Status,small);
 GUI.Box(new Rect(270,200,740,470),"");
 GUI.Label(new Rect(290,220,700,45),"SEPARATE SENSES. SHARED SURVIVAL.",title);
 GUI.Label(new Rect(290,275,700,45),"Win followers. Survive the scroll. Outplay your rival.",text);
 GUI.Label(new Rect(310,315,300,30),ContestantName(0),text);
 a=GUI.SelectionGrid(new Rect(310,350,300,40),a,new[]{"Sight","Sound"},2);
 b=GUI.SelectionGrid(new Rect(310,405,300,40),b,new[]{"Touch: glide","Taste: double jump"},2);
 GUI.Label(new Rect(670,315,300,30),ContestantName(1),text);
 c=GUI.SelectionGrid(new Rect(670,350,300,40),c,new[]{"Sight","Sound"},2);
 d=GUI.SelectionGrid(new Rect(670,405,300,40),d,new[]{"Touch: glide","Taste: double jump"},2);
 GUI.Label(new Rect(300,462,350,30),"Followers lost per death: "+deathLossPercent.ToString("0")+"%",text);
 deathLossPercent=Mathf.Round(GUI.HorizontalSlider(new Rect(680,472,280,20),deathLossPercent,0,100));
 GUI.Label(new Rect(300,507,350,30),"Platform scroll speed: "+scrollSpeed.ToString("0.0")+" units/s",text);
 scrollSpeed=Mathf.Round(GUI.HorizontalSlider(new Rect(680,517,280,20),scrollSpeed,0,maximumScrollSpeed)*10f)/10f;
 GUI.Label(new Rect(290,548,700,30),"PAD: D-pad ↔ sense / ↕ movement · top button switches player · bottom/Start begins",small);
 if(GUI.Button(new Rect(490,600,300,50),"ENTER THE COLOSSEUM")){
 BeginMatch();
 }
 GUI.Label(new Rect(290,574,700,25),"PAD 1 editing "+ContestantName(setupTargets[0])+" · PAD 2 editing "+ContestantName(setupTargets[1])+" · LB/RB death loss · right stick ↕ scroll speed",small);
 }
 if(Session.Trial==TrialLevel.Prototype&&Session.State==MatchState.Finished&&!Session.IsShowingResults){GUI.Box(new Rect(250,290,780,190),"");GUI.Label(new Rect(265,310,750,60),Session.Result,text);if(GUI.Button(new Rect(490,400,300,45),"PLAY AGAIN"))SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
 GUI.matrix=old;
 }
} }
