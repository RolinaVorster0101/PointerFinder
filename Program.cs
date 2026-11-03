using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PointerFinder;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var single = new Mutex(true, "PointerFinder.Singleton", out bool first);
        if (!first) return;

        // never leave the real pointer hidden: restore at start (in case a previous run was killed), on exit and on crashes
        SystemCursor.Restore();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SystemCursor.Restore();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => SystemCursor.Restore();

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var settings = Settings.Load();
        var overlay = new Overlay();
        var detector = new ShakeDetector { Sensitivity = settings.Sensitivity };

        using var hook = new MouseHook();
        hook.Moved += (x, y) =>
        {
            overlay.NotifyMoved();
            if (overlay.IsShrinking) { detector.Reset(); return; }   // a shrink always finishes before the next shake counts
            if (settings.Enabled && detector.Feed(x, y, Environment.TickCount64)) overlay.Pulse();
        };

        using var tray = new Tray(settings, overlay, detector, app.Shutdown);

        // other programs (PCSteward) can edit the settings file, ask for a pulse, or ask Pointer Finder to quit
        var reload = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        reload.Tick += (_, _) =>
        {
            reload.Stop();
            if (Settings.TryLoad() is { } fresh) { settings.CopyFrom(fresh); tray.Refresh(); }
            else reload.Start();   // half-written: try again shortly
        };
        using var watcher = new FileSystemWatcher(Path.GetDirectoryName(Settings.FilePath)!, Path.GetFileName(Settings.FilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        FileSystemEventHandler changed = (_, _) => app.Dispatcher.BeginInvoke(() => { reload.Stop(); reload.Start(); });
        watcher.Changed += changed;
        watcher.Created += changed;
        watcher.Renamed += (s, e) => changed(s, e);

        using var quit = new EventWaitHandle(false, EventResetMode.AutoReset, "PointerFinder.Quit");
        using var pulse = new EventWaitHandle(false, EventResetMode.AutoReset, "PointerFinder.Pulse");
        new Thread(() =>
        {
            var handles = new WaitHandle[] { quit, pulse };
            while (true)
            {
                int which = WaitHandle.WaitAny(handles);
                if (which == 0) { app.Dispatcher.BeginInvoke(app.Shutdown); return; }
                app.Dispatcher.BeginInvoke(overlay.Pulse);
            }
        }) { IsBackground = true }.Start();

        app.Run();
    }
}
