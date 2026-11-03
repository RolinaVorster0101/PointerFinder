using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Draw = System.Drawing;

namespace PointerFinder;

/// <summary>Tray icon and menu. Colours follow the Rosewood Midnight palette.</summary>
sealed class Tray : IDisposable
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "PointerFinder";

    readonly Settings _settings;
    readonly Overlay _overlay;
    readonly ShakeDetector _detector;
    readonly Forms.NotifyIcon _icon;
    readonly List<Action> _sync = new();   // each refreshes one menu item's check mark from the settings

    public Tray(Settings settings, Overlay overlay, ShakeDetector detector, Action exit)
    {
        _settings = settings;
        _overlay = overlay;
        _detector = detector;

        var menu = new Forms.ContextMenuStrip
        {
            Renderer = new Forms.ToolStripProfessionalRenderer(new RosewoodColors()) { RoundedEdges = false },
            BackColor = Draw.Color.FromArgb(0x16, 0x1B, 0x2B),
            ForeColor = Draw.Color.FromArgb(0xF5, 0xF3, 0xF5),
            ShowImageMargin = false,
            ShowCheckMargin = true,
        };

        menu.Items.Add("Show pointer now", null, (_, _) => _overlay.Pulse());
        menu.Items.Add(new Forms.ToolStripSeparator());

        var enabled = new Forms.ToolStripMenuItem("Shake to find is on") { CheckOnClick = true, Checked = _settings.Enabled };
        enabled.Click += (_, _) => { _settings.Enabled = enabled.Checked; _settings.Save(); };
        menu.Items.Add(enabled);
        _sync.Add(() => enabled.Checked = _settings.Enabled);

        menu.Items.Add(Choice("Size", () => _settings.Scale, new[] { (3, "Medium (3x)"), (4, "Large (4x)"), (6, "Huge (6x)") },
            v => { _settings.Scale = v; Apply(); }));
        menu.Items.Add(Choice("Shrinks over", () => (int)_settings.ShrinkSeconds, new[] { (1, "1 second"), (2, "2 seconds"), (3, "3 seconds"), (5, "5 seconds") },
            v => { _settings.ShrinkSeconds = v; Apply(); }));
        menu.Items.Add(Choice("Sensitivity", () => _settings.Sensitivity, new[] { (0, "Low"), (1, "Medium"), (2, "High") },
            v => { _settings.Sensitivity = v; Apply(); }));

        menu.Items.Add(new Forms.ToolStripSeparator());
        var startup = new Forms.ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = IsAutostart() };
        startup.Click += (_, _) => SetAutostart(startup.Checked);
        menu.Items.Add(startup);
        _sync.Add(() => startup.Checked = IsAutostart());
        menu.Opening += (_, _) => Refresh();
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exit());

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Pointer Finder - shake the mouse",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) _overlay.Pulse(); };
        Apply();
    }

    void Apply()
    {
        Refresh();
        _settings.Save();
    }

    /// <summary>Push the current settings into the overlay, the detector and the menu (also used when another program edits the file).</summary>
    public void Refresh()
    {
        _overlay.Configure(_settings.Scale, _settings.ShrinkSeconds);
        _detector.Sensitivity = _settings.Sensitivity;
        foreach (var sync in _sync) sync();
    }

    Forms.ToolStripMenuItem Choice(string title, Func<int> current, (int Value, string Label)[] options, Action<int> onPick)
    {
        var parent = new Forms.ToolStripMenuItem(title);
        foreach (var (value, label) in options)
        {
            var item = new Forms.ToolStripMenuItem(label) { Checked = value == current() };
            _sync.Add(() => item.Checked = value == current());
            item.Click += (_, _) =>
            {
                foreach (Forms.ToolStripMenuItem sibling in parent.DropDownItems) sibling.Checked = false;
                item.Checked = true;
                onPick(value);
            };
            parent.DropDownItems.Add(item);
        }
        return parent;
    }

    static Draw.Icon LoadIcon()
    {
        var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))?.Stream;
        return stream != null ? new Draw.Icon(stream) : Draw.SystemIcons.Application;
    }

    static bool IsAutostart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunName) != null;
    }

    static void SetAutostart(bool on)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (on) key.SetValue(RunName, "\"" + Environment.ProcessPath + "\"");
        else key.DeleteValue(RunName, throwOnMissingValue: false);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    sealed class RosewoodColors : Forms.ProfessionalColorTable
    {
        static readonly Draw.Color Panel = Draw.Color.FromArgb(0x16, 0x1B, 0x2B);
        static readonly Draw.Color Hover = Draw.Color.FromArgb(0x25, 0x32, 0x42);
        static readonly Draw.Color Border = Draw.Color.FromArgb(0x35, 0x41, 0x53);
        static readonly Draw.Color Accent = Draw.Color.FromArgb(0xC5, 0x6D, 0x86);

        public override Draw.Color ToolStripDropDownBackground => Panel;
        public override Draw.Color ImageMarginGradientBegin => Panel;
        public override Draw.Color ImageMarginGradientMiddle => Panel;
        public override Draw.Color ImageMarginGradientEnd => Panel;
        public override Draw.Color MenuBorder => Border;
        public override Draw.Color MenuItemBorder => Draw.Color.FromArgb(0x8B, 0x4D, 0x65);
        public override Draw.Color MenuItemSelected => Hover;
        public override Draw.Color MenuItemSelectedGradientBegin => Hover;
        public override Draw.Color MenuItemSelectedGradientEnd => Hover;
        public override Draw.Color SeparatorDark => Border;
        public override Draw.Color SeparatorLight => Border;
        public override Draw.Color CheckBackground => Accent;
        public override Draw.Color CheckSelectedBackground => Accent;
        public override Draw.Color CheckPressedBackground => Accent;
    }
}
