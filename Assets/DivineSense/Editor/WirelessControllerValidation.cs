using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DivineSense;
using EternalCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.HID;
namespace DivineSenseEditor
{
 public static class WirelessControllerValidation
 {
  [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("WirelessValidation",false)){
   InputSystem.settings=UnityEngine.Object.Instantiate(InputSystem.settings);
   InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   EditorApplication.update+=Attach;
  }}
  public static void Run(){SessionState.SetBool("WirelessValidation",true);EditorSceneManager.OpenScene("Assets/Scenes/DivineSense_Arena.unity");EditorApplication.isPlaying=true;}
  static void Attach(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;EditorApplication.update-=Attach;new GameObject("Wireless HID Validation").AddComponent<WirelessControllerValidationRunner>();}
 }
 [StructLayout(LayoutKind.Explicit,Size=8)]
 public struct WirelessHidState : IInputStateTypeInfo
 {
  public FourCC format=>new FourCC('H','I','D');
  [FieldOffset(0)]public byte x;
  [FieldOffset(1)]public byte y;
  [FieldOffset(2)]public byte hat;
  [FieldOffset(4)]public ushort buttons;
  public static WirelessHidState Neutral=>new WirelessHidState{x=128,y=128,hat=8};
 }
 public sealed class WirelessControllerValidationRunner : MonoBehaviour
 {
  Joystick first,second;PlayerContext a,b;int frame,stage;double started;
  static Joystick AddWireless(){
   var elements=new List<HID.HIDElementDescriptor>();
   for(int i=0;i<2;i++)elements.Add(new HID.HIDElementDescriptor{usagePage=HID.UsagePage.GenericDesktop,usage=(int)HID.GenericDesktop.X+i,reportType=HID.HIDReportType.Input,reportSizeInBits=8,reportOffsetInBits=i*8,logicalMin=0,logicalMax=255,flags=HID.HIDElementFlags.Variable});
   elements.Add(new HID.HIDElementDescriptor{usagePage=HID.UsagePage.GenericDesktop,usage=(int)HID.GenericDesktop.HatSwitch,reportType=HID.HIDReportType.Input,reportSizeInBits=4,reportOffsetInBits=16,logicalMin=0,logicalMax=7,flags=HID.HIDElementFlags.Variable|HID.HIDElementFlags.NullState});
   for(int i=0;i<12;i++)elements.Add(new HID.HIDElementDescriptor{usagePage=HID.UsagePage.Button,usage=i+1,reportType=HID.HIDReportType.Input,reportSizeInBits=1,reportOffsetInBits=32+i,logicalMin=0,logicalMax=1,flags=HID.HIDElementFlags.Variable});
   var descriptor=new HID.HIDDeviceDescriptor{vendorId=11720,productId=12315,usagePage=HID.UsagePage.GenericDesktop,usage=(int)HID.GenericDesktop.Gamepad,inputReportSize=8,elements=elements.ToArray()};
   return (Joystick)InputSystem.AddDevice(new InputDeviceDescription{interfaceName="HID",manufacturer="8BitDo",product="Ultimate 2C Wireless Test",capabilities=descriptor.ToJson()});
  }
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  void Start(){started=EditorApplication.timeSinceStartup;
   foreach(var device in InputSystem.devices)if(device is Gamepad||device is Joystick)InputSystem.DisableDevice(device);
   first=AddWireless();second=AddWireless();
   InputSystem.QueueStateEvent(first,WirelessHidState.Neutral);InputSystem.QueueStateEvent(second,WirelessHidState.Neutral);
   var session=FindFirstObjectByType<GameSession>();FindFirstObjectByType<HUDController>().enabled=false;
   FindFirstObjectByType<MatchSetupController>().Begin(ActiveKind.Sight,MovementKind.Touch,ActiveKind.Sound,MovementKind.Taste);
   a=session.Registry.Players[0];b=session.Registry.Players[1];
   foreach(var p in session.Registry.Players){p.Motor.enabled=false;p.Motor.Body.simulated=false;p.Life.enabled=false;}
   FindFirstObjectByType<GodDirector>().enabled=false;
  }
  void LateUpdate(){try{
   if(EditorApplication.timeSinceStartup-started>30)throw new Exception("Wireless validation timeout");
   if(++frame%4!=0)return;
   var state=WirelessHidState.Neutral;
   switch(stage++){
    case 0:
     Check(LocalGamepads.ForPlayer(0)?.Device==first&&LocalGamepads.ForPlayer(1)?.Device==second,"HID Joystick assignment failed");
     state.x=240;state.buttons=1;InputSystem.QueueStateEvent(first,state);break;
    case 1:
     Check(a.Input.Horizontal>.5f&&Mathf.Abs(b.Input.Horizontal)<.05f,"Wireless HID movement isolation failed");
     Check(a.Input.Held&&a.Input.TakeJump()&&!b.Input.TakeJump(),"Wireless HID jump/glide failed");
     InputSystem.QueueStateEvent(first,state);break;
    case 2:
     state.buttons=2;InputSystem.QueueStateEvent(second,state);break;
    case 3:
     Check(b.Input.TakeAbility()&&!a.Input.TakeAbility(),"Wireless HID ability failed");
     InputSystem.QueueStateEvent(second,state);a.Input.Placement(true);state.hat=0;InputSystem.QueueStateEvent(first,state);break;
    case 4:
     Check(a.Input.Marker.y>.5f,"Wireless HID D-pad respawn failed");
     Check(LocalGamepads.ForPlayer(0).dpad.up.wasPressedThisFrame||LocalGamepads.ForPlayer(0).dpad.up.isPressed,"Wireless HID D-pad setup path failed");
     a.Input.Placement(false);InputSystem.RemoveDevice(first);
     Check(LocalGamepads.ForPlayer(1)?.Device==second,"Wireless disconnect reassigned Devil");
     first=AddWireless();state.x=240;InputSystem.QueueStateEvent(first,state);break;
    case 5:
     Check(LocalGamepads.ForPlayer(0)?.Device==first&&a.Input.Horizontal>.5f,"Wireless HID reconnect failed");
     Debug.Log("WIRELESS_HID_VALIDATION_OK: generated 8BitDo-style HID Joysticks, separate players, movement, jump/glide, ability, D-pad, reconnect");SessionState.SetBool("WirelessValidation",false);EditorApplication.Exit(0);break;
   }
  }catch(Exception e){Debug.LogException(e);SessionState.SetBool("WirelessValidation",false);EditorApplication.Exit(1);}}
 }
}
