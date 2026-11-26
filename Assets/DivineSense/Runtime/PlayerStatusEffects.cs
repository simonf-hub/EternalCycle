using UnityEngine;
namespace DivineSense { public sealed class PlayerStatusEffects : MonoBehaviour {
 public PlayerContext Owner; public float HiddenUntil,GrownUntil,ReversedUntil;
 public bool Hidden=>Owner.Session.Now<HiddenUntil;
 public bool Grown=>Owner.Session.Now<GrownUntil;
 public bool Reversed=>Owner.Session.Now<ReversedUntil;
 const float BombFreezeDuration=3f;
 float frozenUntil;
 public float FreezeRemaining=>Owner.CanAct?Mathf.Max(0,frozenUntil-Owner.Session.Now):0;
 public bool Frozen=>FreezeRemaining>0;
 public void ApplyFreeze()
 {
     if (!Owner.CanAct) return;
     frozenUntil = Owner.Session.Now + BombFreezeDuration;
     Owner.Motor.LockHorizontalForStare();
     Owner.Input.DiscardActions();
 }
 public void ClearFreeze()
 {
     frozenUntil = 0f;
     if (Owner && Owner.Motor) Owner.Motor.ReleaseStareLock();
 }
 public void Clear(){HiddenUntil=GrownUntil=ReversedUntil=0;ClearFreeze();}
 void Update()
 {
     if (frozenUntil > 0f && Owner.Session.Now >= frozenUntil)
         ClearFreeze();
 }
 void OnDisable() => ClearFreeze();
} }
