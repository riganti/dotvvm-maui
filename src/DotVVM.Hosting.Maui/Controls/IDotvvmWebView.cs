using System;

namespace DotVVM.Hosting.Maui.Controls;

/// <summary>
/// Defines a contract for a view that renders Blazor content.
/// </summary>
public interface IDotvvmWebView : IView
{
	
	/// <summary>
	/// Gets or sets the route on which the app should be navigated.
	/// </summary>
	string RouteName { get; set; }

	/// <summary>
	/// Gets or sets the URL on which the page should be navigated.
	/// </summary>
	string Url { get; set; }

	/// <summary>
	/// Obtains the current snapshot of the DotVVM page.
	/// </summary>
	Task<dynamic> GetViewModelSnapshot();

	/// <summary>
	/// Patches the current DotVVM page viewmodel with the specified data.
	/// </summary>
	Task PatchViewModel(dynamic patch);

	/// <summary>
	/// Calls the specified named command with the specified arguments.
	/// </summary>
	/// <param name="viewId"></param>
	/// <param name="commandName"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	Task<dynamic> CallNamedCommand(string viewId, string commandName, params object[] args);

	/// <summary>
	/// Occurs when the page tries to notify the host window using _page.NotifyHost(methodName, args...)
	/// </summary>
	event EventHandler<PageNotificationEventArgs>? PageNotificationReceived;

	/// <summary>
	/// Allows customizing how external links are opened.
	/// Opens external links in the system browser by default.
	/// </summary>
	event EventHandler<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;

}
