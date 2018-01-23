using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddressRepository
    {
        Task CreateAsync(ObservationCategory category, string address);

        Task DeleteAsync(ObservationCategory category, string address);

        Task<IAddress> GetAsync(ObservationCategory category, string address);

        Task<(IEnumerable<IAddress> items, string continuation)> GetByCategoryAsync(ObservationCategory category, string continuation = null, int take = 100);
    }
}
