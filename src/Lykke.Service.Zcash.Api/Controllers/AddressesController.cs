﻿using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Helpers;
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
        public AddressValidationResponse IsValid(string address)
        {
            return new AddressValidationResponse()
            {
                IsValid = ModelState.IsValidAddress(_blockchainService, address)
            };
        }
    }
}
