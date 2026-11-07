using System.Collections.ObjectModel;
using System.Windows.Data;
using SymbolLibrary.Core;
using System.ComponentModel;
namespace SymbolLibrary.Wpf.ViewModels;
public sealed class SymbolLibraryViewModel : NotifyBase
{
    private readonly SymbolCatalog catalog = new();
    private string searchText="", folderPath="", status="Select a folder containing DXF symbols";
    public ObservableCollection<SymbolItemViewModel> Symbols { get; }=new();
    public ICollectionView FilteredSymbols { get; }
    public string SearchText { get => searchText; set { if(Set(ref searchText,value)) { FilteredSymbols.Refresh(); Changed(nameof(VisibleCount)); } } }
    public string FolderPath { get => folderPath; private set => Set(ref folderPath,value); }
    public string Status { get => status; private set => Set(ref status,value); }
    public int VisibleCount => FilteredSymbols.Cast<object>().Count();
    public SymbolLibraryViewModel()
    {
        FilteredSymbols=CollectionViewSource.GetDefaultView(Symbols);
        FilteredSymbols.Filter = obj => obj is SymbolItemViewModel s && (string.IsNullOrWhiteSpace(SearchText) || s.Name.Contains(SearchText.Trim(),StringComparison.OrdinalIgnoreCase));
    }
    public void LoadFolder(string folder)
    {
        var (items,errors)=catalog.Load(folder);
        Symbols.Clear();
        foreach(var item in items) Symbols.Add(new SymbolItemViewModel(item));
        FolderPath=folder;
        FilteredSymbols.Refresh(); Changed(nameof(VisibleCount));
        Status=$"Loaded {items.Count} symbols" + (errors.Count>0 ? $"; {errors.Count} files skipped: {string.Join(" | ",errors.Take(3))}" : "");
    }
    public void SetStatus(string message) => Status=message;
}
