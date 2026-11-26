using UnityEngine;
namespace DivineSense { [DefaultExecutionOrder(-100)] public sealed class SceneModeController : MonoBehaviour {
 public GameSession Session; public Camera Camera; public SpriteRenderer Backdrop;
 [SerializeField] private SceneMode upwardScrollMode=SceneMode.Hell;
 private float nextFlipAt;
 public event System.Action<SceneMode> ModeChanged;
 public float FlipRemaining => Mathf.Max(0,nextFlipAt-Session.Now);
 internal float FlipInterval => Mathf.Max(.1f,Session.Settings.SceneFlipInterval);
 public SceneMode CurrentMode {get;private set;}
 public float ScrollVelocity=>(CurrentMode==upwardScrollMode?1:-1)*Session.Settings.ScrollSpeed;
 public Rect VisibleBounds {get{float h=Camera.orthographicSize,w=h*Camera.aspect;Vector2 p=Camera.transform.position;return new Rect(p.x-w,p.y-h,w*2,h*2);}}
 public bool Contains(Vector2 p){var r=VisibleBounds;float m=Session.Settings.BoundaryMargin;return p.x>=r.xMin-m&&p.x<=r.xMax+m&&p.y>=r.yMin-m&&p.y<=r.yMax+m;}
 void Start(){CurrentMode=Session.Settings.InitialSceneMode;nextFlipAt=FlipInterval;Refresh();}
 void Update(){
 if(!Session.IsPlaying||Session.IsPuzzle)return;
 float interval=FlipInterval;
 while(Session.Now>=nextFlipAt){nextFlipAt+=interval;ToggleMode();}
 }
 public void ToggleMode(){if(!Session.IsPlaying||Session.IsPuzzle)return;CurrentMode=CurrentMode==SceneMode.Heaven?SceneMode.Hell:SceneMode.Heaven;Refresh();ModeChanged?.Invoke(CurrentMode);}
 void Refresh(){Camera.backgroundColor=CurrentMode==SceneMode.Heaven?new Color(.13f,.23f,.34f):new Color(.19f,.045f,.09f);if(Backdrop)Backdrop.color=Camera.backgroundColor;}
} }
