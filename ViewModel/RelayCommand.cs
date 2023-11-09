using System;
using System.Windows.Input;

namespace JeopardyNesTextTool.ViewModel;

public class RelayCommand : ICommand
{

    private readonly Action<object> _methodToExecute;
    private readonly Func<object, bool> _canExecuteEvaluator;

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action<object> methodToExecute, Func<object, bool> canExecuteEvaluator = null)
    {
        _methodToExecute = methodToExecute;
        _canExecuteEvaluator = canExecuteEvaluator;
    }

    public RelayCommand(Action<object> methodToExecute)
        : this(methodToExecute, null)
    {
    }

    public bool CanExecute(object parameter)
    {
        return _canExecuteEvaluator == null || _canExecuteEvaluator.Invoke(parameter);
    }

    public void Execute(object parameter)
    {
        _methodToExecute.Invoke(parameter);
    }
}