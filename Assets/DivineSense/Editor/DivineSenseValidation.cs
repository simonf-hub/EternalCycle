using System;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DivineSenseEditor
{
    public static class DivineSenseValidation
    {
        private static GameSession session;
        private static double start;
        private static float phaseStartedAt;
        private static float deathAt;
        private static Vector2 normalColliderSize;
        private static int stage;
        private static int gods;
        private static int flips;
        private static GodKind? lastGod;
        private static bool failed;
        private static bool respawnChecked;

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool("DivineValidation", false))
                return;

            start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            SessionState.SetBool("DivineValidation", true);
            Check(FollowerCounter.AfterDeath(1000, .2f) == 800, "score 1000 -> 800");
            Check(FollowerCounter.AfterDeath(1, .2f) == 0, "rounded score 1 -> 0");
            for (int previous = 0; previous < 4; previous++)
                for (int roll = 0; roll < 3; roll++)
                    Check(GodDirector.ExcludingPrevious(roll, previous) != previous, "god exclusion");

            EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");
            start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - start > 100)
                    throw new Exception("Validation timed out");

                if (!EditorApplication.isPlaying || EditorApplication.isCompiling)
                    return;

                if (stage == 0)
                {
                    session = Object.FindFirstObjectByType<GameSession>();
                    if (!session || EditorApplication.timeSinceStartup - start < 12)
                        return;
                    BeginPowerChecks();
                    stage = 1;
                    return;
                }

                float elapsed = session.Now - phaseStartedAt;
                switch (stage)
                {
                    case 1 when elapsed > .1f:
                        CheckZeusAndBeginHera();
                        stage = 2;
                        return;
                    case 2 when elapsed > .1f:
                        CheckHeraAndBeginAres();
                        stage = 3;
                        return;
                    case 3 when elapsed > .5f:
                        CheckAresAndBeginArenaSweep();
                        stage = 4;
                        return;
                    case 4:
                        CheckArenaSweep();
                        return;
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Exit(1);
            }
        }

        private static void BeginPowerChecks()
        {
            Object.FindFirstObjectByType<MatchSetupController>()
                .Begin(ActiveKind.Sight, MovementKind.Touch, ActiveKind.Sound, MovementKind.Taste);
            var first = session.Registry.Players[0];
            var second = session.Registry.Players[1];
            normalColliderSize = second.Motor.Collider.size;
            first.Motor.Teleport(new Vector2(-1, -3), true);
            second.Motor.Teleport(new Vector2(1, -3), true);
            first.Abilities.TryActivateActive();
            Check(second.Status.Hidden && Mathf.Approximately(second.Status.HiddenUntil - session.Now, 1.5f)
                && Mathf.Approximately(first.Abilities.Cooldown, session.Settings.SightCooldown), "Sight duration and cooldown");

            second.Status.HiddenUntil = 0;
            CollectGod(GodKind.Zeus, second);
            Check(second.Status.Grown && Mathf.Approximately(second.Status.GrownUntil - session.Now, 3f),
                "Zeus gives exactly three seconds of Agility");
            Check(Mathf.Approximately(session.Settings.GrowthSlowdown, .65f), "Zeus sets 65% walk speed");
            float grownJump = second.Motor.DoubleJump.JumpVelocity(second, false);
            float jumpHeightRatio = Mathf.Pow(grownJump / session.Settings.JumpSpeed, 2);
            Check(Mathf.Approximately(jumpHeightRatio, 1.5f), "Zeus raises jump height by 50%");

            first.Abilities.Sight.Activate(first);
            Check(!second.Status.Hidden, "Zeus blocks Sight");
            first.Abilities.Sound.Activate(first);
            phaseStartedAt = session.Now;
        }

        private static void CheckZeusAndBeginHera()
        {
            var second = session.Registry.Players[1];
            Check(Vector2.Distance(second.Motor.Collider.size, normalColliderSize * 1.5f) < .01f,
                "Zeus enlarges the player's collider by 1.5×");
            Check(Mathf.Abs(second.Motor.Body.linearVelocity.x) < .01f, "Zeus blocks Sound knockback");
            CollectGod(GodKind.Hera, session.Registry.Players[0]);
            phaseStartedAt = session.Now;
        }

        private static void CheckHeraAndBeginAres()
        {
            var first = session.Registry.Players[0];
            var second = session.Registry.Players[1];
            Check(second.Motor.Body.linearVelocity.x < -1f, "Hera pulls the opponent toward the collector");
            first.Motor.Teleport(new Vector2(-.65f, -3), true);
            second.Motor.Teleport(new Vector2(.65f, -3), true);
            CollectGod(GodKind.Ares, first);
            Check(Object.FindFirstObjectByType<HomingBomb>(), "Ares immediately launches a homing projectile");
            phaseStartedAt = session.Now;
        }

        private static void CheckAresAndBeginArenaSweep()
        {
            var first = session.Registry.Players[0];
            var second = session.Registry.Players[1];
            Check(second.Status.Frozen, "Ares projectile reaches and freezes its target");
            second.Status.Clear();

            first.Score.Add(1000);
            first.Life.TryDie();
            first.Life.TryDie();
            deathAt = session.Now;
            Check(first.Score.Total == 800, "idempotent death");
            Vector2 marker = first.Endpoint;
            Vector2 body = second.Endpoint;
            CollectGod(GodKind.Athena, second);
            Check(Vector2.Distance(first.Endpoint, body) < .01f && Vector2.Distance(second.Endpoint, marker) < .01f,
                "Athena swaps body and respawn marker");

            first.HomeLoadout.ActiveReadyAt = 12;
            second.HomeLoadout.ActiveReadyAt = 4;
            session.Swap.Apply();
            Check(first.EquippedLoadout == second.HomeLoadout
                && Mathf.Approximately(first.Abilities.Cooldown, 4 - session.Now), "bundle reference and cooldown transfer");
            first.EquippedLoadout.ActiveReadyAt = 20;
            session.Swap.Restore();
            Check(second.EquippedLoadout.ActiveReadyAt == 20, "used cooldown returns home");

            var world = session.World;
            var platform = Object.FindFirstObjectByType<PlatformScroller>();
            Vector3 before = platform.transform.position;
            float velocity = world.ScrollVelocity;
            world.ToggleMode();
            Check(world.ScrollVelocity == -velocity && platform.transform.position == before,
                "mode reversal preserves layout");
            world.ModeChanged += _ =>
            {
                flips++;
                if (session.Now < flips * session.Settings.SceneFlipInterval
                    || session.Now > flips * session.Settings.SceneFlipInterval + .5f)
                    failed = true;
            };

            var director = Object.FindFirstObjectByType<GodDirector>();
            director.Intervened += kind =>
            {
                if (kind == lastGod || kind is not (GodKind.Athena or GodKind.Zeus or GodKind.Hera or GodKind.Ares))
                    failed = true;
                lastGod = kind;
                gods++;
                if (director.PickupsRoot.GetComponentsInChildren<GodPickup>().Length < session.Settings.GodPickupCount)
                    failed = true;
            };

            Time.timeScale = 30;
            Debug.Log("DIVINE_INITIAL_CHECKS_OK: Sight, Zeus growth and immunity, Hera pull, Ares hit and Athena marker swap");
        }

        private static void CheckArenaSweep()
        {
            var first = session.Registry.Players[0];
            if (session.Now >= deathAt + 3.2f && !respawnChecked)
            {
                Check(first.Life.State == LifeState.Alive, "automatic respawn");
                respawnChecked = true;
            }
            if (session.State != MatchState.Finished)
                return;

            Check(session.Now == 180, "exact round deadline");
            Check(gods == 35 && !failed, "35 nonconsecutive canonical god power-up drops before expiry");
            Check(flips == 11, "eleven automatic 16-second flips, none at round end");
            int score = first.Score.Total;
            first.Score.Add(100);
            first.Life.TryDie();
            Check(score == first.Score.Total, "finished score frozen");
            Debug.Log("DIVINE_VALIDATION_OK: four gods, ability timings, respawn, 180s match and frozen result");
            Exit(0);
        }

        private static void CollectGod(GodKind kind, PlayerContext collector)
        {
            GameObject gift = Object.Instantiate(session.Settings.PickupPrefabFor(kind), collector.Endpoint, Quaternion.identity);
            var pickup = gift.GetComponent<GodPickup>();
            Check(pickup && pickup.Kind == kind, $"prefab is configured for {kind}");
            pickup.SendMessage("OnTriggerEnter2D", collector.Motor.Collider);
        }

        private static void Check(bool okay, string label)
        {
            if (!okay)
                throw new Exception("VALIDATION FAILED: " + label);
        }

        private static void Exit(int code)
        {
            Time.timeScale = 1;
            EditorApplication.update -= Tick;
            SessionState.SetBool("DivineValidation", false);
            EditorApplication.Exit(code);
        }
    }
}
