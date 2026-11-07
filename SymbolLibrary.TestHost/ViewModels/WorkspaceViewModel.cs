using SymbolLibrary.Core;
using SymbolLibrary.Wpf.ViewModels;
using System.IO;
namespace SymbolLibrary.TestHost.ViewModels;
public sealed class WorkspaceViewModel : NotifyBase
{
    private readonly DxfGeometryReader reader=new();
    private GeometryDocument? drawing;
    private string status="Open a DXF drawing and choose a symbol folder";
    private int placementCount;
    public GeometryDocument? Drawing { get => drawing; private set => Set(ref drawing,value); }
    public string Status { get => status; set => Set(ref status,value); }
    public int PlacementCount { get => placementCount; private set => Set(ref placementCount,value); }
    public string PlacementLabel => $"Placed: {PlacementCount}";
    public void OpenDrawing(string path)
    {
        Drawing=reader.Read(path);
        PlacementCount=0; Changed(nameof(PlacementLabel));
        Status=$"Opened {Path.GetFileName(path)} · units: mm";
    }
    public GeometryDocument ReadDroppedSymbol(string sourcePath,string? blockName) => reader.Read(sourcePath,blockName);
    public void RecordPlacement(string name,Pt point)
    {
        PlacementCount++; Changed(nameof(PlacementLabel));
        Status=$"Placed {name} at ({point.X:0}, {point.Y:0}) mm";
    }
}
