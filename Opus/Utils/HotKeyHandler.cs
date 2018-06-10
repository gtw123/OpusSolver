using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.FormattableString;

namespace Opus
{
    public class HotKeyHandler : IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
        [Flags]
        public enum ModifierKeys : uint
        {
            Alt     = 1,
            Control = 2,
            Shift   = 4,
            Win     = 8
        }

        private const uint NoRepeat = 0x4000;

        private IntPtr m_hWnd;

        private struct HotKeyAction
        {
            public int ID;
            public Keys Key;
            public ModifierKeys ModifierKeys;
            public Action<Keys, ModifierKeys> Action;
        }
        private Dictionary<uint, HotKeyAction> m_keyActions = new Dictionary<uint, HotKeyAction>();

        private static int sm_nextID;

        public HotKeyHandler(Form form)
        {
            m_hWnd = form.Handle;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (uint i = 1; i <= m_keyActions.Count; i++)
                {
                    NativeMethods.UnregisterHotKey(m_hWnd, i);
                }
            }
        }

        public void RegisterHotKey(Keys key, ModifierKeys modifiers, Action<Keys, ModifierKeys> action)
        {
            uint param = GetKeyParam(key, modifiers);
            if (m_keyActions.ContainsKey(param))
            {
                throw new InvalidOperationException(Invariant($"Hotkey ({key}, {modifiers}) is already registered."));
            }

            int id = sm_nextID++;
            if (!NativeMethods.RegisterHotKey(m_hWnd, id, (uint)modifiers | NoRepeat, (uint)key))
            {
                throw new InvalidOperationException(Invariant($"Couldn't register hotkey  ({key}, {modifiers}): error {Marshal.GetLastWin32Error()}"));
            }

            m_keyActions[param] = new HotKeyAction { ID = id, Key = key, ModifierKeys = modifiers, Action = action };
        }

        public void UnregisterHotKey(Keys key, ModifierKeys modifiers)
        {
            uint param = GetKeyParam(key, modifiers);
            if (m_keyActions.TryGetValue(param, out var action))
            {
                m_keyActions.Remove(param);
                if (!NativeMethods.UnregisterHotKey(m_hWnd, (uint)action.ID))
                {
                    throw new InvalidOperationException(Invariant($"Couldn't unregister register hotkey  ({key}, {modifiers}): error {Marshal.GetLastWin32Error()}"));
                }
            }
        }

        private static uint GetKeyParam(Keys key, ModifierKeys modifiers)
        {
            return (uint)key << 16 | (uint)modifiers;
        }

        public void WndProc(Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                uint param = (uint)m.LParam;
                if (m_keyActions.TryGetValue(param, out var action))
                {
                    action.Action(action.Key, action.ModifierKeys);
                }
            }
        }
    }
}
