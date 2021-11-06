using System;
using System.Windows.Input;

namespace AnimeSD2HD
{
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> commandExecute;
        private readonly Func<object, bool> commandCanExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(string label, Action<object> commandExecute) : this(label, commandExecute, _ => true)
        {
            // NOTE: Perform a dummy call to get rid of the 'unused member' IntelliSense warning
            CanExecuteChanged?.Invoke(null, EventArgs.Empty);
        }

        public RelayCommand(string label, Action<object> commandExecute, Func<object, bool> commandCanExecute)
        {
            Label = label;
            this.commandExecute = commandExecute;
            this.commandCanExecute = commandCanExecute;
        }

        public string Label { get; }

        public bool CanExecute(object parameter) => commandCanExecute(parameter);

        public void Execute(object parameter) => commandExecute(parameter);
    }
}