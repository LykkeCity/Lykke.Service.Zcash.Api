using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Models.Assets;
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
        [ProducesResponseType(typeof(AssetListResponse), StatusCodes.Status200OK)]
        public IActionResult GetAssetList()
        {
            return Ok(new AssetListResponse { Assets = Constants.Assets.Values.Select(v => new AssetModel(v)).ToArray() });
        }

        /// <summary>
        /// Returns Zcash unit data
        /// </summary>
        /// <param name="id">Unit identifier</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AssetModel), StatusCodes.Status200OK)]
        public IActionResult GetAsset(string id)
        {
            if (Constants.Assets.ContainsKey(id))
            {
                return Ok(new AssetModel(Constants.Assets[id]));
            }
            else
            {
                return NotFound();
            }
        }
    }
}
