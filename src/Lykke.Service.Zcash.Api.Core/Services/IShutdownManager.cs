using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}