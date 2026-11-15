using System;
using UnityEngine;
namespace DivineSense { public sealed class CrumblingPlatform : MonoBehaviour {
 public PlatformScroller Scroller; public SpriteRenderer Visual; float deadline=-1;
 public event Action Started;
 void OnCollisionEnter2D(Collision2D c){var p=c.gameObject.GetComponent<PlayerContext>();if(deadline>=0||!p||!p.CanAct||p.Motor.Body.position.y<transform.position.y)return;bool landing=false;foreach(var contact in c.contacts)if(contact.normal.y<-.5f)landing=true;if(!landing)return;deadline=Scroller.Session.Now+Scroller.Session.Settings.CrumbleDelay;Started?.Invoke();}
 void Update(){if(deadline<0||!Scroller.Session.IsPlaying)return;Visual.color=Color.Lerp(new Color(.9f,.57f,.32f),Color.red,Mathf.PingPong(Scroller.Session.Now*5,1));if(Scroller.Session.Now>=deadline)Destroy(gameObject);}
} }