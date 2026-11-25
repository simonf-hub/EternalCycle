using System;
using System.IO;
using DivineSense;
using MicroSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DivineSenseEditor
{
    public static class FinalCooperativeLevelBuilder
    {
        private const string Prefabs = "Assets/Prefabs/EternalCycle/";
        private const string CooperativePrefabs = Prefabs + "Cooperative/";
        private const string Environment = Prefabs + "Environment/";
        private const string Arena = Prefabs + "Arena/";
        private const string SettingsPath = "Assets/DivineSense/Settings/ArenaSettings.asset";
        private const string TutorialPath = "Assets/Scenes/Level_0.unity";
        private const string CompetitivePath = "Assets/Scenes/Level_1.unity";
        private const string CooperativePath = "Assets/Scenes/Level_2.unity";
        private const int GroundLayer = 9;

        private static readonly Color Gold = new Color(1f, .79f, .45f);
        private static readonly Color SightColor = new Color(.77f, .52f, 1f);
        private static readonly Color SoundColor = new Color(.44f, .83f, 1f);
        private static readonly Color PlateColor = new Color(.59f, .82f, .85f);
        private static readonly Color SteelColor = new Color(.34f, .49f, .64f);

        private static Sprite square;
        private static Material spriteMaterial;

        [MenuItem("Divine-Sense/Build Starter Co-op Layouts")]
        public static void Build()
        {
            var settings = Load<GameSettings>(SettingsPath);
            square = Load<Sprite>(Prefabs + "Art/Prototype/RoundedSquare.png");
            spriteMaterial = Load<GameObject>(Environment + "Platform.prefab")
                .GetComponent<SpriteRenderer>().sharedMaterial;

            BuildTutorial(settings);
            BuildCooperativeTrial(settings);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TutorialPath, true),
                new EditorBuildSettingsScene(CompetitivePath, true),
                new EditorBuildSettingsScene(CooperativePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("FINAL_COOPERATIVE_LEVELS_BUILD_OK");
        }

        private static void BuildTutorial(GameSettings settings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsurePrefabs();
            var level = CreateRig(settings, TrialLevel.Tutorial, "Level 0 · First Steps",
                new Vector2(0f, -1.8f), new Vector2(2f, -1.8f), -5.5f, 60f);
            var layout = Group("Layout");
            var movement = Group("01 · Move and jump", layout);
            var senses = Group("02 · Sight and Sound", layout);
            var gifts = Group("03 · Gifts from the gods", layout);
            var destination = Group("04 · Exit", layout);

            Surface("Starting floor", movement, -5f, 7.3f, -1.8f, .75f);
            Surface("Practice ledge", movement, 7.3f, 11.65f, -.55f, 2f);
            Boundary("West wall", layout, -5.4f);
            Boundary("East wall", layout, 59.7f);
            Sign(movement, 4.4f, 1.15f, "MOVE  ·  JUMP", PlateColor);

            var bridge = Hidden("Sight bridge", senses, 12.15f, 15.95f, .05f);
            Surface("Far landing", senses, 16.4f, 24.25f, -.55f, 1.25f);
            var wall = Breakable("Sound wall", senses, 21.1f, -.55f);
            Sign(senses, 10.5f, 2.4f, "SIGHT", SightColor);
            Sign(senses, 19.75f, 2.4f, "SOUND", SoundColor);

            Surface("Gift gallery floor", gifts, 24.25f, 50.3f, -.55f, 1.25f);
            Surface("Agility practice ledge", gifts, 34.8f, 36.7f, 2.3f, .35f);
            Surface("Exit landing", destination, 50.3f, 59.4f, -.55f, 1.25f);

            var athena = Gift(settings, level.Session, GodKind.Athena, gifts, 26.4f, -.55f);
            var zeus = Gift(settings, level.Session, GodKind.Zeus, gifts, 32.6f, -.55f);
            var hera = Gift(settings, level.Session, GodKind.Hera, gifts, 40f, -.55f);
            var ares = Gift(settings, level.Session, GodKind.Ares, gifts, 47f, -.55f);

            level.RequiredReveals = new[] { bridge };
            level.RequiredBreaks = new[] { wall };
            level.RequiredGifts = new[] { athena, zeus, hera, ares };
            level.Checkpoints = new[]
            {
                Checkpoint("Gallery checkpoint", destination, 23.5f, -.55f),
                Checkpoint("Exit approach checkpoint", destination, 51f, -.55f)
            };
            level.Exit = Exit(destination, 55f, -.55f);
            level.Lessons = new[]
            {
                Lesson(movement, -5f, 8.3f, "Move and jump",
                    "Use the left stick to move and A to jump. On keyboard, Mortal 1 uses A/D and W, and Mortal 2 uses ←/→ and ↑."),
                Lesson(senses, 8.3f, 16.5f, "Mortal 1 · Sight",
                    "Press B near the gap to reveal a platform and cross together. Sight also hides your partner for 1.5 seconds. Keyboard cast is S."),
                Lesson(senses, 16.5f, 24.4f, "Mortal 2 · Sound",
                    "Press B near the cracked wall to break it. Sound also knocks your partner away. Keyboard cast is ↓."),
                Lesson(gifts, 24.4f, 29.3f, GodPickup.DisplayName(GodKind.Athena),
                    "Touch the gift to swap positions with your partner. Try it while standing apart. It also swaps respawn markers when they are in use."),
                Lesson(gifts, 29.3f, 37f, GodPickup.DisplayName(GodKind.Zeus),
                    "For 3 seconds you are 1.5× larger, jump 50% higher and move at 65% speed. Try reaching the high practice ledge. Sight and Sound cannot affect you."),
                Lesson(gifts, 37f, 44f, GodPickup.DisplayName(GodKind.Hera),
                    "Touch the gift while your partner is nearby. Hera pulls them toward you."),
                Lesson(gifts, 44f, 50.35f, GodPickup.DisplayName(GodKind.Ares),
                    "The gift launches a homing bomb toward your partner. A hit stuns them for 3 seconds."),
                Lesson(destination, 50.35f, 59.4f, "Reach the exit together",
                    "Mortal 1 can hold A while falling to glide and release to dash upward. Mortal 2 can jump again in midair or double-tap a direction to dash.")
            };
            Backdrops(6f, 24.5f, 42f, 55f);
            Save(scene, TutorialPath);
        }

        private static void BuildCooperativeTrial(GameSettings settings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = CreateRig(settings, TrialLevel.Cooperative, "Level 2 · Athena's Trial",
                new Vector2(0f, -1.8f), new Vector2(2.2f, -1.8f), -5.5f, 94f);
            var layout = Group("Layout");
            var first = Group("01 · Split gallery", layout);
            var second = Group("02 · Upper gallery", layout);
            var destination = Group("03 · Exit", layout);

            Boundary("West wall", layout, -5.4f);
            Boundary("East wall", layout, 93.7f);
            Surface("Starting gallery", first, -5f, 11f, -1.8f, .85f);
            var firstHidden = Hidden("First hidden step", first, 12.3f, 15.4f, -.95f);
            var secondHidden = Hidden("Second hidden step", first, 16.65f, 19.8f, .05f);
            Surface("Lower gallery floor", first, 21.1f, 44f, .8f, .8f);
            var firstWall = Breakable("Lower Sound wall", first, 26.25f, .8f);
            Surface("Split gallery step 1", first, 28.5f, 32f, 2.02f, .35f);
            Surface("Split gallery step 2", first, 32.3f, 35.65f, 3.25f, .35f);
            Surface("Upper plate balcony", first, 35.9f, 40.25f, 4.55f, .35f);

            var lowerPlate = Plate("Lower plate I", first, 39.6f, .8f);
            var upperPlate = Plate("Upper plate I", first, 38.3f, 4.55f);
            var firstGate = Gate("Paired gate I", first, 42f, .8f, 6.9f, lowerPlate, upperPlate);
            Sign(first, 39.6f, 2.05f, "I", Gold);
            Sign(first, 38.3f, 5.8f, "I", Gold);
            Sign(first, 9.55f, 1f, "SIGHT", SightColor);
            Sign(first, 24.35f, 3.25f, "SOUND", SoundColor);

            Surface("Checkpoint landing", second, 44f, 51f, .8f, .8f);
            var thirdHidden = Hidden("Third hidden step", second, 52.25f, 55.3f, 1.7f);
            var fourthHidden = Hidden("Fourth hidden step", second, 56.5f, 59.65f, 2.6f);
            Surface("Upper gallery floor", second, 60.9f, 85f, 3.45f, .9f);
            var secondWall = Breakable("Upper Sound wall", second, 65.2f, 3.45f);
            Surface("Upper gallery step", second, 69f, 72.4f, 4.75f, .35f);
            Surface("Second plate balcony", second, 73f, 77.9f, 6.05f, .35f);
            var secondLowerPlate = Plate("Lower plate II", second, 79.3f, 3.45f);
            var secondUpperPlate = Plate("Upper plate II", second, 76f, 6.05f);
            var secondGate = Gate("Paired gate II", second, 82.1f, 3.45f, 9.35f, secondLowerPlate, secondUpperPlate);
            Sign(second, 79.3f, 4.7f, "II", Gold);
            Sign(second, 76f, 7.3f, "II", Gold);
            Sign(second, 49.5f, 3.3f, "SIGHT", SightColor);
            Sign(second, 62.8f, 5.8f, "SOUND", SoundColor);
            Surface("Final landing", destination, 85f, 93.4f, 3.45f, .9f);

            level.RequiredReveals = new[] { firstHidden, secondHidden, thirdHidden, fourthHidden };
            level.RequiredBreaks = new[] { firstWall, secondWall };
            level.RequiredGates = new[] { firstGate, secondGate };
            level.Checkpoints = new[]
            {
                Checkpoint("Lower gallery checkpoint", first, 23.25f, .8f),
                Checkpoint("Upper gallery checkpoint", second, 46.45f, .8f),
                Checkpoint("Far gallery checkpoint", second, 63.3f, 3.45f)
            };
            level.Exit = Exit(destination, 89.1f, 3.45f);
            level.Lessons = new[]
            {
                Lesson(first, -5f, 21.1f, "Cross the first gap",
                    "Reveal the hidden steps with Sight. Move to the revealed edge and cast again if the next step is out of range."),
                Lesson(first, 21.1f, 27.75f, "Clear the lower gallery",
                    "Mortal 2 can break the cracked wall with Sound. Give your partner room, since the same cast can knock them back."),
                Lesson(first, 27.75f, 44.2f, "Open gate I",
                    "Take different paths. Stand on the upper and lower plates at the same time to open the gate."),
                Lesson(second, 44.2f, 60.9f, "Ascend together",
                    "Use Sight from the edge and again from the revealed steps. Save a jump or glide for the last rise."),
                Lesson(second, 60.9f, 66.9f, "Clear the upper gallery",
                    "Use Sound to break the second wall, then climb up to the last pair of plates."),
                Lesson(second, 66.9f, 84.2f, "Open gate II",
                    "One mortal climbs to the balcony while the other takes the floor route. Stand on both plates together."),
                Lesson(destination, 84.2f, 93.4f, "Reach the exit together",
                    "Both mortals must reach the final exit after uncovering the path and opening both gates.")
            };
            Backdrops(5f, 24f, 42f, 61f, 80f, 92f);
            Save(scene, CooperativePath);
        }

        private static CooperativeLevelController CreateRig(GameSettings settings, TrialLevel trial,
            string title, Vector2 angelStart, Vector2 devilStart, float left, float right)
        {
            var root = Group("Trial");
            var session = root.gameObject.AddComponent<GameSession>();
            session.Trial = trial;
            session.Settings = settings;
            session.Registry = root.gameObject.AddComponent<PlayerRegistry>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            session.World = root.gameObject.AddComponent<SceneModeController>();
            session.World.Session = session;
            session.World.Camera = camera;

            var positions = Group("Spawn and camera markers", root);
            var level = root.gameObject.AddComponent<CooperativeLevelController>();
            level.Session = session;
            level.Title = title;
            level.AngelStart = Marker("Mortal 1 start", positions, angelStart + Vector2.up * .58f);
            level.DevilStart = Marker("Mortal 2 start", positions, devilStart + Vector2.up * .58f);
            level.LeftLimit = Marker("Camera left limit", positions, new Vector2(left, 0f));
            level.RightLimit = Marker("Camera right limit", positions, new Vector2(right, 0f));
            level.FallLimit = Marker("Fall recovery height", positions, new Vector2(0f, -7.5f));
            level.Setup = root.gameObject.AddComponent<MatchSetupController>();
            level.Setup.Session = session;

            var players = Group("Mortals");
            session.Registry.Players = new[]
            {
                PlacePlayer(Arena + "Angel.prefab", players, level.AngelStart.position, session),
                PlacePlayer(Arena + "Devil.prefab", players, level.DevilStart.position, session)
            };
            return level;
        }

        private static PlayerContext PlacePlayer(string asset, Transform parent, Vector2 position, GameSession session)
        {
            GameObject playerObject = Place(asset, parent, position, Vector3.one);
            var player = playerObject.GetComponent<PlayerContext>();
            player.Session = session;
            player.Registry = session.Registry;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            return player;
        }

        private static void Surface(string name, Transform parent, float left, float right, float top, float depth)
        {
            Place(Environment + "Platform.prefab", parent,
                new Vector2((left + right) * .5f, top - depth * .5f),
                new Vector3(right - left, depth, 1f), name);
        }

        private static void Boundary(string name, Transform parent, float x)
        {
            Place(Environment + "Platform.prefab", parent,
                new Vector2(x, 2f), new Vector3(.7f, 18f, 1f), name);
        }

        private static HiddenPlatform Hidden(string name, Transform parent, float left, float right, float top)
        {
            const float depth = .34f;
            var instance = Place(Environment + "HiddenPlatform.prefab", parent,
                new Vector2((left + right) * .5f, top - depth * .5f),
                new Vector3(right - left, depth, 1f), name);
            return instance.GetComponent<HiddenPlatform>();
        }

        private static BreakablePlatform Breakable(string name, Transform parent, float x, float floor)
        {
            var instance = Place(Environment + "BreakablePlatform.prefab", parent,
                new Vector2(x, floor + 2.2f), new Vector3(1f, 4.7f, 1f), name);
            var visual = instance.GetComponent<SpriteRenderer>();
            visual.color = SoundColor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
            return instance.GetComponent<BreakablePlatform>();
        }

        private static GodPickup Gift(GameSettings settings, GameSession session, GodKind kind,
            Transform parent, float x, float floor)
        {
            var prefab = settings.PickupPrefabFor(kind);
            var instance = Place(AssetDatabase.GetAssetPath(prefab), parent,
                new Vector2(x, floor + .65f), prefab.transform.localScale);
            var pickup = instance.GetComponent<GodPickup>();
            pickup.Session = session;
            pickup.Kind = kind;
            pickup.RefreshAppearance();
            PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
            return pickup;
        }

        private static CooperativePressurePlate Plate(string name, Transform parent, float x, float floor) =>
            Place(CooperativePrefabs + "PressurePlate.prefab", parent, new Vector2(x, floor),
                Vector3.one, name).GetComponent<CooperativePressurePlate>();

        private static CooperativeGate Gate(string name, Transform parent, float x, float floor, float top,
            CooperativePressurePlate lower, CooperativePressurePlate upper)
        {
            var gate = Place(CooperativePrefabs + "PairedGate.prefab", parent, new Vector2(x, (floor + top) * .5f),
                new Vector3(.95f, top - floor, 1f), name).GetComponent<CooperativeGate>();
            gate.LowerPlate = lower;
            gate.UpperPlate = upper;
            PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
            return gate;
        }

        private static CooperativeLevelController.Checkpoint Checkpoint(
            string name, Transform parent, float x, float floor)
        {
            var instance = Place(CooperativePrefabs + "Checkpoint.prefab", parent,
                new Vector2(x, floor), Vector3.one, name);
            return new CooperativeLevelController.Checkpoint
            {
                Zone = instance.GetComponent<Collider2D>(),
                AngelSpawn = instance.transform.Find("Mortal 1 spawn"),
                DevilSpawn = instance.transform.Find("Mortal 2 spawn")
            };
        }

        private static Collider2D Exit(Transform parent, float x, float floor) =>
            Place(CooperativePrefabs + "SharedExit.prefab", parent, new Vector2(x, floor),
                Vector3.one).GetComponent<Collider2D>();

        private static CooperativeLevelController.Lesson Lesson(
            Transform parent, float left, float right, string title, string instructions)
        {
            var volume = Group("Hint · " + title, parent);
            volume.position = new Vector3((left + right) * .5f, 3f);
            var bounds = Trigger(volume.gameObject, Vector2.zero, new Vector2(right - left, 17f));
            return new CooperativeLevelController.Lesson { Title = title, Instructions = instructions, Zone = bounds };
        }

        private static void Backdrops(params float[] centers)
        {
            var root = Group("Backdrop");
            foreach (float x in centers)
                Place(Arena + "Colosseum.prefab", root, new Vector2(x, 1f), Vector3.one);
        }

        private static void EnsurePrefabs()
        {
            if (!AssetDatabase.IsValidFolder(CooperativePrefabs.TrimEnd('/')))
                AssetDatabase.CreateFolder(Prefabs.TrimEnd('/'), "Cooperative");

            EnsurePrefab("SharedExit", root =>
            {
                Trigger(root, new Vector2(0f, 1.1f), new Vector2(3.2f, 2.5f));
                AddSprite("Lintel", root.transform, new Vector2(0f, 2.45f), new Vector2(3.2f, .34f), Gold, 5);
                for (int side = -1; side <= 1; side += 2)
                    AddSprite("Column", root.transform, new Vector2(side * 1.44f, 1.18f), new Vector2(.28f, 2.33f), Gold, 3);
                Sign(root.transform, 0f, 2.9f, "EXIT", Gold);
            });

            EnsurePrefab("Checkpoint", root =>
            {
                Trigger(root, new Vector2(0f, .64f), new Vector2(2.8f, 1.6f));
                AddSprite("Inlay", root.transform, new Vector2(0f, .04f), new Vector2(2.72f, .12f), Gold, 2);
                Marker("Mortal 1 spawn", root.transform, new Vector2(-.55f, .59f));
                Marker("Mortal 2 spawn", root.transform, new Vector2(.55f, .59f));
            });

            EnsurePrefab("PressurePlate", root =>
            {
                var plate = root.AddComponent<CooperativePressurePlate>();
                plate.Zone = Trigger(root, new Vector2(0f, .44f), new Vector2(1.5f, 1.35f));
                AddSprite("Plate", root.transform, new Vector2(0f, .08f), new Vector2(1.45f, .18f), SteelColor, 3);
                plate.Indicator = AddSprite("Indicator", root.transform, new Vector2(0f, .18f),
                    new Vector2(.66f, .14f), PlateColor, 4);
            });

            EnsurePrefab("PairedGate", root =>
            {
                root.layer = GroundLayer;
                var gate = root.AddComponent<CooperativeGate>();
                var barrier = root.AddComponent<BoxCollider2D>();
                barrier.offset = new Vector2(0f, -.02f);
                barrier.size = new Vector2(1f, 1.04f);
                gate.Barrier = barrier;
                AddSprite("Stone frame", root.transform, Vector2.zero, Vector2.one, SteelColor, 3);
                AddSprite("Gate inlay", root.transform, Vector2.zero, new Vector2(.26f, .92f), Gold, 4);
                gate.Visuals = root.GetComponentsInChildren<SpriteRenderer>();
            });
        }

        private static GameObject Place(string asset, Transform parent, Vector2 position, Vector3 scale, string name = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(asset), parent);
            if (name != null)
                instance.name = name;
            instance.transform.position = position;
            instance.transform.localScale = scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset)
                throw new FileNotFoundException("Required Unity asset", path);
            return asset;
        }

        private static void EnsurePrefab(string name, Action<GameObject> configure)
        {
            string path = CooperativePrefabs + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path))
                return;

            var root = new GameObject(name);
            configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static Transform Group(string name, Transform parent = null)
        {
            var transform = new GameObject(name).transform;
            transform.SetParent(parent, false);
            return transform;
        }

        private static Transform Marker(string name, Transform parent, Vector2 position)
        {
            var marker = Group(name, parent);
            marker.localPosition = position;
            return marker;
        }

        private static BoxCollider2D Trigger(GameObject owner, Vector2 center, Vector2 size)
        {
            var box = owner.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            box.offset = center;
            return box;
        }

        private static SpriteRenderer AddSprite(string name, Transform parent, Vector2 position,
            Vector2 size, Color tint, int order)
        {
            var visual = Marker(name, parent, position);
            visual.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sharedMaterial = spriteMaterial;
            renderer.sortingOrder = order;
            renderer.color = tint;
            return renderer;
        }

        private static void Sign(Transform parent, float x, float y, string caption, Color color)
        {
            var label = Marker(caption, parent, new Vector2(x, y));
            var text = label.gameObject.AddComponent<TextMesh>();
            text.text = caption;
            text.fontSize = 55;
            text.characterSize = .095f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = color;
            label.GetComponent<MeshRenderer>().sortingOrder = 8;
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new IOException("Could not save " + path);
        }
    }
}
