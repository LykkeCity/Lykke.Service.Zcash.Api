using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Settings
{
    public interface ISettings
    {
        int ConfirmationLevel { get; }
        string LastBlockHash { get; }
        decimal FeePerKb { get; }
        decimal MaxFee { get; }
        decimal MinFee { get; }
        bool UseDefaultFee { get; }
        bool SkipNodeCheck { get; }
    }
}
