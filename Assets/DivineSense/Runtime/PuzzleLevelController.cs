using EternalCycle;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DivineSense {
 public sealed class PuzzleLevelController : MonoBehaviour {
  public GameSession Session;
  public Transform Exit;
  public Vector2 SpawnA, SpawnB;
  bool complete;
  void Start() {
   Session.Settings.ScrollSpeed=0;
   Session.Settings.SightCooldown=1;
   Session.Settings.SoundCooldown=1;
   PuzzleProgress.Begin(GetComponent<MatchSetupController>());
   Session.Registry.Players[0].Motor.Teleport(SpawnA,true);
   Session.Registry.Players[1].Motor.Teleport(SpawnB,true);
  }
  void Update(){if(complete&&LocalGamepads.ConfirmPressed)SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
  void LateUpdate() {
   var players=Session.Registry.Players;
   if(!complete) {
    bool atExit=true;
    foreach(var p in players) {
     if(p.Endpoint.y < -12) p.Motor.Teleport(p.Id==0?SpawnA:SpawnB,true);
     atExit &= p.CanAct && Mathf.Abs(p.Endpoint.x-Exit.position.x)<1.5f && Mathf.Abs(p.Endpoint.y-Exit.position.y)<2;
    }
    if(atExit) {complete=true;Session.FinishRound();}
   }
   Vector3 center=(players[0].Endpoint+players[1].Endpoint)*.5f;
   var camera=Session.World.Camera;
   camera.transform.position=new Vector3(center.x,Mathf.Max(-1,center.y),-10);
   camera.orthographicSize=Mathf.Max(7,Mathf.Abs(players[0].Endpoint.y-players[1].Endpoint.y)*.5f+3,Mathf.Abs(players[0].Endpoint.x-players[1].Endpoint.x)*.5f/camera.aspect+3);
  }
  void OnGUI() {
   GUI.Box(new Rect(10,10,Screen.width-20,104),"DIVINE SENSE — PUZZLE 1\nAngel: A/D move · W jump · S power   |   Devil: ←/→ move · ↑ jump · ↓ power\nPad: stick/D-pad move · bottom jump/glide · right power | Glide: hold to charge, release to dash up\nSight: purple radius reveals platforms   |   Sound: cyan radius breaks walls   |   Reach the exit together");
   GUI.Label(new Rect(20,176,Screen.width-40,24),LocalGamepads.Status);
   for(int i=0;i<2;i++) {
    var p=Session.Registry.Players[i];
    if(p.EquippedLoadout!=null) GUI.Label(new Rect(20,122+i*24,600,24),(i==0?"ANGEL: ":"DEVIL: ")+p.EquippedLoadout.ActiveKind+" / "+p.EquippedLoadout.MovementKind+"   "+(p.Abilities.Cooldown>0?p.Abilities.Cooldown.ToString("0.0")+"s":"READY"));
   }
   if(complete) {GUI.Box(new Rect(Screen.width/2-180,Screen.height/2-50,360,100),"PUZZLE COMPLETE — ANGEL AND DEVIL");if(GUI.Button(new Rect(Screen.width/2-100,Screen.height/2,200,36),"PLAY AGAIN"))SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
  }
 }
}
