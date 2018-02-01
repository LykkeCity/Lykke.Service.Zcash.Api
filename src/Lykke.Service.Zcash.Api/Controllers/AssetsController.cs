using System.ComponentModel.DataAnnotations;
using System.Linq;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Zcash.Api.Controllers
{
    [Route("/api/assets")]
    public class AssetsController : Controller
    {
        /// <summary>
        /// Returns list of Zcash units
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<AssetContract>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public IActionResult GetAssetList(
            [FromQuery]string continuation,
            [FromQuery]int take)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidContinuation(continuation))
            {
                return BadRequest(ErrorResponseFactory.Create(ModelState));
            }

            return Ok(PaginationResponse.From(null, 
                Constants.Assets.Values.Select(v => v.ToAssetContract()).ToArray()));
        }

        /// <summary>
        /// Returns Zcash unit data
        /// </summary>
        /// <param name="id">Unit identifier</param>
        /// <returns></returns>
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
