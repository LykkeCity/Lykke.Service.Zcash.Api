using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Addresses;
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
        private Network _network;

        public AddressesController(Network network)
        {
            _network = network;
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

            if (_network == ZcashNetworks.Mainnet)
            {
                return Ok(new[] 
                {
                    $"https://explorer.zcha.in/accounts/{address}",
                    $"https://zcash.blockexplorer.com/address/{address}",
                    $"https://zcashnetwork.info/address/{address}"
                });
            }
            else
            {
                return Ok(new[] 
                {
                    $"https://explorer.testnet.z.cash/address/{address}"
                });
            }
        }
    }
}
