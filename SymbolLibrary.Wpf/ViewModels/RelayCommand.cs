using System.Windows.Input;
namespace SymbolLibrary.Wpf.ViewModels;
public sealed class RelayCommand : ICommand
{
    private readonly Action run;
    private readonly Func<bool>? can;
    public RelayCommand(Action run, Func<bool>? can=null) { this.run=run; this.can=can; }
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => can?.Invoke() ?? true;
    public void Execute(object? parameter) => run();
    public void Refresh() => CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}
