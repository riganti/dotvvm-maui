using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Maui.Controls;

namespace DotVVM.Hosting.Maui.Controls
{
    /// <summary>
    /// A <see cref="View"/> that can render Blazor content.
    /// </summary>
    public class DotvvmWebView : View, IDotvvmWebView
	{
		internal const string AppHostAddress = "0.0.0.0";


		/// <summary>
		/// Initializes a new instance of <see cref="DotvvmWebView"/>.
		/// </summary>
		public DotvvmWebView()
		{
		}

		/// <summary>
		/// Gets or sets the path to the HTML file to render.
		/// <para>This is an app relative path to the file such as <c>wwwroot\index.html</c></para>
		/// </summary>
		public string? HostPage { get; set; }

		/// <summary>
		/// Gets or sets the name of the currently open route.
		/// </summary>
        public string RouteName { get; set; }

        /// <inheritdoc/>
        public event EventHandler<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;

		/// <inheritdoc/>
		public virtual IFileProvider CreateFileProvider(string contentRootDir)
		{
			// Call into the platform-specific code to get that platform's asset file provider
			return ((DotvvmWebViewHandler)(Handler!)).CreateFileProvider(contentRootDir);
		}

		internal void NotifyExternalNavigationStarting(ExternalLinkNavigationEventArgs args)
		{
			ExternalNavigationStarting?.Invoke(this, args);
		}
	}
}
