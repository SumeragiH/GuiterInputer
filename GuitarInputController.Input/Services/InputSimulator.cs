using System.Runtime.InteropServices;
using GuitarInputController.Core.Enums;
using GuitarInputController.Input.Interfaces;
using GuitarInputController.Input.Native;

namespace GuitarInputController.Input.Services;

/// <summary>
/// 基于 Windows SendInput API 的键盘鼠标模拟器实现
/// </summary>
public class InputSimulator : IInputSimulator
{
    public void KeyDown(ushort virtualKeyCode)
    {
        SendKeyInput(virtualKeyCode, NativeMethods.KEYEVENTF_KEYDOWN);
    }

    public void KeyUp(ushort virtualKeyCode)
    {
        SendKeyInput(virtualKeyCode, NativeMethods.KEYEVENTF_KEYUP);
    }

    public void KeyPress(ushort virtualKeyCode)
    {
        KeyDown(virtualKeyCode);
        Thread.Sleep(10); // 脉冲间隔确保应用能识别
        KeyUp(virtualKeyCode);
    }

    public void SendCombination(IEnumerable<ushort> modifierKeys, ushort keyCode)
    {
        // 按下所有修饰键
        foreach (var mod in modifierKeys)
        {
            KeyDown(mod);
        }

        Thread.Sleep(10);

        // 按下并释放目标键
        KeyPress(keyCode);

        Thread.Sleep(10);

        // 释放所有修饰键（逆序）
        foreach (var mod in modifierKeys.Reverse())
        {
            KeyUp(mod);
        }
    }

    public void SendCombination(IEnumerable<string> modifierKeys, string keyCode)
    {
        var modVks = new List<ushort>();
        foreach (var mod in modifierKeys)
        {
            var vk = ModifierStringToVirtualKey(mod);
            if (vk.HasValue)
                modVks.Add(vk.Value);
        }

        var keyVk = StringToVirtualKey(keyCode);
        if (keyVk.HasValue && modVks.Count > 0)
        {
            SendCombination(modVks, keyVk.Value);
        }
    }

    public void MouseClick(MouseButtonType button)
    {
        MouseDown(button);
        Thread.Sleep(10);
        MouseUp(button);
    }

    public void MouseDown(MouseButtonType button)
    {
        (uint flags, uint mouseData) = GetMouseDownParams(button);
        SendMouseInput(flags, mouseData);
    }

    public void MouseUp(MouseButtonType button)
    {
        (uint flags, uint mouseData) = GetMouseUpParams(button);
        SendMouseInput(flags, mouseData);
    }

    private static (uint flags, uint mouseData) GetMouseDownParams(MouseButtonType button)
    {
        return button switch
        {
            MouseButtonType.Left => (NativeMethods.MOUSEEVENTF_LEFTDOWN, 0u),
            MouseButtonType.Right => (NativeMethods.MOUSEEVENTF_RIGHTDOWN, 0u),
            MouseButtonType.Middle => (NativeMethods.MOUSEEVENTF_MIDDLEDOWN, 0u),
            MouseButtonType.XButton1 => (NativeMethods.MOUSEEVENTF_XDOWN, NativeMethods.XBUTTON1),
            MouseButtonType.XButton2 => (NativeMethods.MOUSEEVENTF_XDOWN, NativeMethods.XBUTTON2),
            _ => (0u, 0u)
        };
    }

    private static (uint flags, uint mouseData) GetMouseUpParams(MouseButtonType button)
    {
        return button switch
        {
            MouseButtonType.Left => (NativeMethods.MOUSEEVENTF_LEFTUP, 0u),
            MouseButtonType.Right => (NativeMethods.MOUSEEVENTF_RIGHTUP, 0u),
            MouseButtonType.Middle => (NativeMethods.MOUSEEVENTF_MIDDLEUP, 0u),
            MouseButtonType.XButton1 => (NativeMethods.MOUSEEVENTF_XUP, NativeMethods.XBUTTON1),
            MouseButtonType.XButton2 => (NativeMethods.MOUSEEVENTF_XUP, NativeMethods.XBUTTON2),
            _ => (0u, 0u)
        };
    }

    public bool RegisterHotKey(IntPtr windowHandle, int id, uint modKeys, uint virtualKey)
    {
        return NativeMethods.RegisterHotKey(windowHandle, id, modKeys, virtualKey);
    }

    public bool UnregisterHotKey(IntPtr windowHandle, int id)
    {
        return NativeMethods.UnregisterHotKey(windowHandle, id);
    }

    public ushort? StringToVirtualKey(string keyString)
    {
        return NativeMethods.KeyStringToVk(keyString);
    }

    public ushort? ModifierStringToVirtualKey(string modifierString)
    {
        return NativeMethods.ModifierStringToVk(modifierString);
    }

    #region Private Helpers

    private static void SendKeyInput(ushort virtualKeyCode, uint flags)
    {
        var inputs = new NativeMethods.INPUT[1];
        inputs[0].type = NativeMethods.INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = virtualKeyCode;
        inputs[0].u.ki.wScan = 0;
        inputs[0].u.ki.dwFlags = flags;
        inputs[0].u.ki.time = 0;
        inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

        NativeMethods.SendInput(1, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendMouseInput(uint flags, uint mouseData)
    {
        var inputs = new NativeMethods.INPUT[1];
        inputs[0].type = NativeMethods.INPUT_MOUSE;
        inputs[0].u.mi.dx = 0;
        inputs[0].u.mi.dy = 0;
        inputs[0].u.mi.mouseData = mouseData;
        inputs[0].u.mi.dwFlags = flags;
        inputs[0].u.mi.time = 0;
        inputs[0].u.mi.dwExtraInfo = IntPtr.Zero;

        NativeMethods.SendInput(1, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    #endregion
}
