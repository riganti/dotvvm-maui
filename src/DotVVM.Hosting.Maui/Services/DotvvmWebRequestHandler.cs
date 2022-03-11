using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Hosting.Maui.Services
{
    public class DotvvmWebRequestHandler
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly RequestDelegate requestDelegate;

        public DotvvmWebRequestHandler(IServiceScopeFactory serviceScopeFactory, RequestDelegate requestDelegate)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.requestDelegate = requestDelegate;
        }

        public async Task<DotvvmResponse> ProcessRequest(Uri requestUri, string method, IEnumerable<KeyValuePair<string, string>> headers, Stream contentStream)
        {
			using var scope = serviceScopeFactory.CreateScope();
			var httpContext = new DefaultHttpContext()
			{
				Request =
				{
					Scheme = requestUri.Scheme,
					Host = new HostString(requestUri.Host, requestUri.Port),
					Path = requestUri.PathAndQuery,
					Method = method
				},
				Response =
				{
					Body = new MemoryStream()
				},
				ServiceScopeFactory = serviceScopeFactory,
				RequestServices = scope.ServiceProvider
			};
			foreach (var header in headers)
			{
				httpContext.Request.Headers.Add(header.Key, header.Value);
			}			
			if (contentStream != null)
			{
				httpContext.Request.Body = contentStream;
			}
			await requestDelegate(httpContext);

			return new DotvvmResponse(
				httpContext.Response.StatusCode,
				httpContext.Response.Headers.SelectMany(h => h.Value.Select(v => new KeyValuePair<string, string>(h.Key, v))),
				(MemoryStream)httpContext.Response.Body);
		}

    }

    public record DotvvmResponse(int StatusCode, IEnumerable<KeyValuePair<string, string>> Headers, MemoryStream Content);
}
