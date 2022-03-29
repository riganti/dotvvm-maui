using System;
using System.Linq;
using System.Text;
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
				return PlatformView.Source?.ToString();
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

		private int messageIdCounter;

		internal Task<dynamic> CallNamedCommand(string elementSelector, string commandName, object[] args)
		{
			var messageId = Interlocked.Increment(ref messageIdCounter);
			var json = JsonConvert.SerializeObject(new CallNamedCommandMessage(elementSelector, commandName, args, messageId), serializerSettings.Value);
			_webviewManager.SendMessage(json);
			return WaitForMessage<dynamic>(messageId);
		}

		internal Task<dynamic> GetViewModelSnapshot()
		{
			var messageId = Interlocked.Increment(ref messageIdCounter);
			var json = JsonConvert.SerializeObject(new GetViewModelSnapshotMessage(messageId), serializerSettings.Value);
			_webviewManager.SendMessage(json);
			return WaitForMessage<dynamic>(messageId);
		}

		internal Task PatchViewModel(object patch)
		{
			var messageId = Interlocked.Increment(ref messageIdCounter);
			var json = JsonConvert.SerializeObject(new PatchViewModelMessage(patch, messageId), serializerSettings.Value);
			_webviewManager.SendMessage(json);
			return WaitForMessage<object>(messageId);
		}



		private Dictionary<int, TaskCompletionSource<string>> incomingMessageQueue = new();

		private async Task<T> WaitForMessage<T>(int messageId)
        {
			var source = new TaskCompletionSource<string>();
			incomingMessageQueue[messageId] = source;
			var result = await source.Task;
			return JsonConvert.DeserializeObject<T>(result);
        }

		protected async void OnMessageReceived(string message)
        {
			var data = JObject.Parse(message);
			var type = data["type"].Value<string>();

			if (type == "HandlerCommand")
            {
				// get response to the command from the handler
				var messageId = data["id"].Value<int>();
				incomingMessageQueue.Remove(messageId, out var source);

				if (!data.TryGetValue("errorMessage", out var errorMessage))
                {
					source.SetResult(data["result"].Value<string>());
				}
				else
                {
					source.SetException(new Exception("Command failed! " + errorMessage.Value<string>()));
                }
			}
			else if (type == "NavigationCompleted")
            {
				var routeName = data["routeName"].Value<string>();
				if (routeName != RouteName)
				{
					this.routeName = routeName;
					VirtualView.RouteName = routeName;
				}
            }
			else if (type == "PageNotification")
            {
				var args = new PageNotificationEventArgs(data["methodName"].Value<string>(), ((JArray)data["args"]).Values<object>().ToArray());
				PageNotificationReceived?.Invoke(args);
            }
			else if (type == "HttpRequest")
            {
				var messageId = data["id"].Value<int>();
				var handler = Services.GetRequiredService<DotvvmWebRequestHandler>();
				var response = await handler.ProcessRequest
				(
					new Uri(new Uri("https://0.0.0.0/"), data["url"].Value<string>()),
					data["method"].Value<string>(),
					((JObject)data["headers"]).Properties().Select(p => new KeyValuePair<string, string>(p.Name, p.Value.Value<string>())),
					new MemoryStream(Encoding.UTF8.GetBytes(data["body"].Value<string>()))
				);
				var json = JsonConvert.SerializeObject(new HttpRequestMessage(response.StatusCode, response.Headers, Encoding.UTF8.GetString(response.Content.ToArray()), messageId), serializerSettings.Value);
				_webviewManager.SendMessage(json);
			}
        }
	}

	public record CallNamedCommandMessage(string elementSelector, string commandName, object[] args, int messageId)
    {
		public string action => nameof(DotvvmWebViewHandler.CallNamedCommand);
    }
	public record GetViewModelSnapshotMessage(int messageId)
	{
		public string action => nameof(DotvvmWebViewHandler.GetViewModelSnapshot);
	}
	public record PatchViewModelMessage(object patch, int messageId)
	{
		public string action => nameof(DotvvmWebViewHandler.PatchViewModel);
	}
	public record HttpRequestMessage(int status, IEnumerable<KeyValuePair<string, string>> headers, string body, int messageId)
    {
		public string action => "HttpRequest";
	}
}