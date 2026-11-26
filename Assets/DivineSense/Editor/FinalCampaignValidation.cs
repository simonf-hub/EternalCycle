using System;
using System.Collections;
using System.Collections.Generic;
using DivineSense;
using EternalCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace DivineSenseEditor
{
    public static class FinalCampaignValidation
    {
        private const string PendingKey = "FinalCampaignValidation.Pending";

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool(PendingKey, false))
                EditorApplication.update += Attach;
        }

        public static void Run()
        {
            try
            {
                string[] expected =
                {
                    "Assets/Scenes/Level_0.unity",
                    "Assets/Scenes/Level_1.unity",
                    "Assets/Scenes/Level_2.unity"
                };
                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                Require(scenes.Length == expected.Length, "Build Settings must contain exactly three scenes.");
                for (int i = 0; i < expected.Length; i++)
                    Require(scenes[i].enabled && scenes[i].path == expected[i],
                        $"Build slot {i} must be enabled and use {expected[i]}.");

                EditorSceneManager.OpenScene(expected[0]);
                SessionState.SetBool(PendingKey, true);
                EditorApplication.update += Attach;
                EditorApplication.isPlaying = true;
            }
            catch (Exception error)
            {
                Exit(error);
            }
        }

        private static void Attach()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling)
                return;

            EditorApplication.update -= Attach;
            new GameObject("Final Campaign Validation").AddComponent<FinalCampaignValidationRunner>();
        }

        internal static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        internal static void Exit(Exception error = null)
        {
            SessionState.SetBool(PendingKey, false);
            if (error == null)
                Debug.Log("FINAL_CAMPAIGN_VALIDATION_OK: build order, controller guide, opening, A/left-stick/B controls, " +
                    "tutorial exit and gifts, Zeus transition, timed arena results, Athena transition, co-op gates and exit, replay.");
            else
                Debug.LogException(error);

            EditorApplication.Exit(error == null ? 0 : 1);
        }
    }

    public sealed class FinalCampaignValidationRunner : MonoBehaviour
    {
        private readonly List<InputDevice> disabledDevices = new List<InputDevice>();
        private InputSettings originalSettings, validationSettings;
        private Gamepad first, second;
        private double started;
        private string milestone = "controller guide";
        private bool finished;

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            started = EditorApplication.timeSinceStartup;
            IEnumerator steps = Verify();
            while (!finished)
            {
                object next = null;
                bool hasNext;
                try
                {
                    hasNext = steps.MoveNext();
                    if (hasNext)
                        next = steps.Current;
                }
                catch (Exception error)
                {
                    Finish(error);
                    break;
                }

                if (!hasNext)
                {
                    Finish();
                    break;
                }
                yield return next;
            }
            (steps as IDisposable)?.Dispose();
        }

        private void Update()
        {
            if (!finished && EditorApplication.timeSinceStartup - started > 30)
                Finish(new TimeoutException($"Campaign validation stalled at {milestone}."));
        }

        private IEnumerator Verify()
        {
            ConfigureInput();
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            TrialStory story = Resources.Load<TrialStory>("TrialStory");
            FinalCampaignValidation.Require(story, "The campaign could not load its editable TrialStory asset.");
            GameSession tutorial = CheckScene("Level_0", TrialLevel.Tutorial, playing: false);
            foreach (object step in DismissControllerGuide(tutorial, first))
                yield return step;

            milestone = "opening";
            foreach (object step in AdvanceStory(tutorial, story.Opening.Count, first))
                yield return step;
            CheckScene("Level_0", TrialLevel.Tutorial, playing: true);

            milestone = "tutorial controller controls";
            PlayerContext angel = tutorial.Registry.Players[0];
            PlayerContext devil = tutorial.Registry.Players[1];
            InputSystem.QueueStateEvent(first, new GamepadState().WithButton(GamepadButton.DpadRight));
            yield return null;
            yield return null;
            FinalCampaignValidation.Require(Mathf.Abs(angel.Input.Horizontal) < 0.01f,
                "D-pad movement must be ignored in the final campaign.");

            bool castSight = false;
            angel.Abilities.Activated += sense => castSight = sense == ActiveKind.Sight;
            InputSystem.QueueStateEvent(first,
                new GamepadState { leftStick = new Vector2(0.8f, 0) }.WithButton(GamepadButton.East));
            yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();
            FinalCampaignValidation.Require(angel.Input.Horizontal > 0.5f && Mathf.Abs(devil.Input.Horizontal) < 0.01f,
                "The first controller's left stick must move only Angel.");
            FinalCampaignValidation.Require(castSight, "The first controller's B button did not cast Sight.");
            InputSystem.QueueStateEvent(first, new GamepadState());
            yield return null;
            yield return null;

            milestone = "tutorial jump";
            yield return new WaitUntil(() => angel.Motor.Grounded);
            InputSystem.QueueStateEvent(first, new GamepadState().WithButton(GamepadButton.South));
            for (int step = 0; step < 8 && angel.Motor.Body.linearVelocity.y < 1f; step++)
                yield return new WaitForFixedUpdate();
            FinalCampaignValidation.Require(angel.Motor.Body.linearVelocity.y > 1f,
                "The first controller's A button did not jump from the tutorial floor.");
            InputSystem.QueueStateEvent(first, new GamepadState());

            milestone = "tutorial exit";
            foreach (object step in ClearCooperativeLayout(tutorial))
                yield return step;

            milestone = "Zeus transition";
            yield return new WaitForSecondsRealtime(0.55f);
            foreach (object step in AdvanceStory(tutorial, story.ZeusTransition.Count, second))
                yield return step;
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Level_1");
            yield return null;

            milestone = "timed arena results";
            GameSession arena = CheckScene("Level_1", TrialLevel.Arena, playing: true);
            CheckArenaObstacles(arena);
            FinalCampaignValidation.Require(arena.Settings.RoundDuration > 0, "The arena timer was not configured.");
            arena.Settings.RoundDuration = arena.Now + 0.1f;
            yield return new WaitUntil(() => arena.IsShowingResults);
            FinalCampaignValidation.Require(!arena.IsPlaying && !arena.IsShowingNarrative,
                "Arena results must stop gameplay before the next narrative.");
            yield return new WaitForSecondsRealtime(1.5f);
            foreach (object step in TapA(first))
                yield return step;
            yield return new WaitUntil(() => arena.IsShowingNarrative);
            FinalCampaignValidation.Require(!arena.IsShowingResults,
                "The results must close before Athena's transition.");

            milestone = "Athena transition";
            yield return new WaitForSecondsRealtime(0.55f);
            foreach (object step in AdvanceStory(arena, story.AthenaTransition.Count, second))
                yield return step;
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Level_2");
            yield return null;

            milestone = "cooperative completion";
            GameSession cooperative = CheckScene("Level_2", TrialLevel.Cooperative, playing: true);
            foreach (object step in ClearCooperativeLayout(cooperative))
                yield return step;
            FinalCampaignValidation.Require(cooperative.State == MatchState.Finished && !cooperative.IsShowingNarrative,
                "The last cooperative level should end the campaign.");
            foreach (object step in TapA(second))
                yield return step;

            milestone = "replay";
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Level_0");
            yield return null;
            GameSession replay = CheckScene("Level_0", TrialLevel.Tutorial, playing: false);
            foreach (object step in DismissControllerGuide(replay, second))
                yield return step;

            milestone = "optional tutorial gifts";
            foreach (object step in AdvanceStory(replay, story.Opening.Count, first))
                yield return step;
            foreach (object step in CollectTutorialGifts(replay))
                yield return step;
        }

        private static void CheckArenaObstacles(GameSession arena)
        {
            var spawner = arena.GetComponent<PlatformSpawner>();
            FinalCampaignValidation.Require(spawner && spawner.Root, "The arena is missing its platform generator.");
            var angel = arena.Registry.Players[0];
            var devil = arena.Registry.Players[1];
            Vector2 angelStart = angel.Endpoint;
            Vector2 devilStart = devil.Endpoint;

            var hidden = NearestFixture(spawner.Root.GetComponentsInChildren<MicroSense.HiddenPlatform>(), angelStart);
            var breakable = NearestFixture(spawner.Root.GetComponentsInChildren<MicroSense.BreakablePlatform>(), devilStart);
            FinalCampaignValidation.Require(hidden && breakable, "The arena generator must place both kinds of ability platform.");

            var reward = hidden.GetComponentInChildren<FollowerCollectible>(true);
            FinalCampaignValidation.Require(!hidden.IsRevealed && !hidden.IsSolid && reward &&
                !reward.gameObject.activeInHierarchy && reward.Session == arena,
                "A generated hidden platform must start non-solid with its follower unavailable.");
            Place(angel, (Vector2)hidden.transform.position + Vector2.up);
            Physics2D.SyncTransforms();
            angel.Abilities.Sight.Activate(angel);
            FinalCampaignValidation.Require(hidden.IsRevealed && hidden.IsSolid && reward.gameObject.activeInHierarchy,
                "Sight must reveal the generated platform and make its follower available.");

            var breakableCollider = breakable.GetComponent<Collider2D>();
            var effector = breakable.GetComponent<PlatformEffector2D>();
            var visual = breakable.GetComponent<SpriteRenderer>();
            FinalCampaignValidation.Require(!breakable.IsBroken && breakableCollider && breakableCollider.enabled &&
                !breakableCollider.isTrigger && breakableCollider.usedByEffector && effector && effector.enabled &&
                effector.useOneWay && visual && visual.enabled, "A generated Sound platform must start as a visible one-way landing.");
            Place(devil, (Vector2)breakable.transform.position + Vector2.up);
            Physics2D.SyncTransforms();
            devil.Abilities.Sound.Activate(devil);
            FinalCampaignValidation.Require(breakable.IsBroken && !breakableCollider.enabled,
                "Sound must remove the generated platform's support.");
            foreach (var sprite in breakable.GetComponentsInChildren<SpriteRenderer>(true))
                FinalCampaignValidation.Require(!sprite.enabled || !sprite.gameObject.activeInHierarchy,
                    "Sound must hide the generated platform's visuals and attached follower.");

            Place(angel, angelStart);
            Place(devil, devilStart);
        }

        private static T NearestFixture<T>(T[] fixtures, Vector2 origin) where T : Component
        {
            T nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var fixture in fixtures)
            {
                float candidate = ((Vector2)fixture.transform.position - origin).sqrMagnitude;
                if (candidate >= distance)
                    continue;
                nearest = fixture;
                distance = candidate;
            }
            return nearest;
        }

        private static IEnumerable<object> ClearCooperativeLayout(GameSession session)
        {
            var layout = session.GetComponent<CooperativeLevelController>();
            var first = session.Registry.Players[0];
            var second = session.Registry.Players[1];
            string sceneName = SceneManager.GetActiveScene().name;
            bool tutorial = session.Trial == TrialLevel.Tutorial;
            FinalCampaignValidation.Require(layout && layout.Exit && layout.Checkpoints.Length > 0,
                $"The {sceneName} layout is missing its exit or recovery points.");

            Bounds exitBounds = layout.Exit.bounds;
            Vector2 exit = exitBounds.center;
            if (!tutorial)
            {
                FinalCampaignValidation.Require(layout.RequiredReveals.Length > 0 && layout.RequiredBreaks.Length > 0 &&
                    layout.RequiredGates.Length == 2, "The final cooperative level is missing its required objectives.");
                Place(first, exit + Vector2.left * .55f);
                Place(second, exit + Vector2.right * .55f);
                yield return null;
                yield return null;
                FinalCampaignValidation.Require(session.IsPlaying,
                    $"The {sceneName} exit advanced before its objectives were cleared.");
            }

            var checkpoint = layout.Checkpoints[0];
            Place(first, checkpoint.AngelSpawn.position);
            Place(second, checkpoint.DevilSpawn.position);
            yield return null;
            Place(first, new Vector2(checkpoint.AngelSpawn.position.x, layout.FallLimit.position.y - .5f));
            yield return null;
            yield return null;
            FinalCampaignValidation.Require(Vector2.Distance(first.Endpoint, checkpoint.AngelSpawn.position) < .4f,
                $"A fall in {sceneName} did not return the player to their checkpoint.");

            foreach (var platform in layout.RequiredReveals)
            {
                Place(first, (Vector2)platform.transform.position + Vector2.up);
                first.Abilities.Sight.Activate(first);
                FinalCampaignValidation.Require(platform.IsRevealed && platform.IsSolid,
                    $"Sight did not reveal {platform.name} in {sceneName}.");
            }

            foreach (var wall in layout.RequiredBreaks)
            {
                Place(second, (Vector2)wall.transform.position + Vector2.left * 1.5f);
                second.Abilities.Sound.Activate(second);
                FinalCampaignValidation.Require(wall.IsBroken, $"Sound did not break {wall.name} in {sceneName}.");
            }

            foreach (var gate in layout.RequiredGates)
            {
                Place(first, (Vector2)gate.LowerPlate.Zone.bounds.center + Vector2.up * .17f);
                Place(second, layout.DevilStart.position);
                yield return null;
                yield return null;
                FinalCampaignValidation.Require(!gate.IsOpen && gate.Barrier.enabled,
                    $"{gate.name} opened while only one pressure plate was occupied.");

                Place(second, (Vector2)gate.UpperPlate.Zone.bounds.center + Vector2.up * .17f);
                yield return null;
                yield return null;
                FinalCampaignValidation.Require(gate.IsOpen && !gate.Barrier.enabled,
                    $"{gate.name} did not open with a different mortal on each plate.");
            }

            Vector2 firstExitContact = new Vector2(exitBounds.min.x - .15f, exit.y);
            Vector2 secondExitContact = new Vector2(exitBounds.max.x + .15f, exit.y);
            Place(first, firstExitContact);
            Place(second, layout.DevilStart.position);
            yield return null;
            FinalCampaignValidation.Require(session.IsPlaying, $"The {sceneName} exit advanced with only one mortal.");

            Place(second, secondExitContact);
            FinalCampaignValidation.Require(!layout.Exit.OverlapPoint(first.Endpoint) &&
                !layout.Exit.OverlapPoint(second.Endpoint) && layout.Exit.Distance(first.Motor.Collider).isOverlapped &&
                layout.Exit.Distance(second.Motor.Collider).isOverlapped,
                $"Both mortals must overlap the {sceneName} exit with their centers outside it for this check.");
            yield return null;
            yield return null;
            FinalCampaignValidation.Require(session.State == MatchState.Finished,
                $"The {sceneName} exit did not advance when both mortals touched it.");

            if (tutorial)
            {
                var bridge = FindAnyObjectByType<MicroSense.HiddenPlatform>();
                var wall = FindAnyObjectByType<MicroSense.BreakablePlatform>();
                FinalCampaignValidation.Require(bridge && !bridge.IsRevealed && wall && !wall.IsBroken &&
                    FindObjectsByType<GodPickup>().Length == 4,
                    "The tutorial must finish with the bridge hidden, the wall intact, and all four gifts available.");
            }
        }

        private static IEnumerable<object> CollectTutorialGifts(GameSession session)
        {
            var first = session.Registry.Players[0];
            var second = session.Registry.Players[1];
            foreach (var gift in FindObjectsByType<GodPickup>())
            {
                string giftName = GodPickup.DisplayName(gift.Kind);
                Place(second, (Vector2)gift.transform.position + Vector2.left * 2f);
                Place(first, gift.transform.position);
                for (int step = 0; step < 6 && gift; step++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }
                FinalCampaignValidation.Require(!gift, $"The tutorial gift {giftName} did not collect on contact.");
            }
        }

        private static void Place(PlayerContext player, Vector2 position) =>
            player.Motor.Teleport(position, true);

        private void ConfigureInput()
        {
            originalSettings = InputSystem.settings;
            validationSettings = Instantiate(originalSettings);
            validationSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            validationSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            validationSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings = validationSettings;

            foreach (InputDevice device in InputSystem.devices)
            {
                if (device.enabled && (device is Gamepad || device is Joystick))
                    disabledDevices.Add(device);
            }
            foreach (InputDevice device in disabledDevices)
                InputSystem.DisableDevice(device);

            first = InputSystem.AddDevice<Gamepad>();
            second = InputSystem.AddDevice<Gamepad>();
            FinalCampaignValidation.Require(LocalGamepads.ForPlayer(0)?.Device == first &&
                LocalGamepads.ForPlayer(1)?.Device == second, "Virtual controller assignment failed.");
        }

        private static GameSession CheckScene(string sceneName, TrialLevel trial, bool playing)
        {
            GameSession session = FindAnyObjectByType<GameSession>();
            FinalCampaignValidation.Require(SceneManager.GetActiveScene().name == sceneName && session &&
                session.Trial == trial && session.IsPuzzle == (trial != TrialLevel.Arena),
                $"Scene {sceneName} did not load its configured campaign level.");
            FinalCampaignValidation.Require(session.IsPlaying == playing, $"Unexpected gameplay state in {sceneName}.");
            if (playing)
                FinalCampaignValidation.Require(session.Registry.Players[0].HomeLoadout.ActiveKind == ActiveKind.Sight &&
                    session.Registry.Players[1].HomeLoadout.ActiveKind == ActiveKind.Sound,
                    $"The campaign powers changed in {sceneName}.");
            return session;
        }

        private static IEnumerable<object> DismissControllerGuide(GameSession session, Gamepad pad)
        {
            FinalCampaignValidation.Require(session.IsShowingControllerGuide && !session.IsShowingNarrative &&
                !session.IsPlaying, "The controller guide must appear before the opening narration.");
            foreach (object step in TapA(pad))
                yield return step;
            FinalCampaignValidation.Require(!session.IsShowingControllerGuide && session.IsShowingNarrative &&
                !session.IsPlaying, "A must close the controller guide and start the opening narration.");
        }

        private IEnumerable<object> AdvanceStory(GameSession session, int pageCount, Gamepad pad)
        {
            FinalCampaignValidation.Require(pageCount > 0, $"No narrative pages configured for {milestone}.");
            for (int i = 0; i < pageCount; i++)
            {
                FinalCampaignValidation.Require(session && session.IsShowingNarrative,
                    $"Narrative ended before page {i + 1} at {milestone}.");
                foreach (object step in TapA(pad))
                    yield return step;
            }
            FinalCampaignValidation.Require(!session || !session.IsShowingNarrative,
                $"A did not finish the narrative at {milestone}.");
        }

        private static IEnumerable<object> TapA(Gamepad pad)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
            yield return null;
            yield return null;
            FinalCampaignValidation.Require(pad.buttonSouth.isPressed, "The virtual pad did not receive A.");
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
            yield return null;
        }

        private void Finish(Exception error = null)
        {
            finished = true;
            if (first != null && first.added)
                InputSystem.RemoveDevice(first);
            if (second != null && second.added)
                InputSystem.RemoveDevice(second);
            foreach (InputDevice device in disabledDevices)
            {
                if (device.added)
                    InputSystem.EnableDevice(device);
            }
            if (validationSettings)
            {
                InputSystem.settings = originalSettings;
                Destroy(validationSettings);
            }
            FinalCampaignValidation.Exit(error);
        }
    }
}
