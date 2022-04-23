using DotVVM.Framework.Configuration;
using DotVVM.Framework.ResourceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Hosting.Maui.Resources
{
    public class WebViewResourceProcessor : IResourceProcessor
    {
        private readonly DotvvmConfiguration config;

        public WebViewResourceProcessor(DotvvmConfiguration config)
        {
            this.config = config;
        }

        public IEnumerable<NamedResource> Process(IEnumerable<NamedResource> source)
        {
            foreach (var r in source)
            {
                if (r.Name == ResourceConstants.DotvvmResourceName + ".internal")
                    yield return this.config.Resources.FindNamedResource(ResourceConstants.DotvvmResourceName + ".internal-webview");
                else
                    yield return r;
            }
        }
    }
}
