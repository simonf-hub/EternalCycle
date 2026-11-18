using System.Linq;
using DivineSense;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DivineSenseEditor {
 public static class DivineSensePuzzleBuilder {
  [MenuItem("Divine-Sense/Build Puzzle 1 From Level 1")]
  public static void Build() {
   var puzzle=EditorSceneManager.OpenScene("Assets/Scenes/Level_1.unity",OpenSceneMode.Single);
   var oldPlayers=Object.FindObjectsByType<MicroSense.PlayerController>(FindObjectsSortMode.None);
   var positions=oldPlayers.OrderBy(p=>p.transform.position.x).Select(p=>(Vector2)p.transform.position).ToArray();
   var exit=Object.FindFirstObjectByType<MicroSense.LevelExit>().transform;
   foreach(var player in oldPlayers) Object.DestroyImmediate(player.gameObject);
   foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) Object.DestroyImmediate(camera.gameObject);
   foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
    if(behaviour && behaviour.GetType().Namespace=="MicroSense" && !(behaviour is MicroSense.HiddenPlatform) && !(behaviour is MicroSense.BreakablePlatform) && !(behaviour is MicroSense.AmbientDrift)) Object.DestroyImmediate(behaviour);
   }
   // Pickup artwork is not part of the puzzle's carried-in loadouts.
   foreach(var root in puzzle.GetRootGameObjects()) if(root.name=="Ability Pickups"||root.name=="Ability Slot Upgrades"||root.name=="Game Manager"||root.name=="EventSystem")Object.DestroyImmediate(root);
   var arena=EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity",OpenSceneMode.Additive);
   var session=arena.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameSession>()).Single();
   var manager=session.gameObject;
   foreach(var type in new[]{typeof(PlatformSpawner),typeof(GodDirector)}) {var component=manager.GetComponent(type);if(component)Object.DestroyImmediate(component);}
   SceneManager.MoveGameObjectToScene(manager,puzzle);
   foreach(var p in session.Registry.Players) SceneManager.MoveGameObjectToScene(p.gameObject,puzzle);
   SceneManager.MoveGameObjectToScene(session.World.Camera.gameObject,puzzle);
   var controller=manager.AddComponent<PuzzleLevelController>();controller.Session=session;controller.Exit=exit;
   controller.SpawnA=positions[0];controller.SpawnB=positions[1];
   EditorSceneManager.CloseScene(arena,true);
   SceneManager.SetActiveScene(puzzle);
   EditorSceneManager.SaveScene(puzzle,"Assets/Scenes/DivineSense_Puzzle_1.unity");
   var scenes=EditorBuildSettings.scenes.Where(s=>s.path!="Assets/Scenes/DivineSense_Puzzle_1.unity").ToList();scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/DivineSense_Puzzle_1.unity",true));EditorBuildSettings.scenes=scenes.ToArray();
   AssetDatabase.SaveAssets();Debug.Log("DIVINE_PUZZLE_BUILD_OK");
  }
 }
}
