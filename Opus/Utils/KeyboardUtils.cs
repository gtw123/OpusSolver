using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Forms;
using static Opus.NativeMethods;
using static System.FormattableString;

namespace Opus
{
    public static class KeyboardUtils
    {
        private static HashSet<Keys> sm_keysDown = new HashSet<Keys>();

        /// <summary>
        /// Checks whether a specified key is currently down, regardless of whether any events
        /// have been handled.
        public static bool IsKeyDown(Keys key)
        {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        /// <summary>
        /// Simulates the specified keys being pressed, then released.
        /// </summary>
        public static void KeyPress(params Keys[] keys)
        {
            KeyDown(keys);
            ThreadUtils.SleepOrAbort(50);
            KeyUp(keys);
            ThreadUtils.SleepOrAbort(50);
        }

        /// <summary>
        /// Simulates the specified keys being pressed and held down.
        /// </summary>
        public static void KeyDown(params Keys[] keys)
        {
            var inputs = keys.Select(k => CreateKeyboardInput(k, KeyboardFlag.ScanCode));
            SendKeyEvent(inputs.ToArray());
            sm_keysDown.UnionWith(keys);
        }

        /// <summary>
        /// Simulates the specified keys being released.
        /// </summary>
        public static void KeyUp(params Keys[] keys)
        {
            var inputs = keys.Select(k => CreateKeyboardInput(k, KeyboardFlag.KeyUp | KeyboardFlag.ScanCode));
            SendKeyEvent(inputs.ToArray());
            sm_keysDown.ExceptWith(keys);
        }

        /// <summary>
        /// Releases any keys that were previously pressed down via KeyDown.
        /// </summary>
        public static void ClearKeysDown()
        {
            KeyUp(sm_keysDown.ToArray());
        }

        private static bool IsExtendedKey(Keys key)
        {
            return key == Keys.ControlKey;
        }

        private static INPUT CreateKeyboardInput(Keys key, KeyboardFlag flags)
        {
            if (IsExtendedKey(key))
            {
                flags |= KeyboardFlag.ExtendedKey;
            }

            return new INPUT
            {
                Type = (uint)InputType.Keyboard,
                Data = new InputUnion
                {
                    KeyboardInput = new KEYBDINPUT
                    {
                        KeyCode = 0,
                        Scan = GetScanCode(key),
                        Flags = (UInt32)flags,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        private static ushort GetScanCode(Keys key)
        {
            return (ushort)MapVirtualKey((uint)key, MapType.VirtualKeyToScanCode);
        }

        private static void SendKeyEvent(INPUT[] inputs)
        {
            uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (result != inputs.Length)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(Invariant($"SendInput returned {result} instead of {inputs.Length}. Error: {error}."));
            }
        }     
    }
}
