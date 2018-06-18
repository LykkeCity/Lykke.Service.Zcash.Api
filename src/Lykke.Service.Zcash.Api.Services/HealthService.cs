using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Zcash.Api.Core.Domain.Health;
using Lykke.Service.Zcash.Api.Core.Services;

namespace Lykke.Service.Zcash.Api.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    public class HealthService : IHealthService
    {
        public string GetHealthViolationMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health violation message, or NULL if service hasn't critical errors

            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            // TODO: Check gathered health statistics, and add appropriate health issues message to issues

            var blockchainReaderError = BlockchainReader.Error;
            if (blockchainReaderError != null)
            {
                issues.Add(blockchainReaderError.GetType().Name, blockchainReaderError.Message);
            }

            return issues;
        }
    }
}
