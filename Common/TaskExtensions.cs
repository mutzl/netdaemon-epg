using System;
using System.Threading.Tasks;

namespace Mutzl.Homeassistant
{
    public static class TaskExtensions
    {
        public async static void Await(this Task task, Action? completedCallback, Action<Exception>? erroCallback)
        {
            try
            {
                await task;
                completedCallback?.Invoke();
            }
            catch (Exception ex)
            {
                erroCallback?.Invoke(ex);
            }
        }
    }
}
