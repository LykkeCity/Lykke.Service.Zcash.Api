using System.Linq;
using Lykke.Service.BlockchainApi.Contract.Responses;
using Lykke.Service.Zcash.Api.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        [ProducesResponseType(typeof(AssetsListResponse), StatusCodes.Status200OK)]
        public IActionResult GetAssetList()
        {
            return Ok(new AssetsListResponse
            {
                Assets = Constants.Assets.Values.Select(v => v.ToResponse()).ToArray()
            });
        }

        /// <summary>
        /// Returns Zcash unit data
        /// </summary>
        /// <param name="id">Unit identifier</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        public IActionResult GetAsset(string id)
        {
            if (Constants.Assets.ContainsKey(id))
            {
                return Ok(Constants.Assets[id].ToResponse());
            }
            else
            {
                return NotFound();
            }
        }
    }
}
