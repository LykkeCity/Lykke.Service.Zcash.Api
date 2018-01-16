using Lykke.Service.BlockchainApi.Contract.Assets;

namespace Lykke.Service.Zcash.Api.Core.Domain
{
    public static class AssetExtensions
    {
        public static AssetResponse ToAssetResponse(this Asset self)
        {
            return new AssetResponse
            {
                Accuracy = self.DecimalPlaces,
                Address = string.Empty,
                AssetId = self.Id,
                Name = self.Id
            };
        }

        public static AssetContract ToAssetContract(this Asset self)
        {
            return ToAssetResponse(self);
        }
    }
}
