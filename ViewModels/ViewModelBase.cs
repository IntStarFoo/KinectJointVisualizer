using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Input;


/// <summary>
/// MVVM uses commands instead of UI events
/// http://blog.magnusmontin.net/2013/06/30/handling-events-in-an-mvvm-wpf-application/
/// </summary>

public class CommandBase : ICommand
{
    readonly Action<object> execute;
    readonly Predicate<object> canExecute;

    public CommandBase(Action<object> executeDelegate, Predicate<object> canExecuteDelegate)
    {
        execute = executeDelegate;
        canExecute = canExecuteDelegate;
    }

    bool ICommand.CanExecute(object parameter)
    {
        return canExecute == null ? true : canExecute(parameter);
    }

    event EventHandler ICommand.CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    void ICommand.Execute(object parameter)
    {
        execute(parameter);
    }
}

/// <summary>
/// I like this implementation of ViewModelBase
/// http://codereview.stackexchange.com/questions/13823/improvements-to-a-viewmodelbase
/// 
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{


    public bool IsInDesignMode
    {

        get
        {
#if NETFX_CORE
                return Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#elif SILVERLIGHT // includes WINDOWS_PHONE
            return System.ComponentModel.DesignerProperties.IsInDesignTool;
#else //WPF
            return System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());
#endif
        }

    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged(string propertyName)
    {
        var propertyChanged = this.PropertyChanged;

        if (propertyChanged != null)
        {
            propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    /// <summary>
    /// Encapsulates the notion of setting the value of a backing field within a property.
    ///     Instead of explicitly calling RaisePropertyChanged, use SetProperty.  
    /// NOTE: The notify event is only raised if the property is indeed different. If you need the
    ///         event to be raised without the property being different, consider WTF, 
    ///         then call RaisePropertyChanged directly.
    /// </summary>
    /// <typeparam name="T">The viewmodel _instance.  Example: this.SetProperty</typeparam>
    /// <param name="backingField"></param>
    /// <param name="Value">Example: 'value'</param>
    /// <param name="propertyExpression">Example: () => this.PropertyName</param>
    /// <returns>true implies backingField was changed, false implies backingField was not changed</returns>
    protected bool SetProperty<T>(ref T backingField, T Value, Expression<Func<T>> propertyExpression)
    {
        var changed = !EqualityComparer<T>.Default.Equals(backingField, Value);

        if (changed)
        {
            backingField = Value;
            this.RaisePropertyChanged(ExtractPropertyName(propertyExpression));
        }

        return changed;
    }

    private static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
    {
        var memberExp = propertyExpression.Body as MemberExpression;

        if (memberExp == null)
        {
            throw new ArgumentException("Expression must be a MemberExpression.", "propertyExpression");
        }

        return memberExp.Member.Name;
    }
}
