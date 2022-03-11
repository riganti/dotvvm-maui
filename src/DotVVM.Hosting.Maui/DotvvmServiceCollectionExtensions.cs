using System;
#if WEBVIEW2_WINFORMS
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
#elif WEBVIEW2_WPF
using Microsoft.AspNetCore.Components.WebView.Wpf;
#elif WEBVIEW2_MAUI
using Microsoft.Maui.Hosting;
#else
#error Must define WEBVIEW2_WINFORMS, WEBVIEW2_WPF, WEBVIEW2_MAUI
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotVVM.Hosting.Maui.Controls;
using DotVVM.Framework.Configuration;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using DotVVM.Hosting.Maui.Services;

namespace DotVVM.Hosting.Maui
{
    public static class DotvvmServiceCollectionExtensions
    {

		/// <summary>
		/// Configures <see cref="MauiAppBuilder"/> to add support for <see cref="DotvvmWebView"/>.
		/// </summary>
		/// <param name="builder">The <see cref="MauiAppBuilder"/>.</param>
		/// <returns>The <see cref="MauiAppBuilder"/>.</returns>
#if WEBVIEW2_WINFORMS
		public static MauiAppBuilder AddWindowsFormsDotvvmWebView<TDotvvmStartup, TDotvvmServiceConfigurator>(
#elif WEBVIEW2_WPF
		public static MauiAppBuilder AddWpfDotvvmWebView<TDotvvmStartup, TDotvvmServiceConfigurator>(
#elif WEBVIEW2_MAUI
		public static MauiAppBuilder AddMauiDotvvmWebView<TDotvvmStartup, TDotvvmServiceConfigurator>(
#else
#error Must define WEBVIEW2_WINFORMS, WEBVIEW2_WPF, WEBVIEW2_MAUI
#endif
			this MauiAppBuilder builder, 
			string applicationPath,
			bool debug = false,
			Action<DotvvmConfiguration> configure = null)
			where TDotvvmStartup : IDotvvmStartup, new()
			where TDotvvmServiceConfigurator : IDotvvmServiceConfigurator, new()

		{
			builder.ConfigureMauiHandlers(static handlers => handlers.AddHandler<IDotvvmWebView, DotvvmWebViewHandler>());
			
			builder.Services.AddDotVVM<TDotvvmServiceConfigurator>();
			builder.Services.AddSingleton<IWebHostEnvironment>(new DotvvmWebHostEnvironment()
			{
				EnvironmentName = debug ? "Development" : "Production",
				ApplicationName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
				ContentRootPath = applicationPath,
				WebRootPath = Path.Combine(applicationPath, "wwwroot")
			});
			builder.Services.AddSingleton<RequestDelegate>(provider =>
			{
				var factory = new ApplicationBuilderFactory(provider);
				var appBuilder = factory.CreateBuilder(new FeatureCollection());
				appBuilder.UseDotVVM<TDotvvmStartup>(applicationPath, debug, configure);
				return appBuilder.Build();
			});
			builder.Services.AddSingleton<DotvvmWebRequestHandler>();

			return builder;
		}

	}
}
