using DotVVM.Framework.Compilation.Javascript;
using DotVVM.Framework.Compilation.Javascript.Ast;

namespace DotVVM.Hosting.Maui.Binding
{
    public class WebViewBindingApi
    {

        /// <summary> Sends notification to the WebView host application.</summary>
        public void SendNotification(string methodName, params object[] args) =>
            throw new Exception($"Cannot invoke JS command server-side: {methodName}({string.Join(", ", args)}).");
        
        internal static void RegisterJavascriptTranslations(JavascriptTranslatableMethodCollection collection)
        {
            collection.AddMethodTranslator(typeof(WebViewBindingApi), nameof(SendNotification), 
                new GenericMethodCompiler(a => new JsInvocationExpression(a[0].Member("sendPageNotification"), a[1], a[2])));
        }

    }
}
