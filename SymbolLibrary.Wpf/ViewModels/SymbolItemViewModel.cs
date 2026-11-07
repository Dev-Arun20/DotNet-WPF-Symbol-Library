using SymbolLibrary.Core;
namespace SymbolLibrary.Wpf.ViewModels;
public sealed class SymbolItemViewModel
{
    public SymbolEntry Entry { get; }
    public SymbolItemViewModel(SymbolEntry entry) => Entry=entry;
    public string Name => Entry.Name;
    public string Category => Entry.Category;
    public string FilePath => Entry.FilePath;
    public GeometryDocument Geometry => Entry.Geometry;
}
