using DotVVM.Framework.Hosting;
using DotVVM.Framework.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Hosting.Maui.Services
{
    public class WebViewCsrfProtector : ICsrfProtector
    {
        public string GenerateToken(IDotvvmRequestContext context)
        {
            return "webviewToken";
        }

        public void VerifyToken(IDotvvmRequestContext context, string token)
        {
            if (token != "webviewToken")
            {
                throw new SecurityException("Invalid CSRF token!");
            }
        }
    }
}
