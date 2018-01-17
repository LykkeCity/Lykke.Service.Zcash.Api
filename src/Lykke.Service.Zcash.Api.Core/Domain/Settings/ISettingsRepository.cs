using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Zcash.Api.Core.Domain.Settings
{
    public interface ISettingsRepository
    {
        Task<ISettings> GetAsync();

        Task UpsertAsync(ISettings settings);
    }
}
