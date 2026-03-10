using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Providers
{
    public interface IDataProviderManager
    {
        Task ProcessMessageAsync(MeterMessage message, CancellationToken cancellationToken);
    }

    public class DataProviderManager : IDataProviderManager
    {
        private readonly EventLog _eventLog;
        private readonly IEnumerable<IDataProvider> _providers;
        private readonly ServiceConfiguration _config;

        public DataProviderManager(
            EventLog eventLog,
            IEnumerable<IDataProvider> providers,
            ServiceConfiguration config)
        {
            _eventLog = eventLog;
            _providers = providers;
            _config = config;
        }

        public async Task ProcessMessageAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            var enabledProviders = _providers
                .Where(p => _config.EnabledProviders
                    .Any(ep => string.Equals(ep, p.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            _eventLog.WriteEntry(
                string.Format("Processing message SN: {0} with {1} providers", message.Sn, enabledProviders.Count),
                EventLogEntryType.Information);

            var tasks = enabledProviders.Select(provider => ProcessWithProviderAsync(provider, message, cancellationToken));
            await Task.WhenAll(tasks);
        }

        private async Task ProcessWithProviderAsync(IDataProvider provider, MeterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var success = await provider.ProcessAsync(message, cancellationToken);
                if (!success)
                {
                    _eventLog.WriteEntry(
                        string.Format("Provider {0} failed to process message SN: {1}", provider.Name, message.Sn),
                        EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    string.Format("Provider {0} threw exception for SN: {1}: {2}", provider.Name, message.Sn, ex),
                    EventLogEntryType.Error);
            }
        }
    }
}
