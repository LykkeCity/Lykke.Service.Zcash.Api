using System.Collections.Generic;
using System.Linq;
using Lykke.Service.BlockchainApi.Contract.Assets;
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
        public AssetContract[] GetAssetList()
        {
            var assets = Constants.Assets.Values;

            return assets
                .Select(v => v.ToAssetContract())
                .ToArray();
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
