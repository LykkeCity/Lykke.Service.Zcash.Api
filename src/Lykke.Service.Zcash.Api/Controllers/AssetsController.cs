using System.Linq;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/assets")]
    public class AssetsController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<AssetContract>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public IActionResult GetAssetList(
            [FromQuery]string continuation,
            [FromQuery]int take)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToBlockchainErrorResponse());
            }

            return Ok(PaginationResponse.From(null, 
                Constants.Assets.Values.Select(v => v.ToAssetContract()).ToArray()));
        }

        [HttpGet("{id}")]
        public AssetResponse GetAsset(string id)
        {
            if (Constants.Assets.TryGetValue(id, out var asset))
            {
                return asset.ToAssetResponse();
            }
            else
            {
                return null;
            }
        }
    }
}
