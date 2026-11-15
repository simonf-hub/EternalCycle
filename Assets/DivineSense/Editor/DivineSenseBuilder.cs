using System.IO;
using System.Linq;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace DivineSenseEditor {
public static class DivineSenseBuilder {
 const string Folder="Assets/DivineSense";
 static Sprite square,circle;
 static Color gold=new Color(1,.83f,.38f), red=new Color(.95f,.3f,.36f);
 [MenuItem("Divine-Sense/Build New Arena")]
 public static void Build(){
 Directory.CreateDirectory("Assets/Prefabs/EternalCycle/Arena");Directory.CreateDirectory(Folder+"/Settings");
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
 var growth=managers.AddComponent<GrowthEffect>();var reverse=managers.AddComponent<ReverseControlsEffect>();
 var gods=managers.AddComponent<GodDirector>();gods.Session=session;gods.PositionSwap=pos;gods.Growth=growth;gods.AbilitySwap=swap;gods.Reverse=reverse;gods.PickupsRoot=new GameObject("GodPickupsRoot").transform;
 var spawn=managers.AddComponent<PlatformSpawner>();spawn.Session=session;spawn.Root=new GameObject("PlatformsRoot").transform;
 registry.Players=new[]{Player(0,session,registry),Player(1,session,registry)};
 Physics2D.IgnoreCollision(registry.Players[0].Motor.Collider,registry.Players[1].Motor.Collider);
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
 EditorSceneManager.SaveScene(scene,"Assets/Scenes/DivineSense_Arena.unity");
 var entries=EditorBuildSettings.scenes.Where(s=>s.path!="Assets/Scenes/DivineSense_Arena.unity").ToList();entries.Add(new EditorBuildSettingsScene("Assets/Scenes/DivineSense_Arena.unity",true));EditorBuildSettings.scenes=entries.ToArray();
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
 var go=new GameObject(id==0?"Angel":"Devil");go.transform.position=new Vector3(id==0?-1.3f:1.3f,-4.25f,0);
 var p=go.AddComponent<PlayerContext>();p.Id=id;p.Session=session;p.Registry=registry;
 var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.simulated=false;rb.freezeRotation=true;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
 var col=go.AddComponent<BoxCollider2D>();col.size=new Vector2(.7f,1);
 var mat=new PhysicsMaterial2D("Divine frictionless"){friction=0,bounciness=0};string mp=Folder+"/Settings/PlayerPhysics.physicsMaterial2D";var existing=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(mp);if(!existing){AssetDatabase.CreateAsset(mat,mp);existing=mat;}else Object.DestroyImmediate(mat);col.sharedMaterial=existing;
 var visual=new GameObject("Body Visual").transform;visual.SetParent(go.transform,false);
 var color=id==0?gold:red;Visual("Body",visual,Vector3.zero,new Vector3(.7f,1,1),color,circle,10);
 Visual("Eye L",visual,new Vector3(-.13f,.15f,0),new Vector3(.09f,.12f,1),Color.white,circle,11);Visual("Eye R",visual,new Vector3(.13f,.15f,0),new Vector3(.09f,.12f,1),Color.white,circle,11);
 if(id==0){Visual("Halo",visual,new Vector3(0,.68f,0),new Vector3(.6f,.1f,1),gold,circle,12);Visual("Wing L",visual,new Vector3(-.48f,.1f,0),new Vector3(.5f,.25f,1),Color.white,circle,9);Visual("Wing R",visual,new Vector3(.48f,.1f,0),new Vector3(.5f,.25f,1),Color.white,circle,9);}else{foreach(int sign in new[]{-1,1})Visual("Horn",visual,new Vector3(sign*.22f,.5f,0),new Vector3(.13f,.35f,1),red,square,12);}
 var marker=new GameObject("Respawn Cross").transform;marker.SetParent(go.transform,false);Visual("Horizontal",marker,Vector3.zero,new Vector3(.8f,.13f,1),color,square,20);Visual("Vertical",marker,Vector3.zero,new Vector3(.13f,.8f,1),color,square,20);marker.gameObject.SetActive(false);
 p.Input=go.AddComponent<PlayerInputReader>();p.Input.Owner=p;
 p.Motor=go.AddComponent<PlayerMotor2D>();p.Motor.Owner=p;p.Motor.Body=rb;p.Motor.Collider=col;p.Motor.BodyVisual=visual;p.Motor.Glide=go.AddComponent<GlideAbility>();p.Motor.DoubleJump=go.AddComponent<DoubleJumpAbility>();
 p.Life=go.AddComponent<PlayerLifecycle>();p.Life.Owner=p;p.Score=go.AddComponent<FollowerCounter>();p.Score.Owner=p;p.Status=go.AddComponent<PlayerStatusEffects>();p.Status.Owner=p;
 p.Respawn=go.AddComponent<RespawnController>();p.Respawn.Owner=p;p.Respawn.Marker=marker;
 p.Abilities=go.AddComponent<PlayerAbilityController>();p.Abilities.Owner=p;p.Abilities.Sight=go.AddComponent<SightAbility>();p.Abilities.Sound=go.AddComponent<SoundAbility>();
 p.Visuals=go.AddComponent<PlayerVisuals>();p.Visuals.Owner=p;p.Visuals.Renderers=visual.GetComponentsInChildren<SpriteRenderer>();
 PrefabUtility.SaveAsPrefabAssetAndConnect(go,"Assets/Prefabs/EternalCycle/Arena/"+(id==0?"Angel":"Devil")+".prefab",InteractionMode.AutomatedAction);return p;
 }
 static GameObject Visual(string name,Transform parent,Vector3 position,Vector3 scale,Color color,Sprite sprite,int order){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;return go;}
}
}
