using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/addresses")]
    public class AddressesController : Controller
    {
        private readonly IBlockchainService _blockchainService;

        public AddressesController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpGet("{address}/validity")]
        public AddressValidationResponse IsValid([FromRoute]string address)
        {
            return new AddressValidationResponse()
            {
                IsValid = ModelState.IsValidAddress(address)
            };
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetObservable(
            [FromQuery]AddressType type,
            [FromQuery]string continuation,
            [FromQuery]int take)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            var result = await _blockchainService.GetObservableAddressesAsync(type, continuation, take);

            return Ok(PaginationResponse.From(result.continuation, result.items.ToArray()));
        }

        [HttpPost("import")]
        public async Task Import([FromBody]string[] addresses)
        {
            foreach (var addr in addresses)
            {
                await _blockchainService.ImportAddress(addr);
            }
        }

        [HttpGet("{address}/explorer-url")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string[]))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        public IActionResult ExplorerUrl([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            return Ok(_blockchainService.GetExplorerUrl(address));
        }
    }
}
