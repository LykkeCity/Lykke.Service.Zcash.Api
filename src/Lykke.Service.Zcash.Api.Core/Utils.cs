using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core
{
    public static class Utils
    {
        public static bool ValidateAddress(string address, out BitcoinAddress bitcoinAddress)
        {
            try
            {
                bitcoinAddress = BitcoinAddress.Create(address);
                return bitcoinAddress != null;
            }
            catch
            {
                bitcoinAddress = null;
                return false;
            }
        }
    }
}
