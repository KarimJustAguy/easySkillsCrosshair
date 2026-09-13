using System.Runtime.InteropServices;
using System.Windows.Interop;
using easySkillsCrosshair.Core.Reactions;

namespace easySkillsCrosshair.Overlay.Input;

/// <summary>
/// Feeds mouse/keyboard state into a <see cref="ReactionEngine"/> via the Win32 Raw Input API
/// with RIDEV_INPUTSINK. Deliberately NOT a WH_MOUSE_LL/WH_KEYBOARD_LL hook: low-level hooks run
/// synchronously inside the input pipeline and can add input latency; Raw Input only receives
/// asynchronous copies and never delays, blocks or modifies what the game sees. Nothing is
/// injected and no other process is touched — anti-cheat-neutral by design.
///
/// Input is ignored while this app itself is in the foreground, so clicking around in the
/// editor doesn't make the overlay bloom.
/// </summary>
public sealed class RawInputListener : IDisposable
{
    private const int WM_INPUT = 0x00FF;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const uint RIDEV_REMOVE = 0x00000001;
    private const uint RIM_TYPEMOUSE = 0;
    private const uint RIM_TYPEKEYBOARD = 1;

    private const ushort RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
    private const ushort RI_MOUSE_LEFT_BUTTON_UP = 0x0002;
    private const ushort RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
    private const ushort RI_MOUSE_RIGHT_BUTTON_UP = 0x0008;
    private const ushort RI_KEY_BREAK = 0x0001;

    private const int BufferSize = 1024;

    private static readonly int[] MoveKeys = [0x57, 0x41, 0x53, 0x44]; // W A S D
    private static readonly int HeaderSize = Marshal.SizeOf<RAWINPUTHEADER>();

    private readonly HwndSource _source;
    private readonly ReactionEngine _engine;
    private readonly int _processId = Environment.ProcessId;

    // Allocated once: high-polling mice send ~1000 WM_INPUT/s, so no per-event allocation.
    private readonly IntPtr _buffer = Marshal.AllocHGlobal(BufferSize);

    private bool _registered;
    private bool _disposed;

    public RawInputListener(HwndSource source, ReactionEngine engine)
    {
        _source = source;
        _engine = engine;
    }

    public void Start()
    {
        if (_registered || _disposed) return;

        var devices = new[]
        {
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_INPUTSINK, hwndTarget = _source.Handle }, // mouse
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_INPUTSINK, hwndTarget = _source.Handle }, // keyboard
        };

        if (RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            _source.AddHook(WndProc);
            _registered = true;
        }
    }

    public void Stop()
    {
        if (!_registered) return;

        var devices = new[]
        {
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero },
        };
        RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        _source.RemoveHook(WndProc);
        _registered = false;
        _engine.Reset();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        Marshal.FreeHGlobal(_buffer);
        _disposed = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            if (IsOwnProcessForeground())
            {
                _engine.Reset();
            }
            else
            {
                ProcessRawInput(lParam);
            }
        }

        // Never mark handled: DefWindowProc must still run for WM_INPUT cleanup.
        return IntPtr.Zero;
    }

    private void ProcessRawInput(IntPtr hRawInput)
    {
        uint size = 0;
        GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, (uint)HeaderSize);
        if (size == 0 || size > BufferSize) return;

        if (GetRawInputData(hRawInput, RID_INPUT, _buffer, ref size, (uint)HeaderSize) != size) return;

        var header = Marshal.PtrToStructure<RAWINPUTHEADER>(_buffer);
        var data = _buffer + HeaderSize;

        if (header.dwType == RIM_TYPEMOUSE)
        {
            // RAWMOUSE: usFlags(2) + padding(2), then usButtonFlags at offset 4.
            var buttons = (ushort)Marshal.ReadInt16(data, 4);
            if (buttons == 0) return; // pure movement — the common case, nothing to do

            var now = DateTime.UtcNow;
            if ((buttons & RI_MOUSE_LEFT_BUTTON_DOWN) != 0) _engine.SetFiring(true, now);
            if ((buttons & RI_MOUSE_LEFT_BUTTON_UP) != 0) _engine.SetFiring(false, now);
            if ((buttons & RI_MOUSE_RIGHT_BUTTON_DOWN) != 0) _engine.SetAiming(true);
            if ((buttons & RI_MOUSE_RIGHT_BUTTON_UP) != 0) _engine.SetAiming(false);
        }
        else if (header.dwType == RIM_TYPEKEYBOARD)
        {
            // RAWKEYBOARD: MakeCode(2), Flags(2) at offset 2, Reserved(2), VKey(2) at offset 6.
            var flags = (ushort)Marshal.ReadInt16(data, 2);
            int vkey = (ushort)Marshal.ReadInt16(data, 6);
            if (Array.IndexOf(MoveKeys, vkey) >= 0)
            {
                _engine.SetMoveKey(vkey, isDown: (flags & RI_KEY_BREAK) == 0);
            }
        }
    }

    private bool IsOwnProcessForeground()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return false;
        GetWindowThreadProcessId(foreground, out var pid);
        return pid == _processId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
}
