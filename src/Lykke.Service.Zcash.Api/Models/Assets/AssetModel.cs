using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace Lykke.Service.Zcash.Api.Models.Assets
{
    public class AssetModel
    {
        public AssetModel(Asset asset)
        {
            AssetId = asset.Id;
            Address = string.Empty;
            Name = asset.Id;
            Accuracy = asset.DecimalPlaces;
        }

        public string AssetId { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public int Accuracy { get; set; }
    }
}
