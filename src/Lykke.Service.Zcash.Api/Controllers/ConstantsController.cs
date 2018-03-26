using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/constants")]
    public class ConstantsController : Controller
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult Get()
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }
    }
}
