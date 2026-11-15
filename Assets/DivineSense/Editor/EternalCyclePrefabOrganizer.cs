using System;
using System.IO;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DivineSenseEditor
{
    public static class EternalCyclePrefabOrganizer
    {
        public const string ArenaFolder = "Assets/Prefabs/EternalCycle/Arena";

        [MenuItem("Divine-Sense/Organize EternalCycle Prefabs")]
        public static void Organize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before organizing prefabs.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity");
            var session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerContext>(FindObjectsSortMode.None))
                Connect(player.gameObject, ArenaFolder + "/" + (player.Id == 0 ? "Angel" : "Devil") + ".prefab");
            var background = GameObject.Find("Divine Colosseum");
            if (background) Connect(background, ArenaFolder + "/Colosseum.prefab");
            ConfigurePickups(session.Settings);
            ConfigureFollowers(session.Settings);
            foreach (var follower in UnityEngine.Object.FindObjectsByType<FollowerCollectible>(FindObjectsSortMode.None))
                follower.Session = session;
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            foreach (var player in session.Registry.Players)
                if (!PrefabUtility.IsPartOfPrefabInstance(player))
                    throw new InvalidOperationException("Player is not linked to its prefab.");
            Debug.Log("ETERNAL_CYCLE_PREFABS_OK: linked players, background, four god pickups and preserved asset GUIDs.");
        }

        private static void Connect(GameObject instance, string path)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(instance)) return;
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
        }

        private static void ConfigureFollowers(GameSettings settings)
        {
            string path = ArenaFolder + "/Follower.prefab";
            foreach (var platform in new[] { settings.PlatformPrefab, settings.CrumblePrefab })
            {
                string platformPath = AssetDatabase.GetAssetPath(platform);
                var root = PrefabUtility.LoadPrefabContents(platformPath);
                try
                {
                    var follower = root.GetComponentInChildren<FollowerCollectible>();
                    if (!follower || PrefabUtility.IsPartOfPrefabInstance(follower)) continue;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!prefab) prefab = PrefabUtility.SaveAsPrefabAsset(follower.gameObject, path);
                    var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    replacement.transform.localPosition = follower.transform.localPosition;
                    replacement.transform.localRotation = follower.transform.localRotation;
                    replacement.transform.localScale = follower.transform.localScale;
                    UnityEngine.Object.DestroyImmediate(follower.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(root, platformPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        public static void ConfigurePickups(GameSettings settings)
        {
            settings.HermesPickupPrefab = CreatePickup(settings, GodKind.Hermes);
            settings.DemeterPickupPrefab = CreatePickup(settings, GodKind.Demeter);
            settings.AthenaPickupPrefab = CreatePickup(settings, GodKind.Athena);
            settings.DionysusPickupPrefab = CreatePickup(settings, GodKind.Dionysus);
            EditorUtility.SetDirty(settings);
        }

        private static GameObject CreatePickup(GameSettings settings, GodKind kind)
        {
            string path = ArenaFolder + "/" + kind + "Pickup.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;
            // Variants inherit shared pickup behavior while allowing individual artwork.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(settings.GodPickupPrefab);
            instance.name = kind + " Pickup";
            instance.GetComponent<GodPickup>().Kind = kind;
            var label = instance.GetComponentInChildren<TextMesh>();
            if (label) label.text = kind.ToString();
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }
    }
}
