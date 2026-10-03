using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using LanChat.Maui.Services;
using LanChat.Maui.ViewModels;
using Microsoft.Maui.Controls;

namespace LanChat.Maui;

public partial class MainPage : ContentPage
{
	private readonly MainChatViewModel _viewModel;

	public MainPage()
	{
		InitializeComponent();
		_viewModel = new MainChatViewModel();
		BindingContext = _viewModel;

		_viewModel.RequestOpenDrawer += OpenDrawerAsync;
		_viewModel.RequestCloseDrawer += CloseDrawerAsync;

		_viewModel.FilteredMessages.CollectionChanged += OnMessagesChanged;

		// Listen to keyboard height changes on Android
		KeyboardHelper.KeyboardHeightChanged += OnKeyboardHeightChanged;
	}

	private void OnKeyboardHeightChanged(double keyboardHeightDp)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			MainChatGrid.Padding = new Thickness(0, 0, 0, keyboardHeightDp);

			if (keyboardHeightDp > 0 && _viewModel.FilteredMessages.Count > 0)
			{
				try
				{
					var lastItem = _viewModel.FilteredMessages[^1];
					MessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: false);
					await Task.Delay(80);
					MessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: false);
				}
				catch { }
			}
		});
	}

	public async Task OpenDrawerAsync()
	{
		LeftPanel.TranslationX = -320;
		LeftPanel.IsVisible = true;
		Backdrop.IsVisible = true;
		await Task.WhenAll(
			Backdrop.FadeTo(0.4, 250, Easing.CubicOut),
			LeftPanel.TranslateTo(0, 0, 250, Easing.CubicOut)
		);
	}

	public async Task CloseDrawerAsync()
	{
		await Task.WhenAll(
			Backdrop.FadeTo(0, 200, Easing.CubicIn),
			LeftPanel.TranslateTo(-320, 0, 200, Easing.CubicIn)
		);
		Backdrop.IsVisible = false;
		LeftPanel.IsVisible = false;
	}

	private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Add && _viewModel.FilteredMessages.Count > 0)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var lastItem = _viewModel.FilteredMessages[^1];
					MessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
				}
				catch { }
			});
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		KeyboardHelper.KeyboardHeightChanged -= OnKeyboardHeightChanged;
		_viewModel.Manager.Dispose();
	}
}
