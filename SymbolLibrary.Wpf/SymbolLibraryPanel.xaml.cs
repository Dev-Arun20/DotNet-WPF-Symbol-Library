using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SymbolLibrary.Contracts;
using SymbolLibrary.Wpf.ViewModels;
namespace SymbolLibrary.Wpf;
public partial class SymbolLibraryPanel : UserControl
{
    public SymbolLibraryViewModel ViewModel { get; } = new();
    private Point? start;
    public SymbolLibraryPanel() { InitializeComponent(); DataContext=ViewModel; }
    public void LoadFolder(string folder)
    {
        try { ViewModel.LoadFolder(folder); }
        catch(Exception e) { ViewModel.SetStatus("Cannot load symbols: " + e.Message); }
    }
    private void ChooseFolderClick(object sender,RoutedEventArgs e)
    {
        var dlg=new Microsoft.Win32.OpenFolderDialog { Title="Choose a folder containing DXF symbols" };
        if(dlg.ShowDialog()==true) LoadFolder(dlg.FolderName);
    }
    private void CardMouseDown(object sender, MouseButtonEventArgs e) => start=e.GetPosition((IInputElement)sender);
    private void CardMouseUp(object sender,MouseButtonEventArgs e) => start=null;
    private void CardMouseMove(object sender,MouseEventArgs e)
    {
        if(start is null || e.LeftButton!=MouseButtonState.Pressed || sender is not Border border || border.DataContext is not SymbolItemViewModel symbol) return;
        var difference=e.GetPosition(border)-start.Value;
        if(Math.Abs(difference.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(difference.Y)<SystemParameters.MinimumVerticalDragDistance) return;
        start=null;
        var b=symbol.Geometry.Bounds;
        var payload=new SymbolPlacement(
            symbol.FilePath, symbol.Name, Path.GetFullPath(symbol.FilePath),
            null, "Millimeters", b.Width, b.Height);
        var data=new DataObject(); data.SetData(SymbolDragFormat.Name,payload);
        DragDrop.DoDragDrop(border,data,DragDropEffects.Copy);
    }
}
