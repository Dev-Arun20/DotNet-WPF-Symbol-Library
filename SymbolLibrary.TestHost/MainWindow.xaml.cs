using System.IO;
using System.Windows;
using System.Windows.Input;
using SymbolLibrary.Contracts;
using SymbolLibrary.TestHost.ViewModels;
namespace SymbolLibrary.TestHost;
public partial class MainWindow : Window
{
    private readonly WorkspaceViewModel vm=new();
    public MainWindow()
    {
        InitializeComponent(); DataContext=vm;
        DrawingView.Interactive=true;
        Symbols.ViewModel.PropertyChanged += (_,e) => { if(e.PropertyName=="Status") vm.Status=Symbols.ViewModel.Status; };
        Loaded += (_,_) =>
        {
            var symbols=Path.Combine(AppContext.BaseDirectory,"03_Sample_Symbols");
            if(Directory.Exists(symbols)) Symbols.LoadFolder(symbols);
            var plan=Path.Combine(AppContext.BaseDirectory,"04_Sample_Drawing","Sample_Floor_Plan.dxf");
            if(File.Exists(plan)) OpenDrawing(plan);
        };
    }
    private void OpenDrawing(string path)
    {
        try { vm.OpenDrawing(path); DrawingView.Open(vm.Drawing!); }
        catch(Exception ex) { MessageBox.Show(this,ex.Message,"DXF open error",MessageBoxButton.OK,MessageBoxImage.Warning); }
    }
    private void OpenDrawingClick(object sender,RoutedEventArgs e)
    {
        var dialog=new Microsoft.Win32.OpenFileDialog { Filter="DXF drawings (*.dxf)|*.dxf",Title="Open DXF floor plan" };
        if(dialog.ShowDialog(this)==true) OpenDrawing(dialog.FileName);
    }
    private void FitClick(object sender,RoutedEventArgs e) => DrawingView.Fit();
    private void DrawingDragOver(object sender,DragEventArgs e)
    {
        e.Effects=vm.Drawing!=null && e.Data.GetDataPresent(SymbolDragFormat.Name) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled=true;
    }
    private void DrawingDrop(object sender,DragEventArgs e)
    {
        if(vm.Drawing is null || e.Data.GetData(SymbolDragFormat.Name) is not SymbolPlacement symbol) return;
        try
        {
            // Independent modules: drawing always reloads symbol from the contract's DXF path.
            var geo=vm.ReadDroppedSymbol(symbol.SourceFile,symbol.BlockName);
            var point=DrawingView.ScreenToWorld(e.GetPosition(DrawingView));
            DrawingView.AddAt(geo,point);
            vm.RecordPlacement(Path.GetFileNameWithoutExtension(symbol.SourceFile),point);
        }
        catch(Exception ex) { MessageBox.Show(this,ex.Message,"Symbol placement failed",MessageBoxButton.OK,MessageBoxImage.Warning); }
        e.Handled=true;
    }
}
