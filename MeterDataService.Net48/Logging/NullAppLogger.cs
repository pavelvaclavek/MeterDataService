using System;
using System.Threading.Tasks;

namespace MeterDataService.Net48.Logging
{
    /// <summary>
    /// Prázdná implementace loggeru - nic neloguje.
    /// Použije se, pokud je logování do databáze vypnuté v konfiguraci.
    /// </summary>
    public class NullAppLogger : IAppLogger
    {
        public Task LogInformationAsync(string message, string type = null, string ip = null)
            => Task.CompletedTask;

        public Task LogWarningAsync(string message, string type = null, string ip = null)
            => Task.CompletedTask;

        public Task LogErrorAsync(string message, Exception exception = null, string type = null, string ip = null)
            => Task.CompletedTask;
    }
}
