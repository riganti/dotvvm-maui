// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !WEBVIEW2_WINFORMS && !WEBVIEW2_WPF && !WEBVIEW2_MAUI
#error Must specify which WebView2 is targeted
#endif

#if WINDOWS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if WEBVIEW2_WINFORMS
using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;
#elif WEBVIEW2_WPF
using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.Wpf.WebView2;
#elif WEBVIEW2_MAUI
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;
using Launcher = Windows.System.Launcher;
using DotVVM.Hosting.Maui.Controls;
using Microsoft.Maui.Dispatching;
#endif

namespace DotVVM.Hosting.Maui.Services
{
	/// <summary>
	/// An implementation of <see cref="WebViewManager"/> that uses the Edge WebView2 browser control
	/// to render web content.
	/// </summary>
	public class WebView2WebViewManager : WebViewManager
	{
		// Using an IP address means that WebView2 doesn't wait for any DNS resolution,
		// making it substantially faster. Note that this isn't real HTTP traffic, since
		// we intercept all the requests within this origin.
		internal static readonly string AppHostAddress = "0.0.0.0";

		/// <summary>
		/// Gets the application's base URI. Defaults to <c>https://0.0.0.0/</c>
		/// </summary>
		protected static readonly string AppOrigin = $"https://{AppHostAddress}/";

		private readonly WebView2Control _webview;
        private readonly DotvvmWebRequestHandler _dotvvmWebRequestHandler;
		private readonly DotvvmWebViewHandler _dotvvmWebViewHandler;
        private readonly Task _webviewReadyTask;

#if WEBVIEW2_WINFORMS || WEBVIEW2_WPF
		private protected CoreWebView2Environment _coreWebView2Environment;
		private readonly Action<ExternalLinkNavigationEventArgs> _externalNavigationStarting;

		/// <summary>
		/// Constructs an instance of <see cref="WebView2WebViewManager"/>.
		/// </summary>
		/// <param name="webview">A <see cref="WebView2Control"/> to access platform-specific WebView2 APIs.</param>
		/// <param name="services">A service provider containing services to be used by this class and also by application code.</param>
		/// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
		/// <param name="dotvvmWebRequestHandler">Provides static content to the webview.</param>
		/// <param name="dotvvmWebViewHandler">The <see cref="DotvvmWebViewHandler" />.</param>
		/// <param name="externalNavigationStarting">Callback invoked when external navigation starts.</param>
		public WebView2WebViewManager(
			WebView2Control webview!!,
			IServiceProvider services,
			Dispatcher dispatcher,
            DotvvmWebRequestHandler dotvvmWebRequestHandler,
            DotvvmWebViewHandler dotvvmWebViewHandler
			Action<ExternalLinkNavigationEventArgs> externalNavigationStarting)
			: base(services, dispatcher, new Uri(AppOrigin), fileProvider, jsComponents, hostPageRelativePath)

		{
			_webview = webview;
            _dotvvmWebRequestHandler = dotvvmWebRequestHandler;
            _dotvvmWebViewHandler = dotvvmWebViewHandler;
			_externalNavigationStarting = externalNavigationStarting;

			// Unfortunately the CoreWebView2 can only be instantiated asynchronously.
			// We want the external API to behave as if initalization is synchronous,
			// so keep track of a task we can await during LoadUri.
			_webviewReadyTask = InitializeWebView2();
		}
#elif WEBVIEW2_MAUI
		private protected CoreWebView2Environment? _coreWebView2Environment;

		/// <summary>
		/// Constructs an instance of <see cref="WebView2WebViewManager"/>.
		/// </summary>
		/// <param name="webview">A <see cref="WebView2Control"/> to access platform-specific WebView2 APIs.</param>
		/// <param name="services">A service provider containing services to be used by this class and also by application code.</param>
		/// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
		/// <param name="dotvvmWebRequestHandler">Provides static content to the webview.</param>
		/// <param name="dotvvmWebViewHandler">The <see cref="DotvvmWebViewHandler" />.</param>
		public WebView2WebViewManager(
			WebView2Control webview!!,
			IServiceProvider services,
			IDispatcher dispatcher,
            DotvvmWebRequestHandler dotvvmWebRequestHandler,
            DotvvmWebViewHandler dotvvmWebViewHandler
        )
			: base(services, dispatcher, new Uri(AppOrigin))
		{
			_webview = webview;
            _dotvvmWebRequestHandler = dotvvmWebRequestHandler;
            _dotvvmWebViewHandler = dotvvmWebViewHandler;

			// Unfortunately the CoreWebView2 can only be instantiated asynchronously.
			// We want the external API to behave as if initalization is synchronous,
			// so keep track of a task we can await during LoadUri.
			_webviewReadyTask = InitializeWebView2();
		}
#endif

		/// <inheritdoc />
		protected override void NavigateCore(Uri absoluteUri)
		{
			_ = Dispatcher.DispatchAsync(async () =>
			{
				await _webviewReadyTask;
				_webview.Source = absoluteUri;
			});
		}

		/// <inheritdoc />
		public override void SendMessage(string message)
			=> _webview.CoreWebView2.PostWebMessageAsString(message);

		private async Task InitializeWebView2()
		{
			_coreWebView2Environment = await CoreWebView2Environment.CreateAsync()
#if WEBVIEW2_MAUI
				.AsTask()
#endif
				.ConfigureAwait(true);
			await _webview.EnsureCoreWebView2Async();

			ApplyDefaultWebViewSettings();

			_webview.CoreWebView2.AddWebResourceRequestedFilter($"{AppOrigin}*", CoreWebView2WebResourceContext.All);

			_webview.CoreWebView2.WebResourceRequested += async (s, eventArgs) =>
			{
				await HandleWebResourceRequest(eventArgs);
			};

			_webview.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
			_webview.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

			// The code inside dotvvm.webview.js is meant to be agnostic to specific webview technologies,
			// so the following is an adaptor from dotvvm.webview.js conventions to WebView2 APIs
			await _webview.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                window.external = {
                    sendMessage: message => {
                        window.chrome.webview.postMessage(message);
                    },
                    receiveMessage: callback => {
                        window.chrome.webview.addEventListener('message', e => callback(e.data));
                    }
                };
            ")
#if WEBVIEW2_MAUI
				.AsTask()
#endif
				.ConfigureAwait(true);

			_webview.CoreWebView2.WebMessageReceived += (s, e) => OnMessageReceived(new Uri(e.Source), e.WebMessageAsJson);
		}

		/// <summary>
		/// Handles outbound URL requests.
		/// </summary>
		/// <param name="eventArgs">The <see cref="CoreWebView2WebResourceRequestedEventArgs"/>.</param>
		protected virtual async Task HandleWebResourceRequest(CoreWebView2WebResourceRequestedEventArgs eventArgs)
		{
#if WEBVIEW2_WINFORMS || WEBVIEW2_WPF
			var requestUri = new Uri(eventArgs.Request.Uri);
			var response = await dotvvmWebRequestHandler.ProcessRequest(requestUri, eventArgs.Request.Method, eventArgs.Request.Headers, eventArgs.Request.Content?.AsStreamForRead());
			eventArgs.Response = _coreWebView2Environment.CreateWebResourceResponse(response.Content, response.StatusCode, ((HttpStatusCode)response.StatusCode).ToString(), GetHeaderString(response.Headers);
#elif WEBVIEW2_MAUI
			// No-op here because all the work is done in the derived WinUIWebViewManager
#endif
		}

		private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs args)
		{
			if (Uri.TryCreate(args.Uri, UriKind.RelativeOrAbsolute, out var uri) && uri.Host != AppHostAddress)
			{
				var callbackArgs = new ExternalLinkNavigationEventArgs(uri);

#if WEBVIEW2_WINFORMS || WEBVIEW2_WPF
				_externalNavigationStarting?.Invoke(callbackArgs);
#elif WEBVIEW2_MAUI
				_dotvvmWebViewHandler.ExternalNavigationStarting?.Invoke(callbackArgs);
#endif

				if (callbackArgs.ExternalLinkNavigationPolicy == ExternalLinkNavigationPolicy.OpenInExternalBrowser)
				{
					LaunchUriInExternalBrowser(uri);
				}

				args.Cancel = callbackArgs.ExternalLinkNavigationPolicy != ExternalLinkNavigationPolicy.InsecureOpenInWebView;
			}
		}

		private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs args)
		{
			// Intercept _blank target <a> tags to always open in device browser.
			// The ExternalLinkCallback is not invoked.
			if (Uri.TryCreate(args.Uri, UriKind.RelativeOrAbsolute, out var uri))
			{
				LaunchUriInExternalBrowser(uri);
				args.Handled = true;
			}
		}

		private void LaunchUriInExternalBrowser(Uri uri)
		{
#if WEBVIEW2_WINFORMS || WEBVIEW2_WPF
			using (var launchBrowser = new Process())
			{
				launchBrowser.StartInfo.UseShellExecute = true;
				launchBrowser.StartInfo.FileName = uri.ToString();
				launchBrowser.Start();
			}
#elif WEBVIEW2_MAUI
			_ = Launcher.LaunchUriAsync(uri);
#endif
		}

		private protected static string GetHeaderString(IEnumerable<KeyValuePair<string, string>> headers) =>
			string.Join(Environment.NewLine, headers.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

		private void ApplyDefaultWebViewSettings()
		{
			_webview.CoreWebView2.Settings.AreDevToolsEnabled = true;

			// Desktop applications typically don't want the default web browser context menu
			_webview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

			// Desktop applications almost never want to show a URL preview when hovering over a link
			_webview.CoreWebView2.Settings.IsStatusBarEnabled = false;
		}
	}
}
#endif