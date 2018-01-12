using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract;

namespace Lykke.Service.Zcash.Api.Core
{
    public static class PagedResultExtensions
    {
        public static async Task<PaginationResponse<M>> ToResponseAsync<M, T>(this Task<PagedResult<T>> self, Func<T, M> selector)
        {
            var res = await self;

            return PaginationResponse.From(
                res.Continuation,
                res.Items.Select(selector).ToArray());
        }
    }
}
