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
/// Click-through, always-on-top window that draws an enlarged copy of the Rosewood arrow at the pointer.
/// The arrow's tip stays exactly on the real pointer's hotspot while it pops in, follows the mouse, then shrinks away.
/// </summary>
sealed class Overlay : Window
{
    const double NormalHeight = 26;   // DIPs: height of the regular cursor, so Scale = 4 means "4x bigger"
    const double Pad = 30;            // room around the arrow for the glow
    const double SmallScale = 0.25;   // the size it grows from and shrinks back to

    readonly Image _image;
    readonly ScaleTransform _scale = new(SmallScale, SmallScale);
    readonly DispatcherTimer _hold = new();
    readonly double _aspect;
    bool _visible;
    int _generation;                  // bumps on every pulse so a stale fade-out cannot hide a newer one
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
        _hold.Tick += (_, _) => { _hold.Stop(); FadeOut(); };
    }

    public void Configure(int scale, double holdSeconds)
    {
        double h = NormalHeight * scale;
        _image.Height = h;
        _image.Width = h * _aspect;
        Width = _image.Width + Pad * 2;
        Height = h + Pad * 2;
        _hold.Interval = TimeSpan.FromSeconds(holdSeconds);
    }

    /// <summary>Show (or keep showing) the enlarged pointer; restarts the hold timer.</summary>
    public void Pulse()
    {
        _generation++;
        _hold.Stop();
        if (!_visible)
        {
            _visible = true;
            Show();
            Follow();
            CompositionTarget.Rendering += OnRendering;
        }
        var pop = new DoubleAnimation(1, TimeSpan.FromMilliseconds(190)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 } };
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));
        _hold.Start();
    }

    void FadeOut()
    {
        int generation = _generation;
        var shrink = new DoubleAnimation(SmallScale, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
        fade.Completed += (_, _) =>
        {
            if (generation != _generation) return;
            CompositionTarget.Rendering -= OnRendering;
            Hide();
            _visible = false;
        };
        BeginAnimation(OpacityProperty, fade);
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
