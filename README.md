# Symbol Library · .NET 8 / WPF / MVVM

Three projects (Visual Studio 2022, Windows 10/11, .NET 8 Desktop):

- **SymbolLibrary.Core** (net8.0, no WPF): reads DXF through `netDxf.netstandard` 3.0.1, flattens geometry and blocks, normalizes arc and LWPOLYLINE bulge segments, loads/searches catalog.
- **SymbolLibrary.Wpf** (net8.0-windows): reusable MVVM symbol panel, search, vector thumbnails and drag payload; reusable `GeometryView` vector drawing control.
- **SymbolLibrary.TestHost** (net8.0-windows): demo drawing, DXF open, drag/drop, fit, mouse-wheel zoom.

## Start

1. Install Visual Studio 2022 with **.NET desktop development** and the .NET 8 SDK.
2. Open `SymbolLibrary.sln` and restore NuGet packages (Internet is needed on first restore).
3. Set `SymbolLibrary.TestHost` as startup project; press F5. Or from Developer PowerShell: `dotnet build SymbolLibrary.sln`, then `dotnet run --project SymbolLibrary.TestHost`.
4. The demo floor plan and generated symbol folders are copied into the output on build. Select **Choose Symbols Folder** to load other DXF folders recursively. **Open DXF Drawing** opens another drawing.
5. Filter by typing; drag a card into the canvas. Wheel zoom is anchored to your mouse. **Fit to Window** resets zoom.

## IMPORTANT: original assessment kit is not present

Only `01_Task_.docx` was supplied. The original `02_Contract/SymbolPlacement.cs`, 30 official symbol DXFs and sample floor plan DXF **were not attached**. Consequently:

- The contract in `02_Contract` is **OFFICIAL**, implementing fields described in the brief; its actual member declarations cannot be verified. Replace this file with the unmodified official contract and adjust call sites only if API details differ.
- `03_Sample_Symbols` and `04_Sample_Drawing` contain **generated representative demo DXF assets**, not the official samples. Replace/copy the official assets there before submission and test `Table_4_Chairs.dxf` and `Furniture_Pack.dxf` explicitly.
- Source files and solution are supplied, **not** precompiled binaries; Windows WPF build was not available in the generation environment. Compile and run on Windows to validate and resolve any SDK-specific diagnostics.

## Behavior and design notes

- Geometry coordinates are all in millimetres; WPF flips the Y axis exactly once during rendering.
- Zoom and placed objects share a single world-to-screen transform. Symbol placement anchors each symbol's geometry **bounding-box centre** to the drop point; no scale-to-fit is applied to inserted symbols.
- INSERT composition: parent transform · translate(insert position) · rotate · scale · translate(-block origin). Nested references are followed; recursion is guarded.
- Drawing reads the dropped symbol again from `SymbolPlacement.SourceDxfPath`; no panel state is shared with the drawing.
- UI input plumbing (DXF dialogs, physical drag-drop, mouse wheel) resides in view code-behind/control; searchable catalog and workspace data reside in view models. DXF decoding and searchable index reside in Core.
- Unsupported DXF object types are ignored. Only the required primitive types are targeted.

## Project layout

```
02_Contract/SymbolPlacement.cs      provisional required shared API
SymbolLibrary.Core/                domain geometry, reader, catalog
SymbolLibrary.Wpf/ViewModels/       observable view models, commands
SymbolLibrary.Wpf/SymbolLibraryPanel.xaml reusable panel
SymbolLibrary.Wpf/GeometryView.cs   custom vector painter
SymbolLibrary.TestHost/ViewModels/   drawing workspace view model
SymbolLibrary.TestHost/MainWindow.xaml demo application
03_Sample_Symbols/                  generated demo geometry files
04_Sample_Drawing/                  generated floor plan DXF
```


## Fixes (2026-11-06)
- `LwPolyline` changed to `Polyline2D` for netDxf.netstandard 3.0.1.
- Original `TEST_01.zip` contract is included unchanged. Panel and drawing area now use `SymbolLibrary.Contracts.SymbolPlacement`, its positional record constructor, `SourceFile`, and `SymbolDragFormat.Name`.
- Replaced demonstration DXF files with the original kit DXF assets.
- Rebuild the full solution after NuGet restore. XAML missing-control errors are normally cascading after a referenced project does not build.
- Windows Visual Studio / dotnet build verification remains necessary (Windows .NET SDK unavailable here).
