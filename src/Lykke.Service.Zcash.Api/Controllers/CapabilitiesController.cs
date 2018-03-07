using Lykke.Service.BlockchainApi.Contract.Common;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/capabilities")]
    public class CapabilitiesController : Controller
    {
        [HttpGet]
        public CapabilitiesResponse Get()
        {
            return new CapabilitiesResponse()
            {
                AreManyInputsSupported = true,
                AreManyOutputsSupported = true,
                IsTransactionsRebuildingSupported = false
            };
        }
    }
}
