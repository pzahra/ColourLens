# ColourLens

A resizable, always-on-top lens that shows what is underneath it as someone with a colour vision deficiency would see it. It's a plain .NET 10 WinForms app with no runtime to hunt down. It uses the Windows Magnification API at 1× zoom with a colour matrix applied.

## Run

```powershell
dotnet run -c Release
```

Or publish a single exe:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## Use

- Drag the dark bar to move the lens. Drag the edges or corners to resize it. Double-click the bar to fill the screen.
- The lens is click-through, so you can keep working in the window underneath.
- Click the mode name, or right-click the bar, to choose a mode.
- The slider sets severity. At 100% you get the full dichromacy (-opia). Below that you get the anomalous form (-omaly).

| Key | Action |
| --- | --- |
| `0` | Off (normal vision) |
| `1` | Protan (red) |
| `2` | Deutan (green, the most common) |
| `3` | Tritan (blue) |
| `4` | Achromat (no colour) |
| `↑` `↓` / mouse wheel | Severity |
| `Space` | Toggle between the filter and the original |

Mode, severity and window position are saved in `%APPDATA%\ColourLens\settings.json`.

## How it works

- `FrameWindow` is the visible frame. Its window region has a hole where the lens goes.
- `LensWindow` sits in that hole. It is a layered, click-through, topmost window that hosts a `Magnifier` control. The control is excluded from its own capture and re-renders the area under it about 60 times a second through a `MAGCOLOREFFECT` matrix.
- The matrices come from Machado, Oliveira & Fernandes (2009). Severities below 100% are a linear blend towards normal vision.

## Limitations

- The matrix is applied to gamma-encoded sRGB, because the Magnification API offers no gamma step. Windows' own colour filters work the same way. The result is a close approximation, not a lab-grade simulation.
- DRM-protected video shows up black inside the lens.
