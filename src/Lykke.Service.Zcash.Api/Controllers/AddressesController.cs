using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
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

        /// <summary>
        /// Checks if address is valid Zcash t-address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet("{address}/validity")]
        public AddressValidationResponse IsValid(string address)
        {
            return new AddressValidationResponse()
            {
                IsValid = _blockchainService.ValidateAddress(address)
            };
        }
    }
}
