using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// Local helper for Deskflow 1.27. No changes to Deskflow's configuration.
internal static class Program
{
    internal const string AppName = "DeskflowAutoLock";
    internal static readonly string Folder = AppDomain.CurrentDomain.BaseDirectory;
    internal static readonly string LogPath = Path.Combine(Folder, "activity.log");
    internal const string StopName = @"Local\DeskflowAutoLock.Stop";
    internal const string PauseName = @"Local\DeskflowAutoLock.Pause";

    [STAThread]
    private static void Main(string[] args)
    {
        Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string arg = args.Length == 0 ? "" : args[0];
        if (arg == "--stop" || arg == "--disable" || arg == "--pause")
        {
            if (arg == "--disable") SetStartup(false);
            try { using (EventWaitHandle e = EventWaitHandle.OpenExisting(arg == "--pause" ? PauseName : StopName)) e.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            return;
        }
        if (arg == "--test-window") { Application.Run(new TestWindow()); return; }
        bool created;
        using (Mutex mutex = new Mutex(true, @"Local\DeskflowAutoLock.Instance", out created))
        {
            if (!created) return;
            using (Guard guard = new Guard())
            {
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { guard.Fail(e.Exception); };
                AppDomain.CurrentDomain.UnhandledException += delegate { guard.Release(); };
                Application.ApplicationExit += delegate { guard.Release(); };
                Application.Run(guard);
            }
        }
    }
    internal static bool StartupEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            return key != null && key.GetValue(AppName) != null;
    }
    internal static void SetStartup(bool value)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
        {
            if (value) key.SetValue(AppName, "\"" + Application.ExecutablePath + "\"");
            else key.DeleteValue(AppName, false);
        }
    }
    internal static void Log(string text)
    {
        try
        {
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 1024 * 1024)
                File.WriteAllText(LogPath, "");
            File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + text + Environment.NewLine);
        }
        catch (IOException) { }
    }
}

internal sealed class Guard : ApplicationContext
{
    private readonly NotifyIcon tray;
    private readonly ToolStripMenuItem enabledItem, startupItem, statusItem;
    private readonly System.Windows.Forms.Timer timer;
    private readonly EventWaitHandle stop = new EventWaitHandle(false, EventResetMode.AutoReset, Program.StopName);
    private readonly EventWaitHandle pause = new EventWaitHandle(false, EventResetMode.AutoReset, Program.PauseName);
    private readonly HotkeyWindow hotkey;
    private readonly string deskflowLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Deskflow", "deskflow-daemon.log");
    private bool enabled = true, locked, local = true, owned, releasing;
    private bool? pending;
    private DateTime pendingSince, lastCaptured = DateTime.MinValue;
    private IntPtr suppressedWindow;
    private long offset;
    private string partial = "";
    private string currentProcess = "";
    private bool logAvailable;
    private bool captured;
    private string captureDetail = "";
    private string lastSensor = "";

    internal Guard()
    {
        locked = Control.IsKeyLocked(Keys.Scroll);
        ReadLog(true);
        statusItem = new ToolStripMenuItem("Initializing") { Enabled = false };
        enabledItem = new ToolStripMenuItem("Protection enabled") { Checked = true, CheckOnClick = false };
        enabledItem.Click += delegate { Toggle(); };
        startupItem = new ToolStripMenuItem("Run at Windows sign-in") { Checked = Program.StartupEnabled() };
        startupItem.Click += delegate { Program.SetStartup(!Program.StartupEnabled()); startupItem.Checked = Program.StartupEnabled(); };
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add(statusItem);
        menu.Items.Add(enabledItem);
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, delegate { ExitThread(); });
        menu.Items.Add("Quit and disable startup", null, delegate { Program.SetStartup(false); ExitThread(); });
        tray = new NotifyIcon { Icon = SystemIcons.Shield, Text = "Deskflow: mouse confinement protection", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += delegate { Toggle(); };
        hotkey = new HotkeyWindow(Toggle);
        timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += delegate { Tick(); };
        timer.Start();
        Program.Log("START mode=mouse-confinement enabled=True locked=" + locked + " local=" + local);
        UpdateStatus();
    }

    private void Toggle()
    {
        enabled = !enabled;
        enabledItem.Checked = enabled;
        suppressedWindow = IntPtr.Zero;
        Program.Log(enabled ? "RESUME" : "PAUSE");
        Tick();
    }

    private void ReadLog(bool initial)
    {
        try
        {
            using (FileStream stream = new FileStream(deskflowLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (initial) offset = Math.Max(0, stream.Length - 1024 * 1024);
                if (stream.Length < offset) { offset = 0; partial = ""; }
                stream.Seek(offset, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string chunk = reader.ReadToEnd();
                    offset = stream.Position;
                    string text = partial + chunk;
                    int end = text.LastIndexOf('\n');
                    if (end >= 0)
                    {
                        partial = text.Substring(end + 1);
                        foreach (string line in text.Substring(0, end).Split('\n')) ParseLine(line, initial);
                    }
                    else partial = text;
                }
            }
            logAvailable = true;
        }
        catch (IOException) { logAvailable = false; }
        catch (UnauthorizedAccessException) { logAvailable = false; }
    }

    private void ParseLine(string line, bool initial)
    {
        if (line.Contains("INFO: starting core"))
        {
            locked = Control.IsKeyLocked(Keys.Scroll);
            local = true;
            if (!initial) { owned = owned && locked; pending = null; Program.Log("CORE RESTART"); }
        }
        if (line.Contains("INFO: leaving computer")) local = false;
        if (line.Contains("INFO: entering computer")) local = true;
        bool isLock = line.Contains("INFO: cursor locked to current computer");
        bool isUnlock = line.Contains("INFO: cursor unlocked from current computer");
        if (!isLock && !isUnlock) return;
        locked = isLock;
        if (initial) return;
        Program.Log("ACK locked=" + locked);
        if (pending.HasValue && pending.Value == locked)
        {
            pending = null;
            if (!locked) owned = false;
        }
        else if (!locked && owned)
        {
            // A manual Scroll Lock overrides the helper for this foreground window.
            owned = false;
            suppressedWindow = Native.GetForegroundWindow();
            Program.Log("MANUAL OVERRIDE");
        }
    }

    private void Tick()
    {
        if (stop.WaitOne(0)) { ExitThread(); return; }
        if (pause.WaitOne(0)) { Toggle(); return; }
        ReadLog(false);
        if (pending.HasValue && (DateTime.UtcNow - pendingSince).TotalSeconds > 2)
        {
            pending = null;
            enabled = false;
            enabledItem.Checked = false;
            Program.Log("ERROR no Deskflow acknowledgement; automation paused");
            tray.ShowBalloonTip(6000, "Deskflow: protection paused", "The lock change was not confirmed. Press Scroll Lock to unlock manually if needed.", ToolTipIcon.Warning);
        }
        IntPtr window = Native.GetForegroundWindow();
        captured = Native.IsMouseConfined(window, out captureDetail);
        if (window != suppressedWindow || !captured) suppressedWindow = IntPtr.Zero;
        if (captured) lastCaptured = DateTime.UtcNow;
        string sensor = "captured=" + captured + " " + captureDetail;
        if (sensor != lastSensor) { Program.Log("SENSOR " + sensor); lastSensor = sensor; }
        bool wanted = enabled && captured && local && window != suppressedWindow && logAvailable;
        if (!pending.HasValue)
        {
            if (wanted && !locked)
            {
                uint processId;
                Native.GetWindowThreadProcessId(window, out processId);
                try { using (Process p = Process.GetProcessById((int)processId)) currentProcess = p.ProcessName; }
                catch { currentProcess = "application"; }
                Request(true);
            }
            else if (owned && locked && (!enabled || (!captured && (DateTime.UtcNow - lastCaptured).TotalMilliseconds >= 150))) Request(false);
        }
        UpdateStatus();
    }

    private void Request(bool value)
    {
        if (pending.HasValue || locked == value) return;
        // Do not inject a chord if a modifier is held, e.g. while Alt+Tab is in progress.
        if (Native.ModifiersHeld()) return;
        if (value) owned = true;
        pending = value;
        pendingSince = DateTime.UtcNow;
        Program.Log("REQUEST locked=" + value + (value ? " process=" + currentProcess : ""));
        if (!Native.ToggleScrollLock())
        {
            pending = null;
            if (value) owned = false;
            enabled = false;
            enabledItem.Checked = false;
            Program.Log("ERROR SendInput failed; protection paused");
        }
    }

    private void UpdateStatus()
    {
        string status = !enabled ? "Paused" : !logAvailable ? "Deskflow log unavailable" : pending.HasValue ? "Changing lock state" : owned && locked ? "Mouse confined: switching blocked" : locked ? "Manual lock preserved" : "Mouse free: switching allowed";
        statusItem.Text = status;
        tray.Text = "Deskflow: " + status;
        tray.Icon = owned && locked ? SystemIcons.Hand : SystemIcons.Shield;
        try
        {
            string state = "mode=mouse-confinement\r\nenabled=" + enabled + "\r\nlocked=" + locked + "\r\nowned=" + owned + "\r\nlocal=" + local + "\r\ncaptured=" + captured + "\r\nsensor=" + captureDetail + "\r\npending=" + (pending.HasValue ? pending.ToString() : "none") + "\r\nstatus=" + status + "\r\n";
            string path = Path.Combine(Program.Folder, "status.txt");
            if (!File.Exists(path) || File.ReadAllText(path) != state) File.WriteAllText(path, state);
        }
        catch (IOException) { }
    }

    internal void Release()
    {
        if (releasing) return;
        releasing = true;
        timer.Stop();
        try
        {
            // Let an in-flight lock finish, then release only a lock owned by us.
            for (int i = 0; i < 10 && pending.HasValue; i++) { Thread.Sleep(50); ReadLog(false); }
            if (owned && locked && !pending.HasValue)
            {
                Request(false);
                for (int i = 0; i < 10 && pending.HasValue; i++) { Thread.Sleep(50); ReadLog(false); }
            }
            enabled = false;
            UpdateStatus();
            Program.Log("STOP remainingAutoLock=" + (owned && locked));
        }
        finally { tray.Visible = false; }
    }
    internal void Fail(Exception e)
    {
        Program.Log("ERROR " + e.GetType().Name + ": " + e.Message);
        enabled = false;
        Release();
        ExitThread();
    }
    protected override void ExitThreadCore() { Release(); base.ExitThreadCore(); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Release(); timer.Dispose(); tray.Dispose(); hotkey.Dispose(); stop.Dispose(); pause.Dispose(); }
        base.Dispose(disposing);
    }
}

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private readonly Action toggle;
    internal HotkeyWindow(Action action)
    {
        toggle = action;
        CreateHandle(new CreateParams());
        if (!Native.RegisterHotKey(Handle, 1, 0x4003, (uint)Keys.F12)) Program.Log("NOTE emergency shortcut unavailable; use tray or disable shortcut");
    }
    protected override void WndProc(ref Message m) { if (m.Msg == 0x0312) toggle(); base.WndProc(ref m); }
    public void Dispose() { Native.UnregisterHotKey(Handle, 1); DestroyHandle(); }
}

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct RECT { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { internal ushort Key, Scan; internal uint Flags, Time; internal UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] private struct INPUTUNION { [FieldOffset(0)] internal KEYBDINPUT Keyboard; [FieldOffset(0)] internal MOUSEINPUT Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { internal int X, Y; internal uint Data, Flags, Time; internal UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { internal uint Type; internal INPUTUNION Data; }
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out RECT rect);
    [DllImport("user32.dll")] private static extern bool GetClipCursor(out RECT rect);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int length);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    internal static bool ModifiersHeld()
    {
        foreach (int key in new int[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }) if ((GetAsyncKeyState(key) & 0x8000) != 0) return true;
        return false;
    }
    internal static bool ToggleScrollLock()
    {
        INPUT[] input = new INPUT[2];
        input[0].Type = input[1].Type = 1;
        input[0].Data.Keyboard.Key = input[1].Data.Keyboard.Key = 0x91;
        input[0].Data.Keyboard.Scan = input[1].Data.Keyboard.Scan = 0x46;
        input[1].Data.Keyboard.Flags = 2;
        return SendInput(2, input, Marshal.SizeOf(typeof(INPUT))) == 2;
    }
    internal static bool IsMouseConfined(IntPtr window, out string detail)
    {
        detail = "no-foreground";
        if (window == IntPtr.Zero || !IsWindowVisible(window) || IsIconic(window)) return false;
        StringBuilder name = new StringBuilder(256);
        GetClassName(window, name, name.Capacity);
        string cls = name.ToString();
        if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" || cls == "MultitaskingViewFrame" || cls == "XamlExplorerHostIslandWindow") { detail = "system-window"; return false; }
        RECT clip, bounds;
        if (!GetClipCursor(out clip) || !GetWindowRect(window, out bounds)) { detail = "query-failed"; return false; }
        RECT desktop = new RECT { Left = GetSystemMetrics(76), Top = GetSystemMetrics(77) };
        desktop.Right = desktop.Left + GetSystemMetrics(78);
        desktop.Bottom = desktop.Top + GetSystemMetrics(79);
        detail = "clip=" + RectText(clip) + " desktop=" + RectText(desktop);
        return IsConfinement(clip, desktop, bounds);
    }
    internal static string RectText(RECT rect) { return rect.Left + "," + rect.Top + "," + rect.Right + "," + rect.Bottom; }
    internal static bool IsConfinement(RECT clip, RECT desktop, RECT window)
    {
        if (clip.Right <= clip.Left || clip.Bottom <= clip.Top || desktop.Right <= desktop.Left || desktop.Bottom <= desktop.Top) return false;
        // The unrestricted desktop is the whole physical virtual-screen rectangle.
        // A fullscreen window alone is not evidence that the cursor is confined.
        bool restricted = clip.Left > desktop.Left + 2 || clip.Top > desktop.Top + 2 || clip.Right < desktop.Right - 2 || clip.Bottom < desktop.Bottom - 2;
        // Ignore a restriction left behind by another window or a different monitor.
        bool matchesWindow = clip.Left >= window.Left - 8 && clip.Top >= window.Top - 8 && clip.Right <= window.Right + 8 && clip.Bottom <= window.Bottom + 8;
        return restricted && matchesWindow;
    }
}

internal sealed class TestWindow : Form
{
    private bool full;
    private bool captureRequested, ownsClip;
    private Rectangle previous;
    private readonly Label description;
    internal TestWindow()
    {
        Text = "Deskflow Game Lock Test";
        Width = 900; Height = 450; StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(22, 28, 42); ForeColor = Color.White; KeyPreview = true;
        description = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 18) };
        Controls.Add(description);
        ApplyCapture();
        Activated += delegate { ApplyCapture(); };
        Deactivate += delegate { ReleaseClip(); };
        Resize += delegate { ApplyCapture(); };
        Move += delegate { ApplyCapture(); };
        FormClosed += delegate { ReleaseClip(); };
        KeyDown += delegate(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                full = !full;
                if (full) { previous = Bounds; WindowState = FormWindowState.Normal; FormBorderStyle = FormBorderStyle.None; Bounds = Screen.FromControl(this).Bounds; }
                else { FormBorderStyle = FormBorderStyle.Sizable; Bounds = previous; }
                ApplyCapture();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.F10) { if (!full) WindowState = FormWindowState.Maximized; e.Handled = true; }
            if (e.KeyCode == Keys.Space) { captureRequested = !captureRequested; ApplyCapture(); e.Handled = true; }
            if (e.KeyCode == Keys.Escape) Close();
        };
    }
    private void ReleaseClip() { if (ownsClip) { Cursor.Clip = Rectangle.Empty; ownsClip = false; } }
    private void ApplyCapture()
    {
        if (description == null) return;
        if (captureRequested && ContainsFocus && WindowState != FormWindowState.Minimized)
        {
            Cursor.Clip = RectangleToScreen(ClientRectangle);
            ownsClip = true;
        }
        else ReleaseClip();
        description.Text = "Mouse confinement test\n\n" + (captureRequested ? "GAME: pointer confined" : "MENU / VIDEO: pointer free") + "\n\nSpace: confine / release\nF11: fullscreen / windowed\nF10: maximize\nEscape: close";
    }
}
