using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.Wallets
{
    public class CreateTransparentWalletResponse
    {
        public CreateTransparentWalletResponse(string address) => PublicAddress = address;

        public string PublicAddress { get; }
    }
}
