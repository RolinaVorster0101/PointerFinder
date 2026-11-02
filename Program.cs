using System.Windows;

namespace PointerFinder;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var single = new Mutex(true, "RosewoodPointerFinder.Singleton", out bool first);
        if (!first) return;

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var settings = Settings.Load();
        var overlay = new Overlay();
        var detector = new ShakeDetector { Sensitivity = settings.Sensitivity };

        using var hook = new MouseHook();
        hook.Moved += (x, y) =>
        {
            if (settings.Enabled && detector.Feed(x, y, Environment.TickCount64)) overlay.Pulse();
        };

        using var tray = new Tray(settings, overlay, detector, app.Shutdown);
        app.Run();
    }
}
