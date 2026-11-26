using System.IO;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace DivineSenseEditor {
public static class DivineSenseBuilder {
 const string Folder="Assets/DivineSense";
 static Sprite square,circle;
 static Color gold=new Color(1,.83f,.38f);
 public static void Build(){
 Directory.CreateDirectory("Assets/Prefabs/EternalCycle/Arena");Directory.CreateDirectory(Folder+"/Settings");Directory.CreateDirectory("Assets/Scenes/Archive");
 square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/EternalCycle/Art/Prototype/RoundedSquare.png");circle=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/EternalCycle/Art/Prototype/Circle.png");
 var settings=AssetDatabase.LoadAssetAtPath<GameSettings>(Folder+"/Settings/ArenaSettings.asset");
 if(!settings){settings=ScriptableObject.CreateInstance<GameSettings>();AssetDatabase.CreateAsset(settings,Folder+"/Settings/ArenaSettings.asset");}
 settings.PlatformSprite=square;settings.CircleSprite=circle;
 settings.PlatformPrefab=Platform(false);settings.CrumblePrefab=Platform(true);settings.GodPickupPrefab=Pickup();EternalCyclePrefabOrganizer.ConfigurePickups(settings);EditorUtility.SetDirty(settings);
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 var managers=new GameObject("Managers");var session=managers.AddComponent<GameSession>();session.Settings=settings;
 var registry=managers.AddComponent<PlayerRegistry>();session.Registry=registry;
 var cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";cameraGo.transform.position=new Vector3(0,0,-10);var cam=cameraGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=9;cameraGo.AddComponent<AudioListener>();cameraGo.AddComponent<UniversalAdditionalCameraData>();
 var world=managers.AddComponent<SceneModeController>();world.Session=session;world.Camera=cam;session.World=world;
 var swap=managers.AddComponent<AbilitySwapEffect>();swap.Session=session;session.Swap=swap;
 var pos=managers.AddComponent<PositionSwapEffect>();pos.Session=session;
 var gods=managers.AddComponent<GodDirector>();gods.Session=session;gods.PickupsRoot=new GameObject("GodPickupsRoot").transform;
 var spawn=managers.AddComponent<PlatformSpawner>();spawn.Session=session;spawn.Root=new GameObject("PlatformsRoot").transform;
 registry.Players=new[]{Player(0,session,registry),Player(1,session,registry)};
 var setup=managers.AddComponent<MatchSetupController>();setup.Session=session;
 var hud=new GameObject("HUD").AddComponent<HUDController>();hud.Session=session;hud.Setup=setup;hud.Gods=gods;
 var bg=new GameObject("Divine Colosseum").transform;
 for(int side=-1;side<=1;side+=2)for(int j=0;j<3;j++){
 float x=side*(9+j*1.6f);Visual("Column",bg,new Vector3(x,-1,0),new Vector3(.65f,17,1),new Color(.43f,.48f,.55f,.25f),square,-10);
 Visual("Capital",bg,new Vector3(x,6.8f,0),new Vector3(1.4f,.45f,1),new Color(.7f,.62f,.4f,.35f),square,-9);
 }
 for(int i=0;i<22;i++)Visual("Audience spirit",bg,new Vector3(-13+i*1.25f,-6+(i%3)*.45f,0),Vector3.one*.32f,new Color(.78f,.72f,.5f,.2f),circle,-8);
 PrefabUtility.SaveAsPrefabAssetAndConnect(bg.gameObject,"Assets/Prefabs/EternalCycle/Arena/Colosseum.prefab",InteractionMode.AutomatedAction);
 // Broad starting support; generated route continues above it.
 var start=(GameObject)PrefabUtility.InstantiatePrefab(settings.PlatformPrefab,spawn.Root);start.name="Starting Dais";start.transform.position=new Vector3(0,-5,0);start.transform.localScale=new Vector3(9,.4f,1);start.GetComponent<PlatformScroller>().Session=session;foreach(var f in start.GetComponentsInChildren<FollowerCollectible>())f.Session=session;
 EditorSceneManager.SaveScene(scene,"Assets/Scenes/Archive/DivineSense_Arena.unity");
 AssetDatabase.SaveAssets();Debug.Log("DIVINE_SENSE_BUILD_OK");
 }
 static GameObject Platform(bool crumble){
 var go=Visual(crumble?"Crumbling Platform":"Platform",null,Vector3.zero,Vector3.one,crumble?new Color(.9f,.57f,.32f):new Color(.6f,.76f,.83f),square,1);go.layer=9;
 var rb=go.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.useFullKinematicContacts=true;
 var col=go.AddComponent<BoxCollider2D>();col.usedByEffector=true;var eff=go.AddComponent<PlatformEffector2D>();eff.useOneWay=true;eff.useOneWayGrouping=true;
 var sc=go.AddComponent<PlatformScroller>();sc.Body=rb;
 if(crumble){var c=go.AddComponent<CrumblingPlatform>();c.Scroller=sc;c.Visual=go.GetComponent<SpriteRenderer>();}
 var follower=Visual("Follower",go.transform,new Vector3(0,2.2f,0),new Vector3(.12f,1.5f,1),gold,circle,4);var trigger=follower.AddComponent<CircleCollider2D>();trigger.isTrigger=true;trigger.radius=.5f;follower.AddComponent<FollowerCollectible>();
 var saved=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefabs/EternalCycle/Arena/"+(crumble?"CrumblingPlatform":"Platform")+".prefab");Object.DestroyImmediate(go);return saved;
 }
 static GameObject Pickup(){
 var go=Visual("God Gift",null,Vector3.zero,Vector3.one*.65f,new Color(.76f,.51f,1),square,6);var rb=go.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.useFullKinematicContacts=true;var col=go.AddComponent<CircleCollider2D>();col.isTrigger=true;
 var p=go.AddComponent<GodPickup>();var mover=go.AddComponent<FallingPickupMover>();mover.Pickup=p;mover.Body=rb;
 var label=new GameObject("God Name");label.transform.SetParent(go.transform,false);label.transform.localPosition=Vector3.up;var tm=label.AddComponent<TextMesh>();tm.text="GIFT";tm.characterSize=.13f;tm.fontSize=40;tm.anchor=TextAnchor.MiddleCenter;label.GetComponent<MeshRenderer>().sortingOrder=20;
 var saved=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefabs/EternalCycle/Arena/GodPickup.prefab");Object.DestroyImmediate(go);return saved;
 }
 static PlayerContext Player(int id,GameSession session,PlayerRegistry registry){
     string path = EternalCyclePrefabOrganizer.ArenaFolder + (id == 0 ? "/Angel.prefab" : "/Devil.prefab");
     var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
     if (!prefab) throw new FileNotFoundException("Missing mortal prefab.", path);

     var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
     instance.transform.position = new Vector3(id == 0 ? -1.3f : 1.3f, -4.25f, 0f);
     var player = instance.GetComponent<PlayerContext>();
     player.Session = session;
     player.Registry = registry;
     PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
     PrefabUtility.RecordPrefabInstancePropertyModifications(player);
     return player;
 }
 static GameObject Visual(string name,Transform parent,Vector3 position,Vector3 scale,Color color,Sprite sprite,int order){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;return go;}
}
}
