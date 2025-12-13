using System.Windows.Input;

namespace uchat.Services
{
    partial class RelayCommand(Action<object> execute, Func<object, bool> canExecute = null) : ICommand
    {
        private readonly Action<object> _execute = execute;
        private readonly Func<object, bool> _canExecute = canExecute;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
