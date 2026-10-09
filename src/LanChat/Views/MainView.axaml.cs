using System;
using Avalonia;
using Avalonia.Controls;
using LanChat.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using LanChat.Services;
using LanChat.ViewModels;

namespace LanChat.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is MainViewModel vm)
        {
            vm.FilePickerService = new AvaloniaFilePickerService(() => TopLevel.GetTopLevel(this)?.StorageProvider);
            vm.RequestScrollToBottom -= ScrollChatToBottom;
            vm.RequestScrollToBottom += ScrollChatToBottom;
            ScrollChatToBottom();

            UpdateLayoutMode(vm.IsMobileMode);
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.InputPane != null)
        {
            topLevel.InputPane.StateChanged -= OnInputPaneStateChanged;
            topLevel.InputPane.StateChanged += OnInputPaneStateChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.InputPane != null)
        {
            topLevel.InputPane.StateChanged -= OnInputPaneStateChanged;
        }
    }

    private void OnInputPaneStateChanged(object? sender, Avalonia.Controls.Platform.InputPaneStateEventArgs e)
    {
        var rootGrid = this.FindControl<Grid>("RootLayoutGrid");
        if (rootGrid == null) return;

        double bottomMargin = 0;
        if (e.NewState == Avalonia.Controls.Platform.InputPaneState.Open)
        {
            bottomMargin = e.EndRect.Height;
        }

        rootGrid.Margin = new Thickness(0, 0, 0, bottomMargin);

        if (bottomMargin > 0)
        {
            ScrollChatToBottom();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsMobileMode))
        {
            if (DataContext is MainViewModel vm)
            {
                UpdateLayoutMode(vm.IsMobileMode);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.IsImageViewerOpen))
        {
            if (DataContext is MainViewModel vm && vm.IsImageViewerOpen)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var container = this.FindControl<PinchZoomContainer>("ImageZoomContainer");
                    container?.Reset();
                }, DispatcherPriority.Loaded);
            }
        }
    }

    private void UpdateLayoutMode(bool isMobile)
    {
        var grid = this.FindControl<Grid>("MainContentGrid");
        if (grid != null)
        {
            grid.ColumnDefinitions = ColumnDefinitions.Parse(isMobile ? "*" : "310, *");
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (DataContext is MainViewModel vm)
        {
            if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS())
            {
                vm.IsMobileMode = e.NewSize.Width < 720;
            }
        }
    }

    private void ScrollChatToBottom()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var sv = this.FindControl<ScrollViewer>("MessageScrollViewer");
            sv?.ScrollToEnd();
        }, DispatcherPriority.Loaded);

        Dispatcher.UIThread.Post(async () =>
        {
            await System.Threading.Tasks.Task.Delay(60);
            var sv = this.FindControl<ScrollViewer>("MessageScrollViewer");
            sv?.ScrollToEnd();
        }, DispatcherPriority.Background);
    }
}