using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IBlockchainService
    {
        Task<string> BuildAsync(Guid operationId, OperationType type, Asset asset, bool subtractFees, params (BitcoinAddress from, BitcoinAddress to, Money amount)[] items);

        Task BroadcastAsync(Guid operationId, Transaction transaction);

        Task<IOperation> GetOperationAsync(Guid operationId, bool loadItems = true);

        Task<bool> TryDeleteOperationAsync(Guid operationId);

        Task HandleHistoryAsync();

        Task<IEnumerable<IHistoryItem>> GetHistoryAsync(ObservationCategory category, string address, string afterHash = null, int take = 100);

        Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100);

        Task<bool> TryCreateObservableAddressAsync(ObservationCategory category, string address);

        Task<bool> TryDeleteObservableAddressAsync(ObservationCategory category, string address);

        bool ValidateAddress(string address);

        void EnsureSigned(Transaction transaction, ICoin[] coins);
    }
}
