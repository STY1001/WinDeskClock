using System.Windows.Input;

namespace WinDeskClock.Mvvm
{
    /// <summary>
    /// A simple synchronous ICommand implementation for buttons/toggles bound in XAML.
    /// Hooks into CommandManager.RequerySuggested so CanExecute is re-evaluated automatically
    /// on the usual WPF triggers (focus change, mouse click, etc.), matching how built-in
    /// WPF RoutedCommands behave.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Forces WPF to re-evaluate CanExecute right now, without waiting for the next
        /// automatic requery. Useful right after changing a flag that a command's
        /// CanExecute predicate depends on.
        /// </summary>
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
