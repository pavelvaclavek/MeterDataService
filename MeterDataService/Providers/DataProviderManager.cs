using MeterDataService.Models;
using Microsoft.Extensions.Options;

namespace MeterDataService.Providers
{
    public interface IDataProviderManager
    {
        Task ProcessMessageAsync(MeterMessage message, CancellationToken cancellationToken);
    }

    public class DataProviderManager : IDataProviderManager
    {
        private readonly ILogger<DataProviderManager> _logger;
        private readonly IEnumerable<IDataProvider> _providers;
        private readonly ServiceConfiguration _config;

        public DataProviderManager(
            ILogger<DataProviderManager> logger,
            IEnumerable<IDataProvider> providers,
            IOptions<ServiceConfiguration> config)
        {
            _logger = logger;
            _providers = providers;
            _config = config.Value;
        }

        public async Task ProcessMessageAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            var enabledProviders = _providers
                .Where(p => _config.EnabledProviders.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("Processing message SN: {Sn} with {Count} providers",
                message.Sn, enabledProviders.Count);

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
                    _logger.LogWarning("Provider {Provider} failed to process message SN: {Sn}",
                        provider.Name, message.Sn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} threw exception for SN: {Sn}",
                    provider.Name, message.Sn);
            }
        }
    }
}
