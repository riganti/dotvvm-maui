using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace DotVVM.Hosting.Maui
{
    public class DotvvmWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ApplicationName { get; set; } = "HostedApp";
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
    }
}