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

namespace DotVVM.Hosting.Maui
{
    public static class DotvvmServiceCollectionExtensions
    {

		/// <summary>
		/// Configures <see cref="IServiceCollection"/> to add support for <see cref="DotvvmWebView"/>.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/>.</returns>
#if WEBVIEW2_WINFORMS
		public static MauiAppBuilder AddWindowsFormsDotvvmWebView(this MauiAppBuilder builder)
#elif WEBVIEW2_WPF
		public static MauiAppBuilder AddWpfDotvvmWebView(this MauiAppBuilder builder)
#elif WEBVIEW2_MAUI
		public static MauiAppBuilder AddMauiDotvvmWebView<TDotvvmStartup, TDotvvmServiceConfigurator>(this MauiAppBuilder builder, string applicationPath)
			where TDotvvmStartup : IDotvvmStartup, new()
			where TDotvvmServiceConfigurator : IDotvvmServiceConfigurator, new()
#else
#error Must define WEBVIEW2_WINFORMS, WEBVIEW2_WPF, WEBVIEW2_MAUI
#endif
		{
#if WEBVIEW2_MAUI
			// TODO
			// services.TryAddSingleton<MauiDotvvmMarkerService>();
			builder.ConfigureMauiHandlers(static handlers => handlers.AddHandler<IDotvvmWebView, DotvvmWebViewHandler>());
#elif WEBVIEW2_WINFORMS
			services.TryAddSingleton<WindowsFormsDotvvmMarkerService>();
#elif WEBVIEW2_WPF
			services.TryAddSingleton<WpfDotvvmMarkerService>();
#endif

			builder.Services.AddDotVVM<TDotvvmServiceConfigurator>();
			builder.Services.AddSingleton<IWebHostEnvironment>(new DotvvmWebHostEnvironment()
            {
				ContentRootPath = applicationPath,
				WebRootPath = Path.Combine(applicationPath, "wwwroot")				
            });
			builder.Services.AddSingleton<RequestDelegate>(provider =>
			{
				var factory = new ApplicationBuilderFactory(provider);
				var serverFeatures = new FeatureCollection();
				serverFeatures.Set(new Microsoft.AspNetCore.Http.Features.HttpRequestFeature());
				serverFeatures.Set(new Microsoft.AspNetCore.Http.Features.HttpResponseFeature());
				var appBuilder = factory.CreateBuilder(serverFeatures);
				appBuilder.UseDotVVM<TDotvvmStartup>(applicationPath, true);
				return appBuilder.Build();
			});

			return builder;
		}

	}
}
