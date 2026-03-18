using MeterDataService.Logging;
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
        private readonly IAppLogger _appLogger;
        private readonly ServiceConfiguration _config;

        public DataProviderManager(
            ILogger<DataProviderManager> logger,
            IEnumerable<IDataProvider> providers,
            IAppLogger appLogger,
            IOptions<ServiceConfiguration> config)
        {
            _logger = logger;
            _providers = providers;
            _appLogger = appLogger;
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
                    await _appLogger.LogWarningAsync(
                        $"Provider {provider.Name} failed to process message SN: {message.Sn}",
                        $"Provider.{provider.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} threw exception for SN: {Sn}",
                    provider.Name, message.Sn);
                await _appLogger.LogErrorAsync(
                    $"Provider {provider.Name} threw exception for SN: {message.Sn}",
                    ex, $"Provider.{provider.Name}");
            }
        }
    }
}
