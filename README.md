# ColourLens

A resizable, always-on-top lens that shows what is underneath it as someone with a colour vision deficiency would see it. It can also recolour the screen to help someone with the deficiency tell colours apart. It's a plain .NET 10 WinForms app with no runtime to hunt down. It uses the Windows Magnification API at 1× zoom with a colour matrix applied.

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
- For protan, deutan and tritan, the button next to the mode name switches between three views:
  - **Simulate** shows how the screen looks with the deficiency.
  - **Correct** recolours the screen so someone with the deficiency can tell confused colours apart.
  - **Check** applies the correction and then the simulation. It shows a normal-sighted viewer what a colour-blind viewer sees after correction.

| Key | Action |
| --- | --- |
| `0` | Off (normal vision) |
| `1` | Protan (red) |
| `2` | Deutan (green, the most common) |
| `3` | Tritan (blue) |
| `4` | Achromat (no colour) |
| `↑` `↓` / mouse wheel | Severity |
| `S` / `C` / `K` | Simulate / Correct / Check |
| `Space` | Toggle between the filter and the original |

Mode, severity and window position are saved in `%APPDATA%\ColourLens\settings.json`.

## How it works

- `FrameWindow` is the visible frame. Its window region has a hole where the lens goes.
- `LensWindow` sits in that hole. It is a layered, click-through, topmost window that hosts a `Magnifier` control. The control is excluded from its own capture and re-renders the area under it about 60 times a second through a `MAGCOLOREFFECT` matrix.
- The matrices come from Machado, Oliveira & Fernandes (2009). Severities below 100% are a linear blend towards normal vision.
- Correction uses daltonisation (Fidaner et al. 2005). It takes what the viewer loses (the original minus the simulation) and moves it into channels they can still see. For protan and deutan, lost red/green information goes into green and blue. For tritan, lost blue/yellow information goes onto the red–green axis, because Fidaner's red/green shift barely helps tritans. Each step is linear, so the whole correction is still one colour matrix.
- A numerical check used 200,000 random colour pairs. Of the pairs a dichromat would confuse, correction made these clearly distinct (ΔE ≥ 15 when seen through the same simulation): protan 88%, deutan 74%, tritan 76%. In every case, 4% or fewer got worse.

## Limitations

- The matrix is applied to gamma-encoded sRGB, because the Magnification API offers no gamma step. Windows' own colour filters work the same way. The result is a close approximation, not a lab-grade simulation.
- Correction pushes some colours past what the screen can show, so they get clipped. Check applies correction and simulation as one matrix, without the clipping a real screen does in between, so it can look a little more favourable than reality for very saturated colours.
- DRM-protected video shows up black inside the lens.
