using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Zcash.Api.Core.Domain.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

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
        public AddressValidationResponse IsValid(string address)
        {
            return new AddressValidationResponse()
            {
                IsValid = ModelState.IsValidAddress(_blockchainService, address)
            };
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginationResponse<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        public async Task<IActionResult> GetObservable(
            [FromQuery]AddressType type,
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

            var result = await _blockchainService.GetObservableAddressesAsync(type, continuation, take);

            return Ok(PaginationResponse.From(result.continuation, result.items.ToArray()));
        }

        [HttpPost("import")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BlockchainErrorResponse))]
        public async Task<IActionResult> Import([FromBody]string[] addresses)
        {
            if (!ModelState.IsValid || addresses.Count(a => !ModelState.IsValidAddress(a)) > 0)
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            foreach (var addr in addresses)
            {
                await _blockchainService.ImportAddress(addr);
            }

            return Ok();
        }
    }
}
