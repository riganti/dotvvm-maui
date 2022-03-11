using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Maui;

namespace DotVVM.Hosting.Maui.Controls
{
	/// <summary>
	/// Defines a contract for a view that renders Blazor content.
	/// </summary>
	public interface IDotvvmWebView : IView
	{
		/// <summary>
		/// Gets the path to the HTML file to render.
		/// </summary>
		string? HostPage { get; }

		/// <summary>
		/// Gets or sets the name of the currently displayed route.
		/// </summary>
		string RouteName { get; set; }

		/// <summary>
		/// Allows customizing how external links are opened.
		/// Opens external links in the system browser by default.
		/// </summary>
		event EventHandler<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;

		/// <summary>
		/// Creates a file provider for static assets used in the <see cref="DotvvmWebView"/>. The default implementation
		/// serves files from a platform-specific location. Override this method to return a custom <see cref="IFileProvider"/> to serve assets such
		/// as <c>wwwroot/index.html</c>. Call the base method and combine its return value with a <see cref="CompositeFileProvider"/>
		/// to use both custom assets and default assets.
		/// </summary>
		/// <param name="contentRootDir">The base directory to use for all requested assets, such as <c>wwwroot</c>.</param>
		/// <returns>Returns a <see cref="IFileProvider"/> for static assets.</returns>
		public IFileProvider CreateFileProvider(string contentRootDir);
	}
}
