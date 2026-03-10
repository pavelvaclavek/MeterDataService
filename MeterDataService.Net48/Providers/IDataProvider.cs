using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Providers
{
    public interface IDataProvider
    {
        string Name { get; }
        Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken);
    }
}
