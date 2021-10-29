using System;
using System.Windows.Input;

namespace AnimeSD2HD
{
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> commandExecute;
        private readonly Func<object, bool> commandCanExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> commandExecute) : this(commandExecute, _ => true)
        {
        }

        public RelayCommand(Action<object> commandExecute, Func<object, bool> commandCanExecute)
        {
            this.commandExecute = commandExecute;
            this.commandCanExecute = commandCanExecute;
        }

        public bool CanExecute(object parameter) => commandCanExecute(parameter);

        public void Execute(object parameter) => commandExecute(parameter);
    }
}