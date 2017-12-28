using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Services;
using Lykke.Service.Zcash.Api.Models;
using Lykke.Service.Zcash.Api.Models.Wallet;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/wallets")]
    public class WalletController : Controller
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Creates new t-address for Zcash
        /// </summary>
        /// <returns>Public address</returns>
        [HttpPost]
        public async Task<CreateOkResponse> Create()
        {
            return new CreateOkResponse(await _walletService.CreateTransparentWalletAsync());
        }
    }
}
