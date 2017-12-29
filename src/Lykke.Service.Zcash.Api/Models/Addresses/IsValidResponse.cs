using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.Addresses
{
    public class IsValidResponse
    {
        public IsValidResponse(bool isValid) => IsValid = isValid;

        public bool IsValid { get; }
    }
}
