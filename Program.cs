using System.Windows;

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
            if (settings.Enabled && detector.Feed(x, y, Environment.TickCount64)) overlay.Pulse();
        };

        using var tray = new Tray(settings, overlay, detector, app.Shutdown);
        app.Run();
    }
}
