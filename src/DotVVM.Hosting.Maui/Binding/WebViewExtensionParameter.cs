using DotVVM.Framework.Compilation.ControlTree;
using DotVVM.Framework.Compilation.ControlTree.Resolved;
using DotVVM.Framework.Compilation.Javascript.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Hosting.Maui.Binding
{
    public class WebViewExtensionParameter : BindingExtensionParameter
    {
        public WebViewExtensionParameter() : base("_webview", new ResolvedTypeDescriptor(typeof(WebViewBindingApi)), true)
        {
        }

        public override Expression GetServerEquivalent(Expression controlParameter)
        {
            return Expression.New(typeof(WebViewBindingApi));
        }

        public override JsExpression GetJsTranslation(JsExpression dataContext)
        {
            return new JsIdentifierExpression("dotvvm").Member("webview");
        }

    }
}
