using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PointerFinder;

/// <summary>
/// Click-through, always-on-top window that draws an enlarged arrow at the pointer.
/// The arrow's tip stays exactly on the real pointer's hotspot. It stays big while the mouse moves, then shrinks once
/// the mouse is still. Once the shrink has started it always finishes: movement does not bring it back, and a new
/// shake is only recognised after the real pointer has returned.
/// </summary>
sealed class Overlay : Window
{
    const double NormalHeight = 26;   // DIPs: height of the regular cursor, so Scale = 4 means "4x bigger"
    const double Pad = 30;            // room around the arrow for the glow
    const long StillMs = 100;         // how long the mouse must be still before the shrink starts
    const long MaxBigMs = 5000;       // after this long since the last shake it shrinks even if the mouse keeps moving

    enum Phase { Hidden, Big, Shrinking }

    readonly Image _image;
    readonly ScaleTransform _scale = new(0.25, 0.25);
    double _small = 0.25;             // scale that equals a normal-sized pointer (1 / Scale): it grows from and shrinks back to this
    readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(40) };
    readonly double _aspect;
    Phase _phase = Phase.Hidden;
    long _lastMoveMs, _lastShakeMs;
    double _shrinkSeconds = 2;
    int _generation;                  // bumps when a shrink starts or is cancelled so a stale Completed cannot hide a newer state
    nint _hwnd;

    public Overlay()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Focusable = false;
        Opacity = 0;

        var source = new BitmapImage(new Uri("pack://application:,,,/Assets/arrow.png"));
        _aspect = source.PixelWidth / (double)source.PixelHeight;
        _image = new Image
        {
            Source = source,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(Pad),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = new Point(0, 0),   // scale around the tip so it never drifts off the hotspot
            RenderTransform = _scale,
            Effect = new DropShadowEffect { Color = Color.FromRgb(0xEF, 0x82, 0x98), BlurRadius = 24, ShadowDepth = 0, Opacity = 0.6 },
        };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);
        Content = _image;

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            int style = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
            Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, style | Native.WS_EX_TRANSPARENT | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW);
        };
        _tick.Tick += (_, _) => OnTick();
    }

    public void Configure(int scale, double shrinkSeconds)
    {
        double h = NormalHeight * scale;
        _image.Height = h;
        _image.Width = h * _aspect;
        Width = _image.Width + Pad * 2;
        Height = h + Pad * 2;
        _shrinkSeconds = shrinkSeconds;
        _small = 1.0 / scale;
    }

    /// <summary>A shake was recognised (or the user asked for it): show the big pointer.</summary>
    public void Pulse()
    {
        long now = Environment.TickCount64;
        _lastMoveMs = _lastShakeMs = now;
        _generation++;
        if (_phase == Phase.Hidden)
        {
            _scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _scale.ScaleX = _scale.ScaleY = _small;
            SystemCursor.Hide();   // the overlay stands in for the real pointer while it is big
            Show();
            Follow();
            CompositionTarget.Rendering += OnRendering;
            _tick.Start();
        }
        _phase = Phase.Big;
        GrowBack();
    }

    /// <summary>True from the moment the shrink starts until the real pointer is back. Shakes are ignored meanwhile.</summary>
    public bool IsShrinking => _phase == Phase.Shrinking;

    /// <summary>Call on every mouse movement. Only matters while big: movement keeps it big until the mouse is still.</summary>
    public void NotifyMoved()
    {
        if (_phase == Phase.Big) _lastMoveMs = Environment.TickCount64;
    }

    void OnTick()
    {
        if (_phase != Phase.Big) return;
        long now = Environment.TickCount64;
        if (now - _lastMoveMs >= StillMs || now - _lastShakeMs >= MaxBigMs) Shrink();
    }

    void GrowBack()
    {
        var pop = new DoubleAnimation(1, TimeSpan.FromMilliseconds(190)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 } };
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));
    }

    /// <summary>
    /// Slow shrink down to normal pointer size, then hand over to the real pointer. The glow fades in the last fifth
    /// so the swap back to the real pointer is not noticeable.
    /// </summary>
    void Shrink()
    {
        _phase = Phase.Shrinking;
        int generation = ++_generation;
        var length = TimeSpan.FromSeconds(_shrinkSeconds);
        var shrink = new DoubleAnimation(_small, length) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } };   // ease-out: visibly starts shrinking straight away
        shrink.Completed += (_, _) =>
        {
            if (generation != _generation) return;
            _tick.Stop();
            CompositionTarget.Rendering -= OnRendering;
            Hide();
            SystemCursor.Restore();
            _phase = Phase.Hidden;
        };
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink.Clone());
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(_shrinkSeconds * 0.2)) { BeginTime = TimeSpan.FromSeconds(_shrinkSeconds * 0.8) });
    }

    void OnRendering(object? sender, EventArgs e) => Follow();

    /// <summary>Move the window so the arrow's tip sits on the pointer. Works in physical pixels.</summary>
    void Follow()
    {
        if (_hwnd == 0 || !Native.GetCursorPos(out var p)) return;
        int offset = (int)Math.Round(Pad * VisualTreeHelper.GetDpi(this).DpiScaleX);
        Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, p.X - offset, p.Y - offset, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }
}
