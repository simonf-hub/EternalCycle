using System;
using UnityEngine;
namespace DivineSense {
[DefaultExecutionOrder(-200)] public sealed class GameSession : MonoBehaviour {
 public GameSettings Settings; public PlayerRegistry Registry; public SceneModeController World;
 public AbilitySwapEffect Swap; public MatchState State {get;private set;}
 public float Now {get;private set;} public string Result {get;private set;}
 public bool IsPlaying => State==MatchState.Playing;
 public event Action<string> Finished;
 void Awake() { Settings=Instantiate(Settings); }
 public void StartRound() { Now=0; State=MatchState.Playing; }
 void Update() { if(!IsPlaying)return; Now=Mathf.Min(Now+Time.deltaTime,Settings.RoundDuration); if(Now>=Settings.RoundDuration) FinishRound(); }
 public void FinishRound() {
 if(!IsPlaying)return; State=MatchState.Finished;
 int a=Registry.Players[0].Score.Total,b=Registry.Players[1].Score.Total;
 Result=a==b?"DRAW — THE GODS DEMAND A REMATCH":(a>b?"ANGEL":"DEVIL")+" WINS THE GODS' APPROVAL";
 Swap.Restore(); foreach(var p in Registry.Players) { p.Status.Clear(); p.Motor.Freeze(); }
 Finished?.Invoke(Result);
 }
} }