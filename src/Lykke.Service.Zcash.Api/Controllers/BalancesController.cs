using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/balances")]
    public class BalancesController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        [HttpGet]
        public async Task<PaginationResponse<WalletBalanceContract>> Get(
            [FromQuery]string continuation = null,
            [FromQuery]int take = 100)
        {
            var result = await _blockchainService.GetBalancesAsync(continuation, take);

            return PaginationResponse.From(
                result.continuation,
                result.items.Select(b => b.ToWalletContract()).ToArray());
        }

        [HttpPost("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromRoute]string address)
        {
            if (await _blockchainService.TryCreateObservableAddressAsync(ObservationSubject.Balance, address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpDelete("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([FromRoute]string address)
        {
            if (await _blockchainService.TryDeleteObservableAddressAsync(ObservationSubject.Balance, address))
                return Ok();
            else
                return NoContent();
        }
    }
}
