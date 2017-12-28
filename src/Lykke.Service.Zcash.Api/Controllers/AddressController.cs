using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models.Address;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/addresses")]
    public class AddressController : Controller
    {
        private readonly IWalletService _walletService;

        public AddressController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("{address}/is-valid")]
        public ValidateOkResponse Validate(string address)
        {
            return new ValidateOkResponse(_walletService.IsValid(address));
        }
    }
}
