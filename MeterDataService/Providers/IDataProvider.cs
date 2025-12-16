using MeterDataService.Models;

namespace MeterDataService.Providers
{
    public interface IDataProvider
    {
        string Name { get; }
        Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken);
    }
}