using NetDaemon.Common.Reactive;
using System;

namespace Mutzl.Homeassistant
{
    public class AppLogger<T>
    {
        private string className = string.Empty;
        private string GetMessage(string message) => $"[{className}] {message}";
        
        private NetDaemonRxApp app;

        public AppLogger(NetDaemonRxApp app)
        {
            this.app = app;

            className = this.GetType().GetGenericArguments()[0].Name;
        }



        public void Log(string message) 
        {
            app.Log(GetMessage(message));
        }

        public void LogDebug(string message)
        {
            app.LogDebug(GetMessage(message));
        }
        public void LogError(string message)
        {
            app.LogError(GetMessage(message));
        }
        public void LogError(Exception ex, string message)
        {
            app.LogError(ex, GetMessage(message));
        }
        public void LogInformation(string message)
        {
            app.LogInformation(GetMessage(message));
        }
        public void LogTrace(string message)
        {
            app.LogTrace(GetMessage(message));
        }
        public void LogWarning(string message)
        {
            app.LogWarning(GetMessage(message));
        }

    }
}
