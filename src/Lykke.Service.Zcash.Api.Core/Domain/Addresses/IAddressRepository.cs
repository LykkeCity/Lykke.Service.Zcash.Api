using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddressRepository
    {
        Task Create(ObservationCategory subject, string address);

        Task Delete(ObservationCategory subject, string address);

        Task<IAddress> GetAsync(ObservationCategory subject, string address);

        Task<(IEnumerable<IAddress> items, string continuation)> GetBySubjectAsync(ObservationCategory subject, string continuation = null, int take = 100);
    }
}
