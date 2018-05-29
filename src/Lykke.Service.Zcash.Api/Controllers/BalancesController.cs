using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/balances")]
    public class BalancesController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public BalancesController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<WalletBalanceContract>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(
            [FromQuery]string continuation,
            [FromQuery]int take)
        {
            // until ASP.NET Core 2.1 data annotations on arguments do not work
            if (take <= 0)
            {
                ModelState.AddModelError(nameof(take), "Must be greater than zero");
            }

            // kinda specific knowledge but there is no 
            // another way to ensure continuation token
            if (!string.IsNullOrEmpty(continuation))
            {
                try
                {
                    JsonConvert.DeserializeObject<TableContinuationToken>(Utils.HexToString(continuation));
                }
                catch
                {
                    ModelState.AddModelError(nameof(continuation), "Invalid continuation token");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            var result = await _blockchainService.GetBalancesAsync(continuation, take);

            return Ok(PaginationResponse.From(
                result.continuation,
                result.items.Select(b => b.ToWalletContract()).ToArray()));
        }

        [HttpPost("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(_blockchainService, address))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            if (await _blockchainService.TryCreateBalanceAddressAsync(address))
                return Ok();
            else
                return StatusCode(StatusCodes.Status409Conflict);
        }

        [HttpDelete("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> Delete([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(_blockchainService, address))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            if (await _blockchainService.TryDeleteBalanceAddressAsync(address))
                return Ok();
            else
                return NoContent();
        }
    }
}
