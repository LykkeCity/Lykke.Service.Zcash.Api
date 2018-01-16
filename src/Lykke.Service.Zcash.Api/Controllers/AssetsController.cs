using System.Linq;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
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
        public PaginationResponse<AssetContract> GetAssetList(
            [FromQuery]string continuation = null, 
            [FromQuery]int take = 100)
        {
            return PaginationResponse.From(null, 
                Constants.Assets.Values.Select(v => v.ToAssetContract()).ToArray());
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
