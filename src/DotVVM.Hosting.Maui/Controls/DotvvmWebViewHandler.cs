using System;
using System.Linq;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;

namespace DotVVM.Hosting.Maui.Controls
{
	public partial class DotvvmWebViewHandler
	{
		/// <summary>
		/// This field is part of MAUI infrastructure and is not intended for use by application code.
		/// </summary>
		public static readonly PropertyMapper<IDotvvmWebView, DotvvmWebViewHandler> DotvvmWebViewMapper = new(ViewMapper)
		{
			[nameof(IDotvvmWebView.HostPage)] = MapHostPage,
			[nameof(IDotvvmWebView.RouteName)] = MapRouteName,
			[nameof(IDotvvmWebView.ExternalNavigationStarting)] = MapNotifyExternalNavigationStarting
		};

		/// <summary>
		/// Initializes a new instance of <see cref="DotvvmWebViewHandler"/> with default mappings.
		/// </summary>
		public DotvvmWebViewHandler() : this(DotvvmWebViewMapper)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="DotvvmWebViewHandler"/> using the specified mappings.
		/// </summary>
		/// <param name="mapper">The property mappings.</param>
		public DotvvmWebViewHandler(PropertyMapper? mapper) : base(mapper ?? DotvvmWebViewMapper)
		{
		}

		/// <summary>
		/// Maps the <see cref="IDotvvmWebView.HostPage"/> property to the specified handler.
		/// </summary>
		/// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
		/// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
		public static void MapHostPage(DotvvmWebViewHandler handler, IDotvvmWebView webView)
		{
#if !NETSTANDARD
			handler.HostPage = webView.HostPage;
			handler.StartWebViewCoreIfPossible();
#endif
		}

		/// <summary>
		/// Maps the <see cref="IDotvvmWebView.RootComponents"/> property to the specified handler.
		/// </summary>
		/// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
		/// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
		public static void MapRouteName(DotvvmWebViewHandler handler, IDotvvmWebView webView)
		{
#if !NETSTANDARD
			handler.RouteName = webView.RouteName;
			handler.StartWebViewCoreIfPossible();
#endif
		}

		/// <summary>
		/// Maps the <see cref="DotvvmWebView.NotifyExternalNavigationStarting"/> property to the specified handler.
		/// </summary>
		/// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
		/// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
		public static void MapNotifyExternalNavigationStarting(DotvvmWebViewHandler handler, IDotvvmWebView webView)
		{
#if !NETSTANDARD
			if (webView is DotvvmWebView dwv)
			{
				handler.ExternalNavigationStarting = dwv.NotifyExternalNavigationStarting;
			}
#endif
		}

#if !NETSTANDARD
		private string? HostPage { get; set; }
		internal Action<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;

		private string routeName;
		private string RouteName
		{
			get => routeName;
			set
			{
				if (routeName != value)
                {
					routeName = value;
					OnRouteNameChanged();
                }				
			}
		}

		private void OnRouteNameChanged()
		{
			// If we haven't initialized yet, this is a no-op
			if (_webviewManager != null)
			{
				// Dispatch because this is going to be async, and we want to catch any errors
				_ = _webviewManager.Dispatcher.InvokeAsync(async () =>
				{
					// TODO
					//_webViewManager.SetRoute(routeName);
				});
			}
		}
#endif
	}
}