using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PointerFinder;

/// <summary>Global low-level mouse hook. Needs a message loop on the creating thread (the WPF dispatcher provides it).</summary>
sealed class MouseHook : IDisposable
{
    readonly Native.HookProc _proc;   // kept in a field so the delegate is not garbage collected
    nint _hook;

    public event Action<int, int>? Moved;

    public MouseHook()
    {
        _proc = Callback;
        using var module = Process.GetCurrentProcess().MainModule!;
        _hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _proc, Native.GetModuleHandle(module.ModuleName), 0);
        if (_hook == 0) throw new InvalidOperationException("Could not install the mouse hook (error " + Marshal.GetLastWin32Error() + ").");
    }

    nint Callback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && wParam == Native.WM_MOUSEMOVE)
        {
            var data = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
            Moved?.Invoke(data.pt.X, data.pt.Y);
        }
        return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != 0) { Native.UnhookWindowsHookEx(_hook); _hook = 0; }
    }
}
