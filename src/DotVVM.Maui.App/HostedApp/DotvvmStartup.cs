using DotVVM.Framework.Configuration;
using DotVVM.Framework.ResourceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Maui.App.HostedApp
{
    public class DotvvmStartup : IDotvvmStartup, IDotvvmServiceConfigurator
    {
        public void Configure(DotvvmConfiguration config, string applicationPath)
        {
            config.RouteTable.Add("Default", "", "Views/Default.dothtml");
            config.RouteTable.Add("Second", "second", "Views/Second.dothtml");

            config.Resources.Register("emptyModule", new ScriptModuleResource(new UrlResourceLocation("/emptyModule.js")));
        }

        public void ConfigureServices(IDotvvmServiceCollection options)
        {
        }
    }
}
