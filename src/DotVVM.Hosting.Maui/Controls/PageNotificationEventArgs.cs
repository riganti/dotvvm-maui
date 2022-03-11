namespace DotVVM.Hosting.Maui.Controls
{
    /// <summary>
    /// Represents the method name and arguments passed from the DotVVM page to the Maui host app.
    /// </summary>
    public class PageNotificationEventArgs
    {

        public string MethodName { get; }
        
        public object[] Arguments { get; }

        public PageNotificationEventArgs(string methodName, object[] args)
        {
            MethodName = methodName;
            Arguments = args;
        }

    }
}