using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Core.Repositories
{
    public interface IAddressRepository
    {
        Task<bool> CreateIfNotExistsAsync(AddressMonitorType monitorType, string address);

        Task<IAddress> GetAsync(AddressMonitorType monitorType, string address);

        Task<(string continuation, IAddress[] items)> GetAsync(AddressMonitorType monitorType, string continuation = null, int take = 100);

        Task<IAddress[]> GetByTypeAsync();

        Task<bool> DeleteIfExistAsync(AddressMonitorType monitorType, string address);
    }
}
