using System;
using Avalonia;
using Avalonia.Controls;
using LanChat.ViewModels;

namespace LanChat.Views;

public partial class MainWindow : Window
{
    private bool _isRealExit = false;
    private TrayIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        SetupTrayIcon();

        Closing += OnWindowClosing;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RequestExit = ExitApplication;
            vm.RequestShowWindow = ShowWindow;
        }
    }

    private void SetupTrayIcon()
    {
        try
        {
            _trayIcon = new TrayIcon
            {
                Icon = this.Icon,
                ToolTipText = "LAN Chat",
                IsVisible = true
            };

            var menu = new NativeMenu();
            var openItem = new NativeMenuItem("Buka LAN Chat / Open");
            openItem.Click += (s, e) => ShowWindow();
            menu.Items.Add(openItem);

            menu.Items.Add(new NativeMenuItemSeparator());

            var exitItem = new NativeMenuItem("Keluar / Exit");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            _trayIcon.Menu = menu;
            _trayIcon.Clicked += (s, e) => ShowWindow();

            var trayIcons = new TrayIcons { _trayIcon };
            TrayIcon.SetIcons(Application.Current!, trayIcons);
        }
        catch
        {
            // Platform fallback jika sistem tidak mendukung system tray
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isRealExit)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void ExitApplication()
    {
        _isRealExit = true;
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = false;
            _trayIcon.Dispose();
        }
        Close();
    }
}