using System.Linq;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NBitcoin;

namespace Lykke.Service.BlockchainApi.Contract.Requests
{
    public static class BlockchainServiceExtensions
    {
        public static bool IsValidRequest(this IBlockchainService self, CashoutFromWalletRequest request, ModelStateDictionary modelState,
            out IDestination to,
            out Asset asset,
            out Money money,
            out BitcoinAddress[] signers)
        {
            if (self.IsValidAddress(request.To, out var toAddress))
            {
                to = toAddress;
            }
            else
            {
                modelState.AddModelError(nameof(CashoutFromWalletRequest.To), "Invalid destination adddress");
                to = null;
            }

            if (!Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                modelState.AddModelError(nameof(CashoutFromWalletRequest.AssetId), "Invalid asset");
                asset = null;
            }

            if (decimal.TryParse(request.Amount, out var value) && asset != null)
            {
                money = new Money(value, asset.Unit);
            }
            else
            {
                modelState.AddModelError(nameof(CashoutFromWalletRequest.Amount), "Invalid amount");
                money = null;
            }

            if (request.Signers != null && request.Signers.Any())
            {
                signers = request.Signers
                    .Select(s =>
                    {
                        if (self.IsValidAddress(s, out var address))
                        {
                            return address;
                        }
                        else
                        {
                            modelState.AddModelError(nameof(CashoutFromWalletRequest.Signers), $"Invalid signer address: {s}");
                            return null;
                        }
                    })
                    .ToArray();
            }
            else
            {
                signers = null;
            }

            return modelState.IsValid;
        }
    }
}
