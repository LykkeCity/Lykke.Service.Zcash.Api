using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Addresses
{
    public interface IAddressRepository
    {
        Task<bool> CreateIfNotExistsAsync(AddressMonitorType monitorType, string address);

        Task<bool> DeleteIfExistAsync(AddressMonitorType monitorType, string address);

        Task<IAddress> GetAsync(AddressMonitorType monitorType, string address);

        Task<(string continuation, IEnumerable<IAddress> items)> GetByTypeAsync(AddressMonitorType monitorType, string continuation = null, int take = 100);
    }
}
