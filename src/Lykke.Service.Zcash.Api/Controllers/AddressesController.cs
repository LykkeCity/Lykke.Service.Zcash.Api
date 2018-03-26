using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.Zcash;

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
