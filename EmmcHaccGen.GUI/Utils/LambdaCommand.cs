using System;
using System.Windows.Input;

namespace EmmcHaccGen.GUI.Utils
{
    public class LambdaCommand : ICommand
    {
        public delegate void LambdaFunction(object? parameter);
        private LambdaFunction lambda;

        public LambdaCommand(LambdaFunction lambda) => this.lambda = lambda;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => lambda.Invoke(parameter);
    }
}
