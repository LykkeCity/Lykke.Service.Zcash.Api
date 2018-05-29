namespace Lykke.Service.Zcash.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id, int decimalPlaces) => (Id, DecimalPlaces) = (id, decimalPlaces);

        public string Id { get; }
        public int DecimalPlaces { get; }

        // static instances (constants)

        public static Asset Zec { get; } = new Asset("ZEC", 8);
    }
}
