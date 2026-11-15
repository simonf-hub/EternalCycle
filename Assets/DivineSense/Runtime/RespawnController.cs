using UnityEngine;
namespace DivineSense { public sealed class RespawnController : MonoBehaviour {
 public PlayerContext Owner; public Transform Marker; float deadline;
 public Vector2 Position=>Marker.position;
 public float Remaining=>Mathf.Max(0,deadline-Owner.Session.Now);
 public void Begin(){SetPosition(Owner.Session.World.VisibleBounds.center);deadline=Owner.Session.Now+Owner.Settings.RespawnDuration;}
 public void SetPosition(Vector2 p){Rect r=Owner.Session.World.VisibleBounds;Marker.position=new Vector3(Mathf.Clamp(p.x,r.xMin+.25f,r.xMax-.25f),Mathf.Clamp(p.y,r.yMin+.25f,r.yMax-.25f),0);}
 void Update(){bool active=Owner.Session.IsPlaying&&Owner.Life.State==LifeState.RespawnPlacement;Marker.gameObject.SetActive(active);if(!active)return;SetPosition(Position+Owner.Input.Marker*Owner.Settings.MarkerSpeed*Time.deltaTime);if(Owner.Session.Now>=deadline){Owner.Life.CompleteRespawn(Position);Marker.gameObject.SetActive(false);}}
} }