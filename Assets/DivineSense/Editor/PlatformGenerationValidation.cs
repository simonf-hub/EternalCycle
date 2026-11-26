using System;
using System.Collections.Generic;
using DivineSense;
using MicroSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DivineSenseEditor {
 public static class PlatformGenerationValidation {
  static double started; static bool begun; static int checks; static int stoppedCount=-1;
  [InitializeOnLoadMethod] static void Resume(){
   if(SessionState.GetBool("PlatformValidation",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
   if(SessionState.GetBool(LevelOnePendingKey,false)){started=EditorApplication.timeSinceStartup;EditorApplication.update-=TickLevelOne;EditorApplication.update+=TickLevelOne;}
  }
  public static void Run(){SessionState.SetBool("PlatformValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");Resume();EditorApplication.isPlaying=true;}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  static void Tick(){try{
   if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Platform validation timed out");
   if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   var session=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!session)return;
   if(!begun){
    UnityEngine.Object.FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);
    session.Settings.CrumbleChance=0; Time.timeScale=20;begun=true;
   }
   var spawner=UnityEngine.Object.FindFirstObjectByType<PlatformSpawner>();
   var platforms=spawner.Root.GetComponentsInChildren<PlatformScroller>();
   Physics2D.SyncTransforms();
   for(int i=0;i<platforms.Length;i++)for(int j=i+1;j<platforms.Length;j++){
    var a=platforms[i].GetComponent<Collider2D>().bounds;var b=platforms[j].GetComponent<Collider2D>().bounds;
    float horizontal=Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x);
    float vertical=Mathf.Max(a.min.y-b.max.y,b.min.y-a.max.y);
    Check(horizontal>=.24f||vertical>=1.09f,"Platforms are stacked too closely: "+platforms[i].name+" / "+platforms[j].name+" gaps "+horizontal+", "+vertical);
   }
   Check(platforms.Length<90,"Platform population grew without bounds"); checks++;
   // Exercise zero scroll, maximum HUD scroll, and repeated automatic reversals.
   if(session.Now>20&&session.Now<25){session.Settings.ScrollSpeed=0;if(session.Now>21){if(stoppedCount<0)stoppedCount=platforms.Length;Check(platforms.Length==stoppedCount,"Platforms accumulated while scrolling was stopped");}}
   else session.Settings.ScrollSpeed=session.Now<25?.8f:5f;
   if(session.Now>155){Check(checks>100,"Too few simulation checks");Debug.Log("PLATFORM_GENERATION_VALIDATION_OK: initial dais clearance, bounded population, zero scroll, fast scroll, repeated direction flips; "+checks+" simulation samples");Time.timeScale=1;SessionState.SetBool("PlatformValidation",false);EditorApplication.Exit(0);}
  }catch(Exception error){Debug.LogException(error);SessionState.SetBool("PlatformValidation",false);EditorApplication.Exit(1);}}

        private const string LevelOnePendingKey = "PlatformGenerationValidation.LevelOne";
        private const string LevelOne = "Level_1";
        private static readonly (int seed, float aspect)[] ArenaCases =
        {
            (461, 16f / 9f), (1203, 4f / 3f), (9322, 16f / 9f)
        };

        private static GameSession arena;
        private static PlatformSpawner arenaSpawner;
        private static int arenaCase;
        private static bool loading;
        private static int openingHiddenCount, openingSoundCount;
        private static int openingTimedCount;
        private static int timedCount, outerCount;
        private static int directionsWithNewFixtures;
        private static int stationaryCount = -1;
        private static readonly HashSet<HiddenPlatform> SeenHidden = new HashSet<HiddenPlatform>();
        private static readonly HashSet<BreakablePlatform> SeenSoundDecks = new HashSet<BreakablePlatform>();
        private static readonly HashSet<PlatformScroller> SeenSupports = new HashSet<PlatformScroller>();
        private static PlatformScroller openingPlatform;
        private static Vector2 firstOpeningPlatformPosition;

        public static void RunLevelOne()
        {
            // Loading from an empty Play Mode scene lets each case seed Random before the spawner's Start.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(LevelOnePendingKey, true);
            Resume();
            EditorApplication.isPlaying = true;
        }

        private static void PrepareArena(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LevelOne)
                return;

            SceneManager.sceneLoaded -= PrepareArena;
            var (seed, aspect) = ArenaCases[arenaCase];
            UnityEngine.Random.InitState(seed);
            arena = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            arena.World.Camera.aspect = aspect;
            arenaSpawner = UnityEngine.Object.FindAnyObjectByType<PlatformSpawner>();
            UnityEngine.Object.FindAnyObjectByType<GodDirector>().enabled = false;
        }

        private static void TickLevelOne()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup - started < 100, "Level_1 platform validation timed out");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling)
                    return;

                if (!loading && !arena)
                {
                    loading = true;
                    SceneManager.sceneLoaded += PrepareArena;
                    SceneManager.LoadScene(LevelOne);
                    return;
                }
                if (!arena || !arena.IsPlaying)
                    return;

                PlatformScroller[] platforms = arenaSpawner.Root.GetComponentsInChildren<PlatformScroller>();
                Physics2D.SyncTransforms();
                bool opening = loading;
                if (opening)
                {
                    loading = false;
                    SeenHidden.Clear();
                    SeenSoundDecks.Clear();
                    SeenSupports.Clear();
                    timedCount = outerCount = 0;
                    stationaryCount = -1;
                    directionsWithNewFixtures = 0;
                    foreach (PlayerContext player in arena.Registry.Players)
                    {
                        player.Motor.enabled = false;
                        player.Motor.Body.simulated = false;
                        player.Life.enabled = false;
                    }
                    Time.timeScale = 15;
                }

                ValidateArenaPlatforms(platforms, opening);
                if (opening)
                {
                    openingHiddenCount = SeenHidden.Count;
                    openingSoundCount = SeenSoundDecks.Count;
                    openingTimedCount = timedCount;
                    Check(openingHiddenCount > 0 && openingSoundCount > 0,
                        "The opening layout is missing a Sight bridge or Sound-breakable landing");
                    Check(timedCount * 2 > SeenSupports.Count,
                        "Timed platforms must outnumber steady platforms in the opening layout");
                    Check(Mathf.Approximately(arena.Settings.CrumbleDelay, .5f),
                        "Level_1 timed platforms must crumble 0.5 seconds after landing");
                }

                if (arena.Now >= 5f && arena.Now < 8f)
                {
                    arena.Settings.ScrollSpeed = 0;
                    if (arena.Now >= 6f)
                    {
                        if (stationaryCount < 0)
                            stationaryCount = platforms.Length;
                        Check(platforms.Length == stationaryCount, "Level_1 spawns platforms while scrolling is stopped");
                    }
                }
                else if (arena.Now >= 8f)
                    arena.Settings.ScrollSpeed = 4f;

                if (arena.Now < 50f)
                    return;

                Check(!openingPlatform, "The opening platform did not cull during scrolling");
                Check(stationaryCount > 0, "Stationary platform check did not run");
                Check(SeenHidden.Count > openingHiddenCount && SeenSoundDecks.Count > openingSoundCount,
                    "Sight bridges and Sound-breakable landings must recur in new rows");
                Check(directionsWithNewFixtures == 3, "New obstacles must spawn with either scroll direction");
                Check(timedCount > openingTimedCount, "Timed orange landings must recur in new rows");
                // At 16:9, the outer pads sit beyond the range of ordinary connectors.
                if (ArenaCases[arenaCase].aspect > 1.5f)
                {
                    int steadyCount = SeenSupports.Count - timedCount;
                    Check(outerCount < steadyCount * 1.9f, "The outer routes need occasional gaps");
                }
                Debug.Log($"Level_1 seed {ArenaCases[arenaCase].seed}: {SeenHidden.Count} hidden, {SeenSoundDecks.Count} Sound-breakable, {timedCount} timed platforms");
                if (++arenaCase == ArenaCases.Length)
                    ExitLevelOne();
                else
                    arena = null;
            }
            catch (Exception error)
            {
                ExitLevelOne(error);
            }
        }

        private static void ValidateLandingRoutes(List<(Bounds bounds, PlatformScroller platform)> landings, bool opening)
        {
            Rect visible = arena.World.VisibleBounds;
            var steady = new List<Bounds>();
            var soundBranches = new List<Bounds>();
            PlatformScroller outerLeft = null;
            bool hasOuterRight = false;
            foreach (var (bounds, platform) in landings)
            {
                if (platform.GetComponent<HiddenPlatform>())
                    continue;
                if (platform.GetComponent<BreakablePlatform>())
                {
                    soundBranches.Add(bounds);
                    continue;
                }

                bool firstObservation = SeenSupports.Add(platform);
                if (platform.TryGetComponent<CrumblingPlatform>(out var temporary))
                {
                    if (firstObservation)
                    {
                        timedCount++;
                        Color color = temporary.Visual.color;
                        Check(color.r > color.g && color.g > color.b, "Generated crumble platforms must start orange");
                    }

                    if (bounds.size.x < 2.55f)
                    {
                        float side = bounds.center.x - visible.center.x;
                        if (firstObservation && Mathf.Abs(side) > visible.width * .20f)
                            outerCount++;
                        if (opening && side < -visible.width * .18f && (!outerLeft || bounds.center.x < outerLeft.transform.position.x))
                            outerLeft = platform;
                        if (opening && side > visible.width * .18f)
                            hasOuterRight = true;
                    }
                    continue;
                }

                Check(Mathf.Abs(bounds.center.x - visible.center.x) < visible.width * .1f,
                    "Level_1 has a steady platform away from the central route");
                steady.Add(bounds);

                if (firstObservation && platform.name != "Starting Dais")
                {
                    Color color = platform.GetComponent<SpriteRenderer>().color;
                    Check(color.r > color.g && color.g > color.b && color.g > .65f,
                        "The steady route needs visibly gold landings");
                }
            }

            Check(steady.Count >= 4, "Too few supports on the steady central route");
            steady.Sort((a, b) => a.center.y.CompareTo(b.center.y));
            ValidateSoundBranches(steady, soundBranches, visible);
            for (int i = 1; i < steady.Count; i++)
            {
                Bounds lower = steady[i - 1];
                Bounds upper = steady[i];
                float gap = upper.center.y - lower.center.y;
                float overlap = Mathf.Min(lower.max.x, upper.max.x) - Mathf.Max(lower.min.x, upper.min.x);
                // At accelerated scroll the frame that reaches the threshold can overshoot by one update.
                if (gap <= 2.65f || gap >= (opening ? 3.65f : 4.9f) || overlap < .5f)
                {
                    string neighboringLandings = string.Join(", ", landings.FindAll(landing =>
                            landing.bounds.center.y >= lower.center.y - .1f && landing.bounds.center.y <= upper.center.y + .1f)
                        .ConvertAll(landing => $"{landing.platform.name} {landing.bounds.center:F2}"));
                    throw new Exception($"Level_1 seed {ArenaCases[arenaCase].seed}, time {arena.Now:F2}, " +
                        $"opening {opening}, scroll {arena.World.ScrollVelocity:F2}. The steady route lost a reachable " +
                        $"landing between {lower.center:F2} and {upper.center:F2}: vertical gap {gap:F2}, " +
                        $"horizontal overlap {overlap:F2}. Landings within the gap: {neighboringLandings}");
                }
            }

            if (!opening)
                return;

            Check(outerLeft && hasOuterRight, "The opening needs narrow, temporary landings on both sides");
            Vector2 position = outerLeft.transform.position;
            if (arenaCase == 0)
                firstOpeningPlatformPosition = position;
            if (arenaCase == 2)
                Check(Vector2.Distance(firstOpeningPlatformPosition, position) > .01f,
                    "Distinct seeds produced the same opening position at the same aspect ratio");
            openingPlatform = outerLeft;
        }

        private static void ValidateSoundBranches(List<Bounds> steady, List<Bounds> soundBranches, Rect visible)
        {
            var occupiedRows = new HashSet<int>();
            float verticalOffset = arena.Settings.PlatformConnectorHeight;
            float verticalTolerance = arena.Settings.PlatformVerticalJitter + .02f;
            foreach (Bounds option in soundBranches)
            {
                int anchor = steady.FindIndex(landing =>
                {
                    float dx = Mathf.Max(option.min.x - landing.max.x, landing.min.x - option.max.x);
                    float dy = option.center.y - landing.center.y;
                    return dx >= .24f && dx <= .29f && Mathf.Abs(dy - verticalOffset) <= verticalTolerance;
                });

                // A deck can be observed outside the viewport just as its row anchor is culled.
                if (anchor < 0 && (option.center.y < visible.yMin - 1f || option.center.y > visible.yMax + 1f))
                    continue;
                Check(anchor >= 0 && option.size.x <= 2.21f && occupiedRows.Add(anchor),
                    $"Sound decks must be single, narrow options next to a gold landing. Found one at {option.center:F2}.");
            }
        }

        private static void ValidateArenaPlatforms(PlatformScroller[] platforms, bool opening)
        {
            Check(platforms.Length < 90, "Level_1 platform population grew without bounds");
            var landings = new List<(Bounds bounds, PlatformScroller platform)>();
            foreach (PlatformScroller platform in platforms)
            {
                var collider = platform.GetComponent<Collider2D>();
                Bounds bounds = collider.bounds;
                foreach (var (previous, _) in landings)
                {
                    float horizontal = Mathf.Max(previous.min.x - bounds.max.x, bounds.min.x - previous.max.x);
                    float vertical = Mathf.Max(previous.min.y - bounds.max.y, bounds.min.y - previous.max.y);
                    Check(horizontal >= .24f || vertical >= 1.09f, "Level_1 landing platforms are too close");
                }
                landings.Add((bounds, platform));

                var soundDeck = platform.GetComponent<BreakablePlatform>();
                if (soundDeck && SeenSoundDecks.Add(soundDeck))
                {
                    var sprite = platform.GetComponent<SpriteRenderer>();
                    Color color = sprite.color;
                    var soundFollower = soundDeck.GetComponentInChildren<FollowerCollectible>();
                    Check(bounds.size.x > bounds.size.y * 2f &&
                        platform.TryGetComponent<PlatformEffector2D>(out var effector) &&
                        effector.enabled && effector.useOneWay && collider.usedByEffector,
                        "Level_1 must generate horizontal one-way Sound platforms");
                    Check(platform.Session == arena && platform.Body && platform.Body.bodyType == RigidbodyType2D.Kinematic &&
                        !soundDeck.IsBroken && collider.enabled && !collider.isTrigger && sprite.enabled &&
                        color.g > color.r && color.b > color.r,
                        "A generated Sound platform must start as a visible cyan landing that scrolls with the arena");
                    Check(!soundFollower || soundFollower.Session == arena, "A Sound follower must use the arena session");
                    soundDeck.Break();
                    Check(soundDeck.IsBroken && !collider.enabled && !sprite.enabled &&
                        (!soundFollower || !soundFollower.gameObject.activeInHierarchy),
                        "Breaking a generated Sound platform must remove the deck and its follower");
                    soundDeck.ResetPlatform();
                    Check(!soundDeck.IsBroken && collider.enabled && sprite.enabled &&
                        (!soundFollower || soundFollower.gameObject.activeInHierarchy), "Reset did not restore the Sound platform");
                    RecordFixtureDirection();
                }

                var hidden = platform.GetComponent<HiddenPlatform>();
                if (!hidden || !SeenHidden.Add(hidden))
                    continue;

                Check(platform.Session == arena, "Generated hidden platform belongs to the wrong session");
                var reward = hidden.GetComponentInChildren<FollowerCollectible>(true);
                Check(!hidden.IsRevealed && !hidden.IsSolid && reward && !reward.gameObject.activeInHierarchy,
                    "The hidden platform and its follower must start inactive");
                hidden.Reveal();
                Check(hidden.IsSolid && reward.gameObject.activeInHierarchy && reward.Session == arena,
                    "Revealing a bridge must enable its support and follower");
                hidden.ResetPlatform();
                Check(!hidden.IsSolid && !reward.gameObject.activeInHierarchy, "Reset did not hide the bridge and follower");
                RecordFixtureDirection();
            }

            ValidateLandingRoutes(landings, opening);
        }

        private static void RecordFixtureDirection()
        {
            if (arena.Now < 1f || arena.World.ScrollVelocity == 0f)
                return;

            directionsWithNewFixtures |= arena.World.ScrollVelocity > 0f ? 1 : 2;
        }

        private static void ExitLevelOne(Exception error = null)
        {
            EditorApplication.update -= TickLevelOne;
            SceneManager.sceneLoaded -= PrepareArena;
            SessionState.SetBool(LevelOnePendingKey, false);
            Time.timeScale = 1;
            if (error == null)
                Debug.Log("LEVEL_ONE_PLATFORM_GENERATION_OK: one recoverable central route, orange sides with a 0.5-second crumble setting, recurring Sight bridges and one-way Sound platforms, and scroll reversals across seeded layouts");
            else
                Debug.LogException(error);
            EditorApplication.Exit(error == null ? 0 : 1);
        }
 }
}
