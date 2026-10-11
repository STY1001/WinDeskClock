using System.Windows.Input;

namespace WinDeskClock.Mvvm
{
    /// <summary>
    /// An ICommand for async operations (button clicks that used to be "private async void ..._Click").
    /// By default it disables itself while running to avoid re-entrancy (double-click starting the
    /// same action twice), mirroring how most of the original click handlers behaved implicitly
    /// (state flags like "TimerIsRunning" already guarded against double execution).
    ///
    /// Any exception thrown by the wrapped action is left to propagate to the current
    /// SynchronizationContext, which for a WPF app means it reaches
    /// Application.DispatcherUnhandledException - exactly like the original "async void" event
    /// handlers did. We deliberately do NOT swallow exceptions here, since the app relies on that
    /// handler to show its crash dialog.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private readonly bool _preventReentrancy;
        private bool _isRunning;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, bool preventReentrancy = true)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute(), preventReentrancy)
        {
        }

        public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null, bool preventReentrancy = true)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _preventReentrancy = preventReentrancy;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_preventReentrancy && _isRunning)
            {
                return false;
            }

            return _canExecute?.Invoke(parameter) ?? true;
        }

        public async void Execute(object? parameter)
        {
            _isRunning = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isRunning = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
