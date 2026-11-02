# Pointer Finder

Shake the mouse and your pointer pops up large, with a soft glow, so you can find it on a big screen.
It stays big while you move, then slowly shrinks back once the mouse is still.
A small tray app; no admin rights needed. Theme-independent: it only draws an arrow image, so any theme
can supply its own (see "Changing the arrow").

## Run

```powershell
dotnet build -c Release
.\bin\Release\net10.0-windows\PointerFinder.exe
```

Look for the arrow in the system tray (it may be under the `^` overflow arrow).

## Behaviour

1. Shake the mouse: the big pointer pops in, tip on the real pointer.
2. It stays big while the mouse moves, for at most 5 seconds after the last shake.
3. As soon as the mouse is still (about a tenth of a second) it starts shrinking, quickly at first and then easing out, down to normal size.
4. Once the shrink has started it always finishes: moving does not bring it back, and a new shake is only
   recognised after the real pointer is back. (A shake while it is still big keeps it big.)

## Tray menu

| Item | What it does |
|---|---|
| Show pointer now (or left-click the icon) | Test the effect without shaking |
| Shake to find is on | Turn the feature on or off |
| Size | Medium 3x, Large 4x (default), Huge 6x |
| Shrinks over | How long the slow shrink takes: 1, 2 (default), 3 or 5 seconds |
| Sensitivity | Low, Medium (default), High |
| Start with Windows | Adds or removes a `Run` entry in `HKCU` |
| Exit | Quit |

Settings are saved in `%APPDATA%\PointerFinder\settings.json`.

## How it works

- `MouseHook` listens to all mouse movement with a low-level hook (no admin needed).
- `ShakeDetector` counts quick direction reversals on either axis within 800 ms; each swing must travel a
  minimum distance, which is what the sensitivity setting changes.
- `Overlay` is a click-through, always-on-top transparent window that draws a scaled copy of the arrow. The arrow's
  tip is the scaling origin, so the tip stays exactly on the real pointer. The real pointer is not touched.

## Changing the arrow

The arrow is `Assets/arrow.png` (currently the Rosewood Midnight arrow), compiled into the app. Replace it and
rebuild to match another theme. A per-user override file is a possible later addition.

## Limits

- The overlay cannot appear above windows running as administrator, or over some exclusive full-screen games,
  unless this app also runs as administrator.
- The enlarged arrow is a picture, so it always looks like the arrow, whatever cursor shape is active.
- Mixed-DPI multi-monitor setups are handled per monitor, but have only been tried on a single screen.

## Planned

- Becomes a module of the manager app.

## Fresh Windows

Setting up everything again after a reinstall (theme, PCSteward, temperatures, Pointer Finder): see `docs/SETUP.md` in the PCSteward repository, https://github.com/RolinaVorster0101/PCSteward.

