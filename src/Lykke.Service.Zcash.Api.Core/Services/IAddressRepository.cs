using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IAddressRepository
    {
        Task<PagedResult<IAddress>> GetObservableAddresses(AddressMonitorType monitorType, string continuation = null, int take = 100);

        Task CreateAsync(AddressMonitorType monitorType, string address);

        Task DeleteAsync(AddressMonitorType monitorType, string address);

        Task<IAddress> GetAsync(AddressMonitorType monitorType, string address);
    }
}
