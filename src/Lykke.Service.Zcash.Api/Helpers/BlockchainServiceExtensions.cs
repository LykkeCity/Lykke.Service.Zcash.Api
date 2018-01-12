using System;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public static class BlockchainServiceExtensions
    {
        public static bool IsValidRequest(this IBlockchainService self, ModelStateDictionary modelState, 
            BaseTransactionBuildingRequest request,
            out BitcoinAddress from,
            out BitcoinAddress to,
            out Asset asset,
            out Money amount)
        {
            (from, to, asset, amount) = (null, null, null, null);

            if (!modelState.IsValid)
            {
                return false;
            }

            if (self.ValidateAddress(request.FromAddress, out var fromAddress))
            {
                from = fromAddress;
            }
            else
            {
                modelState.AddModelError(
                    nameof(BuildTransactionRequest.FromAddress),
                    "Invalid sender adddress");
            }

            if (self.ValidateAddress(request.ToAddress, out var toAddress))
            {
                to = toAddress;
            }
            else
            {
                modelState.AddModelError(
                    nameof(BuildTransactionRequest.ToAddress),
                    "Invalid destination adddress");
            }

            if (!Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                modelState.AddModelError(
                    nameof(BuildTransactionRequest.AssetId),
                    "Invalid asset");
            }

            try
            {
                var coins = Conversions.CoinsFromContract(request.Amount, asset.DecimalPlaces);
                amount = Money.FromUnit(coins, asset.Unit);
            }
            catch (ConversionException ex)
            {
                modelState.AddModelError(
                    nameof(BuildTransactionRequest.Amount),
                    ex.Message);
            }

            return modelState.IsValid;
        }

        public static bool IsValidRequest(this IBlockchainService self, ModelStateDictionary modelState, 
            BroadcastTransactionRequest request,
            out Transaction transaction)
        {
            transaction = null;

            if (!modelState.IsValid)
            {
                return false;
            }

            try
            {
                transaction = Transaction.Parse(request.SignedTransaction);
            }
            catch (Exception ex)
            {
                modelState.AddModelError(
                    nameof(BroadcastTransactionRequest.SignedTransaction),
                    ex.Message);
            }

            return modelState.IsValid;
        }
    }
}
