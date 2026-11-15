using System;
using UnityEngine;
namespace DivineSense { public sealed class FollowerCounter : MonoBehaviour {
 public PlayerContext Owner; public int Total {get;private set;}
 public event Action<int,int,int> Changed;
 public void Initialize(){Total=Owner.Settings.StartingFollowers;}
 public static int AfterDeath(int total,float fraction) => Mathf.Max(0,total-Mathf.CeilToInt(total*fraction));
 public void Add(int amount){if(!Owner.CanAct)return;int old=Total;Total=Mathf.Max(0,Total+amount);Changed?.Invoke(Owner.Id,Total,Total-old);}
 public void ApplyDeathPenalty(float fraction){if(!Owner.Session.IsPlaying)return;int old=Total;Total=AfterDeath(Total,fraction);Changed?.Invoke(Owner.Id,Total,Total-old);}
} }