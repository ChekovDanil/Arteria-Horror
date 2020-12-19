using System;
using UnityEngine;
using ThunderWire.CrossPlatform.Input;

public class CrossPlatformSprites : ScriptableObject
{
    [Serializable]
    public class PS4Sprites
    {
        public Sprite PS4Gamepad;
        public Sprite DpadUp;
        public Sprite DpadDown;
        public Sprite DpadLeft;
        public Sprite DpadRight;
        public Sprite Triangle;
        public Sprite Circle;
        public Sprite Cross;
        public Sprite Square;
        public Sprite LeftStick;
        public Sprite RightStick;
        public Sprite L1;
        public Sprite R1;
        public Sprite L2;
        public Sprite R2;
        public Sprite Options;
        public Sprite Share;
        public Sprite Touchpad;
    }

    [Serializable]
    public class XboxOneSprites
    {
        public Sprite XboxGamepad;
        public Sprite DpadUp;
        public Sprite DpadDown;
        public Sprite DpadLeft;
        public Sprite DpadRight;
        public Sprite Y;
        public Sprite B;
        public Sprite A;
        public Sprite X;
        public Sprite LeftStick;
        public Sprite RightStick;
        public Sprite LB;
        public Sprite RB;
        public Sprite LT;
        public Sprite RT;
        public Sprite Menu;
        public Sprite ChangeView;
    }

    [Serializable]
    public class MouseSprites
    {
        public Sprite MouseLeft;
        public Sprite MouseMiddle;
        public Sprite MouseRight;
    }

    [Serializable]
    public class KeyboardSprites
    {
        public Sprite Key0;
        public Sprite Key1;
        public Sprite Key2;
        public Sprite Key3;
        public Sprite Key4;
        public Sprite Key5;
        public Sprite Key6;
        public Sprite Key7;
        public Sprite Key8;
        public Sprite Key9;
    }

    public PS4Sprites PS4;
    public XboxOneSprites XboxOne;
    public MouseSprites Mouse;
    public KeyboardSprites Keyboard;

    /// <summary>
    /// Get Control Sprite for Specific Platform
    /// </summary>
    public Sprite GetSprite(string control, Platform platform)
    {
        if (platform != Platform.PC)
        {
            if (Enum.TryParse(control, out CrossGamepadControl c))
            {
                return GetSprite(c, platform);
            }
        }
        else if (platform == Platform.PC && Enum.TryParse(control, out KeyMouse k))
        {
            return GetKeyboardSprite(k);
        }

        return default;
    }

    /// <summary>
    /// Get Control Sprite for Specific Console Platform
    /// </summary>
    public Sprite GetSprite(CrossGamepadControl control, Platform platform)
    {
        if(platform == Platform.PS4)
        {
            switch (control)
            {
                case CrossGamepadControl.DpadUp:
                    return PS4.DpadUp;
                case CrossGamepadControl.DpadDown:
                    return PS4.DpadDown;
                case CrossGamepadControl.DpadLeft:
                    return PS4.DpadLeft;
                case CrossGamepadControl.DpadRight:
                    return PS4.DpadRight;
                case CrossGamepadControl.FaceUp:
                    return PS4.Triangle;
                case CrossGamepadControl.FaceRight:
                    return PS4.Circle;
                case CrossGamepadControl.FaceDown:
                    return PS4.Cross;
                case CrossGamepadControl.FaceLeft:
                    return PS4.Square;
                case CrossGamepadControl.LeftStick:
                    return PS4.LeftStick;
                case CrossGamepadControl.RightStick:
                    return PS4.RightStick;
                case CrossGamepadControl.LeftShoulder:
                    return PS4.L1;
                case CrossGamepadControl.RightShoulder:
                    return PS4.R1;
                case CrossGamepadControl.LeftTrigger:
                    return PS4.L2;
                case CrossGamepadControl.RightTrigger:
                    return PS4.R2;
                case CrossGamepadControl.Start:
                    return PS4.Options;
                case CrossGamepadControl.Select:
                    return PS4.Share;
                case CrossGamepadControl.PS4TouchpadButton:
                    return PS4.Touchpad;
            }
        }
        else if (platform == Platform.XboxOne)
        {
            switch (control)
            {
                case CrossGamepadControl.DpadUp:
                    return XboxOne.DpadUp;
                case CrossGamepadControl.DpadDown:
                    return XboxOne.DpadDown;
                case CrossGamepadControl.DpadLeft:
                    return XboxOne.DpadLeft;
                case CrossGamepadControl.DpadRight:
                    return XboxOne.DpadRight;
                case CrossGamepadControl.FaceUp:
                    return XboxOne.Y;
                case CrossGamepadControl.FaceRight:
                    return XboxOne.B;
                case CrossGamepadControl.FaceDown:
                    return XboxOne.A;
                case CrossGamepadControl.FaceLeft:
                    return XboxOne.X;
                case CrossGamepadControl.LeftStick:
                    return XboxOne.LeftStick;
                case CrossGamepadControl.RightStick:
                    return XboxOne.RightStick;
                case CrossGamepadControl.LeftShoulder:
                    return XboxOne.LB;
                case CrossGamepadControl.RightShoulder:
                    return XboxOne.RB;
                case CrossGamepadControl.LeftTrigger:
                    return XboxOne.LT;
                case CrossGamepadControl.RightTrigger:
                    return XboxOne.RT;
                case CrossGamepadControl.Start:
                    return XboxOne.Menu;
                case CrossGamepadControl.Select:
                    return XboxOne.ChangeView;
                case CrossGamepadControl.PS4TouchpadButton:
                    return PS4.Touchpad;
            }
        }

        return default;
    }

    /// <summary>
    /// Get Control Sprite for Mouse
    /// </summary>
    public Sprite GetMouseSprite(KeyMouse control)
    {
        switch (control)
        {
            case KeyMouse.MouseLeft:
                return Mouse.MouseLeft;
            case KeyMouse.MouseMiddle:
                return Mouse.MouseMiddle;
            case KeyMouse.MouseRight:
                return Mouse.MouseRight;
            default:
                break;
        }

        return default;
    }

    /// <summary>
    /// Get Control Sprite for Keyboard
    /// Only Numbers 0-9
    /// </summary>
    public Sprite GetKeyboardSprite(KeyMouse control)
    {
        switch (control)
        {
            case KeyMouse.Digit0:
                return Keyboard.Key0;
            case KeyMouse.Digit1:
                return Keyboard.Key1;
            case KeyMouse.Digit2:
                return Keyboard.Key2;
            case KeyMouse.Digit3:
                return Keyboard.Key3;
            case KeyMouse.Digit4:
                return Keyboard.Key4;
            case KeyMouse.Digit5:
                return Keyboard.Key5;
            case KeyMouse.Digit6:
                return Keyboard.Key6;
            case KeyMouse.Digit7:
                return Keyboard.Key7;
            case KeyMouse.Digit8:
                return Keyboard.Key8;
            case KeyMouse.Digit9:
                return Keyboard.Key9;
        }
        return default;
    }

    /// <summary>
    /// Get Control Sprite for Mouse
    /// </summary>
    public Sprite GetMouseSprite(string control)
    {
        if(Enum.TryParse(control, out KeyMouse mouse))
        {
            return GetMouseSprite(mouse);
        }

        return default;
    }
}
