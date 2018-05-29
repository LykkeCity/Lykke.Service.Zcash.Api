using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.History;
using Lykke.Service.Zcash.Api.Core.Domain.Operations;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public interface IBlockchainService
    {
        Task<(string context, Dictionary<string, decimal> outputs)> BuildAsync(
            Guid operationId, OperationType type, Asset asset, bool subtractFees, params (string from, string to, decimal amount)[] items);

        Task BroadcastAsync(Guid operationId, string transaction);

        Task<IOperation> GetOperationAsync(Guid operationId, bool loadItems = true);

        Task<bool> TryDeleteOperationAsync(Guid operationId);

        Task HandleHistoryAsync();

        Task<IEnumerable<IHistoryItem>> GetHistoryAsync(HistoryAddressCategory category, string address, string afterHash = null, int take = 100);

        Task<(string continuation, IEnumerable<AddressBalance> items)> GetBalancesAsync(string continuation = null, int take = 100);

        Task<(string continuation, IEnumerable<string> items)> GetObservableAddressesAsync(AddressType type, string continuation = null, int take = 100);

        Task<bool> TryDeleteBalanceAddressAsync(string address);

        Task<bool> TryCreateBalanceAddressAsync(string address);

        Task<bool> TryDeleteHistoryAddressAsync(string address, HistoryAddressCategory category);

        Task<bool> TryCreateHistoryAddressAsync(string address, HistoryAddressCategory category);

        Task ImportAddress(string address);

        void EnsureSigned(Transaction transaction, ICoin[] coins);

        Task<bool> ValidateAddressAsync(string address);

        Task<bool> ValidateSignedTransactionAsync(string transaction);
    }
}
