using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models.Addresses;
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
        [HttpGet("{address}/is-valid")]
        public IsValidResponse IsValid(string address)
        {
            return new IsValidResponse(_blockchainService.IsValidAddress(address, out var bitcoinAddress));
        }
    }
}
