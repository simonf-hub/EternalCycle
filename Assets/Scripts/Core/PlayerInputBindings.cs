using System;
using EternalCycle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MicroSense
{
    [Serializable]
    public struct PlayerInputBindings
    {
        public Key Left;
        public Key Right;
        public Key Up;
        public Key Down;
        public Key AbilityPrimary;
        public Key AbilitySecondary;

        public PlayerInputBindings(
            Key left,
            Key right,
            Key up,
            Key down,
            Key abilityPrimary,
            Key abilitySecondary)
        {
            Left = left;
            Right = right;
            Up = up;
            Down = down;
            AbilityPrimary = abilityPrimary;
            AbilitySecondary = abilitySecondary;
        }

        public float ReadHorizontal()
        {
            float keyboard=ReadAxis(Left, Right);
            float controller=ControllerDirection.x;
            return Mathf.Abs(controller)>Mathf.Abs(keyboard)?controller:keyboard;
        }

        public float ReadVertical()
        {
            float keyboard=ReadAxis(Down, Up);
            float controller=ControllerDirection.y;
            return Mathf.Abs(controller)>Mathf.Abs(keyboard)?controller:keyboard;
        }

        private LocalController Controller => LocalGamepads.ForPlayer(Left == Key.LeftArrow ? 1 : 0);

        private Vector2 ControllerDirection
        {
            get
            {
                var pad=Controller;
                if(pad==null)return Vector2.zero;
                Vector2 stick=pad.leftStick.ReadValue(),dpad=pad.dpad.ReadValue();
                return dpad.sqrMagnitude>stick.sqrMagnitude?dpad:stick;
            }
        }

        public bool UpPressedThisFrame => WasPressedThisFrame(Up) || Controller?.buttonSouth.wasPressedThisFrame == true;

        public bool PrimaryPressedThisFrame => WasPressedThisFrame(AbilityPrimary) || Controller?.buttonEast.wasPressedThisFrame == true;

        public bool SecondaryPressedThisFrame => WasPressedThisFrame(AbilitySecondary) || Controller?.buttonWest.wasPressedThisFrame == true;

        private static float ReadAxis(Key negative, Key positive)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return 0f;
            }

            float value = 0f;
            if (keyboard[negative].isPressed)
            {
                value -= 1f;
            }

            if (keyboard[positive].isPressed)
            {
                value += 1f;
            }

            return value;
        }

        private static bool WasPressedThisFrame(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }
    }
}
