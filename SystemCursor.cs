namespace PointerFinder;

/// <summary>
/// Hides the real pointer while the big one is showing, by swapping every system cursor role for a transparent one.
/// Restore() reloads the user's cursor scheme from the registry, so whichever theme is active comes back exactly.
/// If the app is killed while hidden the pointer stays invisible until the next start (which restores) or sign-out.
/// </summary>
static class SystemCursor
{
    // OCR_* ids: normal, ibeam, wait, cross, up, size nwse/nesw/we/ns/all, no, hand, app starting
    static readonly uint[] Roles = { 32512, 32513, 32514, 32515, 32516, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650 };

    static bool _hidden;

    public static void Hide()
    {
        if (_hidden) return;
        _hidden = true;
        var and = new byte[32 * 32 / 8];
        Array.Fill(and, (byte)0xFF);       // AND = 1, XOR = 0 leaves the screen untouched (fully transparent)
        var xor = new byte[32 * 32 / 8];
        foreach (var role in Roles)
        {
            var blank = Native.CreateCursor(0, 0, 0, 32, 32, and, xor);
            if (blank != 0) Native.SetSystemCursor(blank, role);
        }
    }

    public static void Restore()
    {
        _hidden = false;
        Native.SystemParametersInfo(Native.SPI_SETCURSORS, 0, 0, 0);
    }
}
