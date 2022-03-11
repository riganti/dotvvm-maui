﻿using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Maui.Dispatching;
using Microsoft.Web.WebView2.Core;
using Windows.ApplicationModel;
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
		private readonly WebView2Control _webview;
		private readonly string _hostPageRelativePath;
		private readonly string _contentRootDir;
        private readonly RequestDelegate requestDelegate;
        private IServiceScopeFactory serviceScopeFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="WinUIWebViewManager"/>
        /// </summary>
        /// <param name="webview">A <see cref="WebView2Control"/> to access platform-specific WebView2 APIs.</param>
        /// <param name="services">A service provider containing services to be used by this class and also by application code.</param>
        /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
        /// <param name="fileProvider">Provides static content to the webview.</param>
        /// <param name="jsComponents">The <see cref="JSComponentConfigurationStore"/>.</param>
        /// <param name="hostPageRelativePath">Path to the host page within the <paramref name="fileProvider"/>.</param>
        /// <param name="contentRootDir">Path to the directory containing application content files.</param>
        /// <param name="webViewHandler">The <see cref="BlazorWebViewHandler" />.</param>
        public WinUIWebViewManager(
			WebView2Control webview,
			IServiceProvider services,
			Microsoft.AspNetCore.Components.Dispatcher dispatcher,
			IFileProvider fileProvider,
			string hostPageRelativePath,
			string contentRootDir,
			DotvvmWebViewHandler webViewHandler)
			: base(webview, services, dispatcher, fileProvider, hostPageRelativePath, webViewHandler)
		{
			_webview = webview;
			_hostPageRelativePath = hostPageRelativePath;
			_contentRootDir = contentRootDir;

			requestDelegate = services.GetRequiredService<RequestDelegate>();
			serviceScopeFactory = services.GetService<IServiceScopeFactory>();
		}

		/// <inheritdoc />
		protected override async Task HandleWebResourceRequest(CoreWebView2WebResourceRequestedEventArgs eventArgs)
		{
			// Unlike server-side code, we get told exactly why the browser is making the request,
			// so we can be smarter about fallback. We can ensure that 'fetch' requests never result
			// in fallback, for example.
			var allowFallbackOnHostPage =
				eventArgs.ResourceContext == CoreWebView2WebResourceContext.Document ||
				eventArgs.ResourceContext == CoreWebView2WebResourceContext.Other; // e.g., dev tools requesting page source

			// Get a deferral object so that WebView2 knows there's some async stuff going on. We call Complete() at the end of this method.
			using var deferral = eventArgs.GetDeferral();

			// TODO
			//var requestUri = QueryStringHelper.RemovePossibleQueryString(eventArgs.Request.Uri);
			var requestUri = new Uri(eventArgs.Request.Uri);

			// create request
			using var scope = serviceScopeFactory.CreateScope();
			var httpContext = new DefaultHttpContext()
			{
				Request =
                {
					Scheme = requestUri.Scheme,
					Host = new HostString(requestUri.Host, requestUri.Port),
					Path = requestUri.PathAndQuery,
					Method = eventArgs.Request.Method
				},
				Response =
                {
					Body = new MemoryStream()
                },
				ServiceScopeFactory = serviceScopeFactory,
				RequestServices = scope.ServiceProvider
			};
			foreach (var header in eventArgs.Request.Headers)
            {
				httpContext.Request.Headers.Add(header.Key, header.Value);
            }
			using var contentStream = eventArgs.Request.Content?.AsStreamForRead();
			if (contentStream != null)
            {
				httpContext.Request.Body = contentStream;
            }
			await requestDelegate(httpContext);

			// First, call into WebViewManager to see if it has a framework file for this request. It will
			// fall back to an IFileProvider, but on WinUI it's always a NullFileProvider, so that will never
			// return a file.
			//if (TryGetResponseContent(requestUri, allowFallbackOnHostPage, out var statusCode, out var statusMessage, out var content, out var headers)
			//	&& statusCode != 404)
			//{
			// NOTE: This is stream copying is to work around a hanging bug in WinRT with managed streams.
			// See issue https://github.com/microsoft/CsWinRT/issues/670
			var ms = new InMemoryRandomAccessStream();
			await ms.WriteAsync(((MemoryStream)httpContext.Response.Body).GetWindowsRuntimeBuffer());

			var headerString = GetHeaderString(httpContext.Response.Headers);
			eventArgs.Response = _coreWebView2Environment!.CreateWebResourceResponse(ms, httpContext.Response.StatusCode, ((HttpStatusCode)httpContext.Response.StatusCode).ToString(), headerString);
			//}
			//else
			//{
			//	// Next, try to go through WinUI Storage to find a static web asset
			//	var uri = new Uri(requestUri);
			//	if (new Uri(AppOrigin).IsBaseOf(uri))
			//	{
			//		var relativePath = new Uri(AppOrigin).MakeRelativeUri(uri).ToString();
			//		if (allowFallbackOnHostPage && string.IsNullOrEmpty(relativePath))
			//		{
			//			relativePath = _hostPageRelativePath;
			//		}
			//		relativePath = Path.Combine(_contentRootDir, relativePath.Replace('/', '\\'));

			//		var winUIItem = await Package.Current.InstalledLocation.TryGetItemAsync(relativePath);
			//		if (winUIItem != null)
			//		{
			//			statusCode = 200;
			//			statusMessage = "OK";
			//			var contentType = StaticContentProvider.GetResponseContentTypeOrDefault(relativePath);
			//			headers = StaticContentProvider.GetResponseHeaders(contentType);
			//			var headerString = GetHeaderString(headers);
			//			var winUIFile = await Package.Current.InstalledLocation.GetFileAsync(relativePath);

			//			eventArgs.Response = _coreWebView2Environment!.CreateWebResourceResponse(await winUIFile.OpenReadAsync(), statusCode, statusMessage, headerString);
			//		}
			//	}
			//}

			// Notify WebView2 that the deferred (async) operation is complete and we set a response.
			deferral.Complete();
		}

		/// <inheritdoc />
		protected override void QueueDotvvmStart()
		{
			// TODO
			//// In .NET MAUI we use autostart='false' for the Blazor script reference, so we start it up manually in this event
			//_webview.CoreWebView2.DOMContentLoaded += async (_, __) =>
			//{
			//	await _webview.CoreWebView2!.ExecuteScriptAsync(@"
			//		Blazor.start();
			//		");
			//};
		}
	}
}