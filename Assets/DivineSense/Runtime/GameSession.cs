using System;
using EternalCycle;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DivineSense {
[DefaultExecutionOrder(-200)] public sealed class GameSession : MonoBehaviour {
 public GameSettings Settings; public PlayerRegistry Registry; public SceneModeController World;
 public AbilitySwapEffect Swap; public MatchState State {get;private set;}
 public float Now {get;private set;} public string Result {get;private set;}
 public bool IsPuzzle => SceneManager.GetActiveScene().name.StartsWith("DivineSense_Puzzle_");
 public bool IsPlaying => State==MatchState.Playing;
 private NarrativeSequence narrative;
 private ArenaResults results;
 public bool IsShowingResults => results != null && results.enabled;
 public bool IsShowingNarrative => narrative != null && narrative.enabled;
 internal void ShowNarrative(bool afterArena, Action complete, ArenaTutorial tutorial=null) {
 if(IsShowingNarrative)return;
 narrative=gameObject.AddComponent<NarrativeSequence>();
 narrative.Play(afterArena,complete,tutorial);
 }
 public event Action<string> Finished;
 void Awake() { Settings=Instantiate(Settings); }
 public void StartRound() { Now=0; State=MatchState.Playing; }
 void Update() { if(!IsPlaying)return; Now=IsPuzzle?Now+Time.deltaTime:Mathf.Min(Now+Time.deltaTime,Settings.RoundDuration); if(!IsPuzzle && Now>=Settings.RoundDuration) FinishRound(); }
 public void FinishRound() {
 if(!IsPlaying)return; State=MatchState.Finished;
 int a=Registry.Players[0].Score.Total,b=Registry.Players[1].Score.Total;
 Result=a==b?"DRAW — THE GODS DEMAND A REMATCH":LocalGamepads.ContestantName(a>b?0:1)+" WINS THE GODS' APPROVAL";
 if(SceneManager.GetActiveScene().name=="DivineSense_Arena") {
 PuzzleProgress.Capture(this);
 results=gameObject.AddComponent<ArenaResults>();
 results.Play(this,()=>ShowNarrative(true,()=>SceneManager.LoadScene("DivineSense_Puzzle_1")));
 }
 Swap.Restore(); foreach(var p in Registry.Players) { p.Status.Clear(); p.Motor.Freeze(); }
 Finished?.Invoke(Result);
 }
} }
