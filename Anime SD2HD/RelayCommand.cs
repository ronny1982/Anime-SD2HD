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
        }

        public RelayCommand(string label, Action<object> commandExecute, Func<object, bool> commandCanExecute)
        {
            Label = label;
            this.commandExecute = commandExecute;
            this.commandCanExecute = commandCanExecute;
        }

        public string Label { get; }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(object parameter) => commandCanExecute(parameter);
        public void Execute(object parameter) => commandExecute(parameter);
    }
}