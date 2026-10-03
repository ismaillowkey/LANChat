using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace LanChat.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light;

		Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>()
			.UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}