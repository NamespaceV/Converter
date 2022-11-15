using System;
using System.Windows.Input;

namespace Converter.Basic
{
    public class SimpleCommand : ICommand
    {
        private readonly Action lambda;
        private bool enabled;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public SimpleCommand(Action lambda)
        {
            this.lambda = lambda;
            enabled = true;
        }

        public bool CanExecute(object parameter)
        {
            return enabled;
        }

        public void Execute(object parameter)
        {
            lambda.Invoke();
        }

        public void SetEnabled(bool enabled)
        {
            if (this.enabled == enabled) return;
            this.enabled = enabled;
        }
    }
}
