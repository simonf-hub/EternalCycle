using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DivineSenseEditor
{
    public static class TimedCrumbleValidation
    {
        private const string PendingKey = "TimedCrumbleValidation.Pending";
        private const string SharedDelayKey = "TimedCrumbleValidation.SharedDelay";
        private const string SettingsPath = "Assets/DivineSense/Settings/ArenaSettings.asset";
        private const float ExpectedCrumbleDelay = .5f;
        private static GameSession session;
        private static PlayerContext rider;
        private static CrumblingPlatform platform;
        private static Collider2D support;
        private static SimulationMode2D originalSimulation;
        private static double started;
        private static float landedAt;
        private static int landingSignals;
        private static bool usingScriptSimulation, heldInitially;

        public static void Run()
        {
            SessionState.SetFloat(SharedDelayKey, AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath).CrumbleDelay);
            EditorSceneManager.OpenScene("Assets/Scenes/Level_1.unity");
            SessionState.SetBool(PendingKey, true);
            Resume();
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(PendingKey, false))
                return;

            started = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 30, "Timed crumble validation timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling)
                    return;

                if (!session)
                {
                    var arena = Object.FindAnyObjectByType<GameSession>();
                    if (arena && arena.IsPlaying)
                        Begin(arena);
                    return;
                }

                float elapsed = session.Now - landedAt;
                if (platform)
                {
                    Simulate();
                    Require(support.enabled && rider.Motor.Collider.IsTouching(support),
                        "The timed platform stopped supporting its rider before removal.");
                    heldInitially |= elapsed >= ExpectedCrumbleDelay * .25f;
                    Require(elapsed <= ExpectedCrumbleDelay + .2f, "The timed platform survived past its landing deadline.");
                    return;
                }

                Require(heldInitially && landingSignals == 1 && elapsed >= ExpectedCrumbleDelay - .01f &&
                    elapsed <= ExpectedCrumbleDelay + .2f,
                    $"The timed platform did not hold and then break after its deadline. Elapsed: {elapsed:F2}s.");
                float height = rider.Endpoint.y;
                for (int i = 0; i < 12; i++)
                    Simulate();
                Require(rider.Endpoint.y < height - .3f, "The timed platform still supported its rider after removal.");
                Finish();
            }
            catch (Exception error)
            {
                Finish(error);
            }
        }

        private static void Begin(GameSession arena)
        {
            session = arena;
            var sharedSettings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            Require(Mathf.Approximately(session.Settings.CrumbleDelay, ExpectedCrumbleDelay),
                $"Level_1 did not apply its {ExpectedCrumbleDelay} second crumble delay.");
            Require(session.Settings != sharedSettings && Mathf.Approximately(sharedSettings.CrumbleDelay,
                SessionState.GetFloat(SharedDelayKey, -1f)), "Level_1 changed the shared crumble delay.");

            session.Settings.ScrollSpeed = 0;
            Object.FindAnyObjectByType<GodDirector>().enabled = false;
            var spawner = session.GetComponent<PlatformSpawner>();
            spawner.enabled = false;
            foreach (var terrain in spawner.Root.GetComponentsInChildren<PlatformScroller>())
            {
                terrain.gameObject.SetActive(false);
                Object.Destroy(terrain.gameObject);
            }

            rider = session.Registry.Players[0];
            foreach (var player in session.Registry.Players)
            {
                player.Motor.enabled = false;
                player.Life.enabled = false;
                player.Motor.Body.interpolation = RigidbodyInterpolation2D.None;
            }
            session.Registry.Players[1].Motor.Body.simulated = false;
            originalSimulation = Physics2D.simulationMode;
            usingScriptSimulation = true;
            Physics2D.simulationMode = SimulationMode2D.Script;

            var instance = Object.Instantiate(session.Settings.CrumblePrefab);
            instance.transform.localScale = new Vector3(5.4f, .35f, 1f);
            platform = instance.GetComponent<CrumblingPlatform>();
            platform.Scroller.Session = session;
            support = instance.GetComponent<Collider2D>();
            platform.Started += () => landingSignals++;

            rider.Motor.Teleport(new Vector2(0, -1.6f), true);
            rider.Motor.Body.linearVelocity = Vector2.up * 15;
            StepUntil(() => rider.Motor.Collider.bounds.min.y > support.bounds.max.y + .1f,
                "A player could not pass upward through the one-way platform.");
            float upwardSpeed = rider.Motor.Body.linearVelocity.y;
            Require(upwardSpeed > 0f, "The one-way check did not reach clearance during ascent.");
            Require(landingSignals == 0, "Passing through from below started the crumble timer.");

            StepUntil(() => landingSignals == 1, "The top landing did not start the crumble timer.");
            Require(support.enabled && rider.Motor.Collider.IsTouching(support),
                "The platform must initially stay solid when the timer starts.");
            landedAt = session.Now;
        }

        private static void StepUntil(Func<bool> complete, string message)
        {
            for (int step = 0; step < 90; step++)
            {
                Simulate();
                if (complete())
                    return;
            }
            throw new InvalidOperationException(message);
        }

        private static void Simulate()
        {
            Physics2D.SyncTransforms();
            Physics2D.Simulate(Time.fixedDeltaTime);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Finish(Exception error = null)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(PendingKey, false);
            if (usingScriptSimulation)
                Physics2D.simulationMode = originalSimulation;
            if (error == null)
                Debug.Log($"TIMED_CRUMBLE_VALIDATION_OK: Level_1's {ExpectedCrumbleDelay}s delay, upward traversal, top landing, temporary support, and collapse");
            else
                Debug.LogException(error);
            EditorApplication.Exit(error == null ? 0 : 1);
        }
    }
}
