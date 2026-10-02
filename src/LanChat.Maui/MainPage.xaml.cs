using System.Collections.Specialized;
using LanChat.Maui.ViewModels;

namespace LanChat.Maui;

public partial class MainPage : ContentPage
{
	private readonly MainChatViewModel _viewModel;

	public MainPage()
	{
		InitializeComponent();
		_viewModel = new MainChatViewModel();
		BindingContext = _viewModel;

		_viewModel.Manager.Messages.CollectionChanged += OnMessagesChanged;
	}

	private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Add && _viewModel.Manager.Messages.Count > 0)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var lastItem = _viewModel.Manager.Messages[^1];
					MessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
				}
				catch { }
			});
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_viewModel.Manager.Dispose();
	}
}
