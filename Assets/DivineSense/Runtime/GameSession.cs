using System;
using System.Collections.Generic;
using EternalCycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DivineSense
{
    public enum TrialLevel { Prototype, Tutorial, Arena, Cooperative }

    [DefaultExecutionOrder(-200)]
    public sealed class GameSession : MonoBehaviour
    {
        private const string TutorialScene = "Level_0";
        private const string ArenaScene = "Level_1";
        private const string CooperativeScene = "Level_2";

        public TrialLevel Trial;
        public GameSettings Settings;
        public PlayerRegistry Registry;
        public SceneModeController World;
        public AbilitySwapEffect Swap;
        [SerializeField, Min(0f), Tooltip("Seconds from landing to crumble. Zero uses the shared settings.")]
        private float crumbleDelayOverride;

        public MatchState State { get; private set; }
        public bool IsPlaying => State == MatchState.Playing;
        public bool IsPuzzle => Trial == TrialLevel.Tutorial || Trial == TrialLevel.Cooperative
            || SceneManager.GetActiveScene().name.StartsWith("DivineSense_Puzzle_", StringComparison.Ordinal);
        public float Now { get; private set; }
        public string Result { get; private set; }
        public bool IsShowingResults => results != null && results.enabled;
        public bool IsShowingControllerGuide => narrative != null && narrative.enabled && narrative.IsShowingControllerGuide;
        public bool IsShowingNarrative => narrative != null && narrative.enabled && !narrative.IsShowingControllerGuide;
        public event Action<string> Finished;

        private TrialStory story;
        private NarrativeSequence narrative;
        private ArenaResults results;
        private TrialContinueInput completionInput;

        private void Awake()
        {
            Settings = Instantiate(Settings);
            if (crumbleDelayOverride > 0f)
                Settings.CrumbleDelay = crumbleDelayOverride;
            if (Trial != TrialLevel.Prototype)
                story = Resources.Load<TrialStory>("TrialStory");
        }

        private void Start()
        {
            if (Trial == TrialLevel.Arena)
                GetComponent<MatchSetupController>().BeginTrial();
        }

        private void Update()
        {
            if (IsPlaying)
            {
                Now = IsPuzzle ? Now + Time.deltaTime : Mathf.Min(Now + Time.deltaTime, Settings.RoundDuration);
                if (!IsPuzzle && Now >= Settings.RoundDuration)
                    FinishRound();
            }
            else if (Trial == TrialLevel.Cooperative && State == MatchState.Finished && completionInput.Pressed())
            {
                SceneManager.LoadScene(TutorialScene);
            }
        }

        public void StartRound()
        {
            Now = 0;
            State = MatchState.Playing;
        }

        public void BeginCooperativeLevel(Action start)
        {
            if (Trial == TrialLevel.Tutorial)
                Present(story?.Opening, start, controllerGuide: Resources.Load<Texture2D>("Controller-Guide"));
            else
                start();
        }

        public void CompleteCooperativeLevel()
        {
            if (!IsPlaying)
                return;

            Result = Trial == TrialLevel.Tutorial ? "Tutorial complete" : "Trials complete";
            EndPlay();
            if (Trial == TrialLevel.Tutorial)
                Present(story?.ZeusTransition, () => SceneManager.LoadScene(ArenaScene), fadeIn: true);
            else
                completionInput.Reset();
        }

        public void FinishRound()
        {
            if (!IsPlaying)
                return;

            int angel = Registry.Players[0].Score.Total;
            int devil = Registry.Players[1].Score.Total;
            Result = angel == devil ? "DRAW — THE GODS DEMAND A REMATCH"
                : LocalGamepads.ContestantName(angel > devil ? 0 : 1) + " WINS THE GODS' APPROVAL";

            if (Trial == TrialLevel.Arena)
            {
                results = gameObject.AddComponent<ArenaResults>();
                results.Play(this, () => Present(story?.AthenaTransition,
                    () => SceneManager.LoadScene(CooperativeScene), fadeIn: true));
            }
            else if (SceneManager.GetActiveScene().name == "DivineSense_Arena")
            {
                PuzzleProgress.Capture(this);
                results = gameObject.AddComponent<ArenaResults>();
                results.Play(this, () => ShowNarrative(true, () =>
                {
                    const string prototypePuzzle = "Assets/Scenes/Archive/DivineSense_Puzzle_1.unity";
                    if (SceneUtility.GetBuildIndexByScenePath(prototypePuzzle) >= 0)
                        SceneManager.LoadScene(prototypePuzzle);
                    else
                        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
                }));
            }

            EndPlay();
        }

        private void EndPlay()
        {
            State = MatchState.Finished;
            if (Swap)
                Swap.Restore();

            foreach (PlayerContext player in Registry.Players)
            {
                player.Status.Clear();
                player.Input.DiscardActions();
                player.Motor.Freeze();
            }

            Finished?.Invoke(Result);
        }

        private void Present(IReadOnlyList<NarrativePage> pages, Action complete, bool fadeIn = false,
            Texture2D controllerGuide = null)
        {
            if (IsShowingNarrative || IsShowingControllerGuide)
                return;

            if (pages == null || pages.Count == 0)
            {
                Debug.LogError($"No story pages configured for {Trial}.", this);
                complete();
                return;
            }

            narrative = gameObject.AddComponent<NarrativeSequence>();
            narrative.Play(pages, complete, fadeIn, controllerGuide: controllerGuide);
        }

        internal void ShowNarrative(bool afterArena, Action complete, ArenaTutorial tutorial = null)
        {
            if (IsShowingNarrative || IsShowingControllerGuide)
                return;

            narrative = gameObject.AddComponent<NarrativeSequence>();
            narrative.Play(afterArena, complete, tutorial);
        }
    }
}
