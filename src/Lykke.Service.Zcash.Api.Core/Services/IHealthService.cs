using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Health;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    public interface IHealthService
    {
        Task<string> GetHealthViolationMessage();
        Task<IEnumerable<HealthIssue>> GetHealthIssues();
    }
}
