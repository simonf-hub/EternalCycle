using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EternalCycle
{
    // Bluetooth HID controllers may be Joysticks even when shaped like standard gamepads.
    public sealed class LocalController
    {
        public InputDevice Device { get; }
        public ControllerStick leftStick, rightStick, dpad;
        public ControllerButton buttonSouth, buttonEast, buttonWest, buttonNorth;
        public ControllerButton startButton, selectButton, leftShoulder, rightShoulder;
        public ControllerAxis rightTrigger;

        public LocalController(InputDevice device)
        {
            Device = device;
            if (device is Gamepad pad)
            {
                leftStick = new ControllerStick(pad.leftStick); rightStick = new ControllerStick(pad.rightStick);
                dpad = new ControllerStick(pad.dpad);
                buttonSouth = new ControllerButton(pad.buttonSouth); buttonEast = new ControllerButton(pad.buttonEast);
                buttonWest = new ControllerButton(pad.buttonWest); buttonNorth = new ControllerButton(pad.buttonNorth);
                startButton = new ControllerButton(pad.startButton); selectButton = new ControllerButton(pad.selectButton);
                leftShoulder = new ControllerButton(pad.leftShoulder); rightShoulder = new ControllerButton(pad.rightShoulder);
                rightTrigger = new ControllerAxis(pad.rightTrigger);
            }
            else if (device is Joystick joystick)
            {
                leftStick = new ControllerStick(joystick.stick);
                dpad = new ControllerStick(joystick.hatswitch ?? device.TryGetChildControl<Vector2Control>("hat"));
                rightStick = new ControllerStick(device.TryGetChildControl<Vector2Control>("rightStick"));
                // Unity's generated HID layout names button usage 1 "trigger", then button2, etc.
                buttonSouth = new ControllerButton(joystick.trigger); buttonEast = Button("button2");
                buttonWest = Button("button3"); buttonNorth = Button("button4");
                leftShoulder = Button("button5"); rightShoulder = Button("button6");
                rightTrigger = new ControllerAxis(device.TryGetChildControl<AxisControl>("rightTrigger")
                    ?? device.TryGetChildControl<ButtonControl>("button8"));
                selectButton = Button("button9"); startButton = Button("button10");
            }
        }
        private ControllerButton Button(string name) => new ControllerButton(Device.TryGetChildControl<ButtonControl>(name));
    }

    public readonly struct ControllerButton
    {
        private readonly ButtonControl control;
        public ControllerButton(ButtonControl control) { this.control = control; }
        public bool wasPressedThisFrame => control != null && control.wasPressedThisFrame;
        public bool isPressed => control != null && control.isPressed;
    }
    public readonly struct ControllerAxis
    {
        private readonly AxisControl control;
        public ControllerAxis(AxisControl control) { this.control = control; }
        public float ReadValue() => control?.ReadValue() ?? 0;
    }
    public readonly struct ControllerStick
    {
        private readonly Vector2Control control;
        public ControllerStick(Vector2Control control) { this.control = control; }
        public Vector2 ReadValue() => control?.ReadValue() ?? Vector2.zero;
        public ControllerAxis y => new ControllerAxis(control?.TryGetChildControl<AxisControl>("y"));
        public ControllerButton up => Direction("up");
        public ControllerButton down => Direction("down");
        public ControllerButton left => Direction("left");
        public ControllerButton right => Direction("right");
        private ControllerButton Direction(string name) => new ControllerButton(control?.TryGetChildControl<ButtonControl>(name));
    }

    public static class LocalGamepads
    {
        private static readonly LocalController[] slots = new LocalController[2];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { slots[0] = slots[1] = null; }
        private static bool Supported(InputDevice device) => device.enabled && device.added && (device is Gamepad || device is Joystick);
        public static LocalController ForPlayer(int player)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && !Supported(slots[i].Device)) slots[i] = null;
            foreach (var device in InputSystem.devices)
            {
                if (!Supported(device) || slots[0]?.Device == device || slots[1]?.Device == device) continue;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] == null)
                    {
                        slots[i] = new LocalController(device);
                        Debug.Log("Controller " + (i + 1) + ": " + device.displayName + " [" + device.layout + "]");
                        break;
                    }
            }
            return player >= 0 && player < slots.Length ? slots[player] : null;
        }
        public static string Status
        {
            get
            {
                var a = ForPlayer(0); var b = ForPlayer(1);
                return "Angel: " + (a == null ? "no controller detected" : a.Device.displayName)
                    + "   |   Devil: " + (b == null ? "no controller detected" : b.Device.displayName);
            }
        }
        public static bool RestartPressed
        {
            get { for (int i = 0; i < 2; i++) if (ForPlayer(i)?.selectButton.wasPressedThisFrame == true) return true; return false; }
        }
        public static bool ConfirmPressed
        {
            get
            {
                for (int i = 0; i < 2; i++)
                {
                    var pad = ForPlayer(i);
                    if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
                }
                return false;
            }
        }
    }
}
