using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using DotVVM.Hosting.Maui.Services;
using Microsoft.Maui.Dispatching;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;

namespace DotVVM.Hosting.Maui.Controls
{
	/// <summary>
	/// An implementation of <see cref="WebViewManager"/> that uses the Edge WebView2 browser control
	/// to render web content in WinUI applications.
	/// </summary>
	public class WinUIWebViewManager : WebView2WebViewManager
	{
        private readonly DotvvmWebRequestHandler dotvvmWebRequestHandler;

		/// <summary>
		/// Initializes a new instance of <see cref="WinUIWebViewManager"/>
		/// </summary>
		/// <param name="webview">A <see cref="WebView2Control"/> to access platform-specific WebView2 APIs.</param>
		/// <param name="services">A service provider containing services to be used by this class and also by application code.</param>
		/// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
		/// <param name="dotvvmWebRequestHandler">Provides a handler for DotVVM requests.</param>
		/// <param name="webViewHandler">The <see cref="DotvvmWebViewHandler" />.</param>
		public WinUIWebViewManager(
			WebView2Control webview,
			IServiceProvider services,
			IDispatcher dispatcher,
			DotvvmWebRequestHandler dotvvmWebRequestHandler,
			DotvvmWebViewHandler webViewHandler)
			: base(webview, services, dispatcher, dotvvmWebRequestHandler, webViewHandler)
		{
            this.dotvvmWebRequestHandler = dotvvmWebRequestHandler;
		}

		/// <inheritdoc />
		protected override async Task HandleWebResourceRequest(CoreWebView2WebResourceRequestedEventArgs eventArgs)
		{
			// Get a deferral object so that WebView2 knows there's some async stuff going on. We call Complete() at the end of this method.
			using var deferral = eventArgs.GetDeferral();
			
			var requestUri = new Uri(eventArgs.Request.Uri);
			var response = await dotvvmWebRequestHandler.ProcessRequest(requestUri, eventArgs.Request.Method, eventArgs.Request.Headers, eventArgs.Request.Content?.AsStreamForRead());

			using var ms = new InMemoryRandomAccessStream();
			await ms.WriteAsync(response.Content.GetWindowsRuntimeBuffer());
			
			eventArgs.Response = _coreWebView2Environment!.CreateWebResourceResponse(ms, response.StatusCode, ((HttpStatusCode)response.StatusCode).ToString(), GetHeaderString(response.Headers));

			// Notify WebView2 that the deferred (async) operation is complete and we set a response.
			deferral.Complete();
		}

	}
}
