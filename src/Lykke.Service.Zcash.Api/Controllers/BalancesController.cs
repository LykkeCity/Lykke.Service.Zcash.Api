using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract.Balances;
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
        public async Task<WalletBalanceContract[]> Get([FromQuery]int skip = 0, [FromQuery]int take = 100)
        {
            return (await _blockchainService.GetBalancesAsync(skip, take))
                .Select(b = b.ToWalletBalanceContract())
                .ToArray();
        }

        [HttpPost("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromRoute]string address)
        {
            if (await _blockchainService.CreateObservableAddressAsync(address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpDelete("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([FromRoute]string address)
        {
            if (await _blockchainService.DeleteObservableAddressAsync(address))
                return Ok();
            else
                return NoContent();
        }
    }
}
