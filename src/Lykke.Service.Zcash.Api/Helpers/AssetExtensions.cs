using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Zcash.Api.Core.Domain;

namespace Lykke.Service.BlockchainApi.Contract.Responses
{
    public static class AssetExtensions
    {
        public static AssetResponse ToResponse(this Asset self)
        {
            return new AssetResponse
            {
                Accuracy = self.DecimalPlaces,
                Address = string.Empty,
                AssetId = self.Id,
                Name = self.Id
            };
        }
    }
}
