using DotVVM.Hosting.Maui;
using DotVVM.Maui.App.HostedApp;

namespace DotVVM.Maui.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		var path = Path.Combine(Path.GetDirectoryName(typeof(MauiProgram).Assembly.Location), "HostedApp");
		builder.AddMauiDotvvmWebView<DotvvmStartup, DotvvmStartup>(path, true);

		return builder.Build();
	}
}
