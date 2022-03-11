using System;
using System.IO;
using DotVVM.Hosting.Maui.Services;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;

namespace DotVVM.Hosting.Maui.Controls
{
	/// <summary>
	/// A <see cref="ViewHandler"/> for <see cref="BlazorWebView"/>.
	/// </summary>
	public partial class DotvvmWebViewHandler : ViewHandler<IDotvvmWebView, WebView2Control>
	{
		private WebView2WebViewManager? _webviewManager;

		/// <inheritdoc />
		protected override WebView2Control CreateNativeView()
		{
			return new WebView2Control();
		}

        protected override void ConnectHandler(WebView2Control nativeView)
        {
            base.ConnectHandler(nativeView);

			StartWebViewCoreIfPossible();
        }

        /// <inheritdoc />
        protected override void DisconnectHandler(WebView2Control platformView)
		{
			if (_webviewManager != null)
			{
				// Dispose this component's contents and block on completion so that user-written disposal logic and
				// DotVVM disposal logic will complete.
				_webviewManager?
					.DisposeAsync()
					.AsTask()
					.GetAwaiter()
					.GetResult();

				_webviewManager = null;
			}
		}

		private bool RequiredStartupPropertiesSet =>
			Services != null
			&& (!string.IsNullOrEmpty(RouteName) || !string.IsNullOrEmpty(Url));

		private void StartWebViewCoreIfPossible()
		{
			if (!RequiredStartupPropertiesSet ||
				_webviewManager != null)
			{
				return;
			}
            if (NativeView == null)
            {
                throw new InvalidOperationException($"Can't start {nameof(DotvvmWebView)} without native web view instance.");
            }

			_webviewManager = new WinUIWebViewManager(
				NativeView,
				Services!,
				Dispatcher.GetForCurrentThread()!,
				Services!.GetRequiredService<DotvvmWebRequestHandler>(),
				this);

			_webviewManager.MessageReceived += OnMessageReceived;

			// triggers the navigation to the default route
			if (!string.IsNullOrEmpty(RouteName))
			{
				NavigateToRoute(RouteName);
			}
			else
            {
				_webviewManager.Navigate(Url);
            }
		}
	}
}
