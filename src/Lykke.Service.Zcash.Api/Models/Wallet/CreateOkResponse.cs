using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.Wallet
{
    public class CreateOkResponse
    {
        public CreateOkResponse(string address) => PublicAddress = address;

        public string PublicAddress { get; }
    }
}
