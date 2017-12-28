using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.Address
{
    public class ValidateOkResponse
    {
        public ValidateOkResponse(bool isValid) => IsValid = isValid;

        public bool IsValid { get; }
    }
}
