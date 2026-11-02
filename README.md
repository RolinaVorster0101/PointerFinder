# Rosewood Pointer Finder

Shake the mouse and your pointer pops up large, with a pink glow, so you can find it on a big screen.
It uses the Rosewood Midnight arrow, stays for a couple of seconds after the last shake, then shrinks away.
A small tray app; no admin rights needed.

## Run

```powershell
dotnet build -c Release
.\bin\Release\net10.0-windows\RosewoodPointerFinder.exe
```

Look for the pink arrow in the system tray (it may be under the `^` overflow arrow).

## Tray menu

| Item | What it does |
|---|---|
| Show pointer now (or left-click the icon) | Test the effect without shaking |
| Shake to find is on | Turn the feature on or off |
| Size | Medium 3x, Large 4x (default), Huge 6x |
| Stays for | 1, 2 (default), 3 or 5 seconds after the last shake |
| Sensitivity | Low, Medium (default), High |
| Start with Windows | Adds or removes a `Run` entry in `HKCU` |
| Exit | Quit |

Settings are saved in `%APPDATA%\RosewoodPointerFinder\settings.json`.

## How it works

- `MouseHook` listens to all mouse movement with a low-level hook (no admin needed).
- `ShakeDetector` counts quick direction reversals on either axis within 800 ms; each swing must travel a
  minimum distance, which is what the sensitivity setting changes.
- `Overlay` is a click-through, always-on-top transparent window that draws a scaled copy of the arrow. The arrow's
  tip is the scaling origin, so the tip stays exactly on the real pointer. The real pointer is not touched.

## Limits

- The overlay cannot appear above windows running as administrator, or over some exclusive full-screen games,
  unless this app also runs as administrator.
- The enlarged arrow is a picture, so it always looks like the arrow, whatever cursor shape is active.
- Mixed-DPI multi-monitor setups are handled per monitor, but have only been tried on a single screen.

## Planned

- Becomes a module of the Rosewood manager app.
