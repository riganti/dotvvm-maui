using System;
using System.Linq;
using DotVVM.Framework.Configuration;
using DotVVM.Hosting.Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotVVM.Hosting.Maui.Controls
{
	public partial class DotvvmWebViewHandler
	{

		private Lazy<JsonSerializerSettings> serializerSettings = new Lazy<JsonSerializerSettings>(() => DefaultSerializerSettingsProvider.Instance.GetSettingsCopy());

		/// <summary>
		/// This field is part of MAUI infrastructure and is not intended for use by application code.
		/// </summary>
		public static readonly PropertyMapper<IDotvvmWebView, DotvvmWebViewHandler> DotvvmWebViewMapper = new(ViewMapper)
		{
			[nameof(IDotvvmWebView.ExternalNavigationStarting)] = MapNotifyExternalNavigationStarting,
			[nameof(IDotvvmWebView.PageNotificationReceived)] = MapPageNotificationReceived,
			[nameof(IDotvvmWebView.RouteName)] = MapRouteName,
			[nameof(IDotvvmWebView.Url)] = MapUrl
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
		public static void MapRouteName(DotvvmWebViewHandler handler, IDotvvmWebView webView)
		{
			handler.RouteName = webView.RouteName;
			handler.StartWebViewCoreIfPossible();
		}


		/// <summary>
		/// Maps the <see cref="IDotvvmWebView.HostPage"/> property to the specified handler.
		/// </summary>
		/// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
		/// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
		public static void MapUrl(DotvvmWebViewHandler handler, IDotvvmWebView webView)
		{
			handler.Url = webView.Url;
			handler.StartWebViewCoreIfPossible();
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

		/// <summary>
		/// Maps the <see cref="DotvvmWebView.MapPageNotificationReceived"/> property to the specified handler.
		/// </summary>
		/// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
		/// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
		public static void MapPageNotificationReceived(DotvvmWebViewHandler handler, IDotvvmWebView webView)
		{
			if (webView is DotvvmWebView dwv)
			{
				handler.PageNotificationReceived = dwv.NotifyPageNotificationReceived;
			}
		}

		private string routeName;
		public string RouteName
        {
			get 
			{
				return routeName;
			}
			set
            {
				if (!string.IsNullOrEmpty(value) && value != routeName)
				{
					NavigateToRoute(value);
				}
            }
        }

        protected void NavigateToRoute(string value)
        {
			// make sure DotVVM is initialized
			Services.GetRequiredService<DotvvmWebRequestHandler>();

            var route = Services.GetRequiredService<DotvvmConfiguration>().RouteTable[value];
            var url = route.BuildUrl().TrimStart('~');

            routeName = value;
            Url = url;
        }

        public string Url
		{
			get
			{
				return NativeView.Source?.ToString();
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					_webviewManager?.Navigate(value);
				}
			}
		}


#if !NETSTANDARD
		internal Action<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;
#endif

		internal Action<PageNotificationEventArgs>? PageNotificationReceived;


		internal Task<dynamic> CallNamedCommand(string elementSelector, string commandName, object[] args)
		{
			var messageId = Guid.NewGuid();
			var json = JsonConvert.SerializeObject(new CallNamedCommandMessage(elementSelector, commandName, args, messageId), serializerSettings.Value);
			_webviewManager.SendMessage(json);
			return WaitForMessage<dynamic>(messageId);
		}

		internal Task<dynamic> GetViewModelSnapshot()
		{
			var messageId = Guid.NewGuid();
			var json = JsonConvert.SerializeObject(new GetViewModelSnapshotMessage(messageId), serializerSettings.Value);
			_webviewManager.SendMessage(json);
			return WaitForMessage<dynamic>(messageId);
		}

		internal Task PatchViewModel(object patch)
		{
			var messageId = Guid.NewGuid();
			var json = JsonConvert.SerializeObject(new PatchViewModelMessage(patch, messageId), serializerSettings.Value);
			_webviewManager.SendMessage(json);
			return WaitForMessage<object>(messageId);
		}



		private Dictionary<Guid, TaskCompletionSource<string>> incomingMessageQueue = new();

		private async Task<T> WaitForMessage<T>(Guid messageId)
        {
			var source = new TaskCompletionSource<string>();
			incomingMessageQueue[messageId] = source;
			var result = await source.Task;
			return JsonConvert.DeserializeObject<T>(result);
        }

		protected void OnMessageReceived(string message)
        {
			var data = JObject.Parse(message);
			var type = data["type"].Value<string>();

			if (type == "handlerCommand")
            {
				// get response to the command from the handler
				var messageId = data["messageId"].Value<string>();
				incomingMessageQueue.Remove(Guid.Parse(messageId), out var source);

				if (!data.TryGetValue("errorMessage", out var errorMessage))
                {
					source.SetResult(data["result"].Value<string>());
				}
				else
                {
					source.SetException(new Exception("Command failed! " + errorMessage.Value<string>()));
                }
			}
			else if (type == "navigationCompleted")
            {
				var routeName = data["routeName"].Value<string>();
				if (routeName != RouteName)
				{
					this.routeName = routeName;
					VirtualView.RouteName = routeName;
				}
            }
			else if (type == "pageNotification")
            {
				var args = new PageNotificationEventArgs(data["methodName"].Value<string>(), ((JArray)data["args"]).Values<object>().ToArray());
				PageNotificationReceived?.Invoke(args);
            }
        }
	}

	public record CallNamedCommandMessage(string elementSelector, string commandName, object[] args, Guid messageId)
    {
		public string action => nameof(DotvvmWebViewHandler.CallNamedCommand);
    }
	public record GetViewModelSnapshotMessage(Guid messageId)
	{
		public string action => nameof(DotvvmWebViewHandler.GetViewModelSnapshot);
	}
	public record PatchViewModelMessage(object patch, Guid messageId)
	{
		public string action => nameof(DotvvmWebViewHandler.PatchViewModel);
	}
}