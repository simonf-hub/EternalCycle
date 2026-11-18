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
 public void ApplyFreeze(){
 if(!Owner.CanAct||Grown)return;
 frozenUntil=Owner.Session.Now+BombFreezeDuration;
 Owner.Motor.StopHorizontalMotion();
 Owner.Input.DiscardActions();
 }
 public void ClearFreeze(){frozenUntil=0;}
 public void Clear(){HiddenUntil=GrownUntil=ReversedUntil=0;ClearFreeze();}
 void OnDisable(){if(Owner&&Owner.Motor)ClearFreeze();}
} }