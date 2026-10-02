using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using LanChat.Wpf.ViewModels;

namespace LanChat.Wpf;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainWindowViewModel();
        DataContext = _vm;

        ((INotifyCollectionChanged)_vm.MessagesView).CollectionChanged += OnMessagesChanged;
        Closed += MainWindow_Closed;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.InvokeAsync(() =>
            {
                MessagesScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            if (_vm.SendMessageCommand.CanExecute(null))
            {
                _vm.SendMessageCommand.Execute(null);
            }
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _vm.Manager.Dispose();
    }
}