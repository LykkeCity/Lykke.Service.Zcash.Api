using System;
using System.Text;
using Common;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NBitcoin;
using NBitcoin.JsonConverters;

namespace Lykke.Service.Zcash.Api.Core.Services
{
    public static class BlockchainServiceExtensions
    {
        public static bool IsValidRequest(this IBlockchainService self, ModelStateDictionary modelState,
            BuildSingleTransactionRequest request,
            out (BitcoinAddress from, BitcoinAddress to, Money amount)[] items,
            out Asset asset)
        {
            items = new(BitcoinAddress from, BitcoinAddress to, Money amount)[1];
            asset = null;

            if (!modelState.IsValid)
            {
                return false;
            }

            if (self.ValidateAddress(request.FromAddress, out var fromAddress))
            {
                items[0].from = fromAddress;
            }
            else
            {
                modelState.AddModelError(
                    nameof(BuildSingleTransactionRequest.FromAddress),
                    "Invalid sender adddress");
            }

            if (self.ValidateAddress(request.ToAddress, out var toAddress))
            {
                items[0].to = toAddress;
            }
            else
            {
                modelState.AddModelError(
                    nameof(BuildSingleTransactionRequest.ToAddress),
                    "Invalid destination adddress");
            }

            if (!Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                modelState.AddModelError(
                    nameof(BuildSingleTransactionRequest.AssetId),
                    "Invalid asset");
            }

            try
            {
                var coins = Conversions.CoinsFromContract(request.Amount, asset.DecimalPlaces);
                items[0].amount = Money.FromUnit(coins, asset.Unit);
            }
            catch (ConversionException ex)
            {
                modelState.AddModelError(
                    nameof(BuildSingleTransactionRequest.Amount),
                    ex.Message);
            }

            return modelState.IsValid;
        }

        public static bool IsValidRequest(this IBlockchainService self, ModelStateDictionary modelState,
            BuildTransactionWithManyInputsRequest request,
            out (BitcoinAddress from, BitcoinAddress to, Money amount)[] items,
            out Asset asset)
        {
            items = new(BitcoinAddress from, BitcoinAddress to, Money amount)[request.Inputs.Count];
            asset = null;

            if (!modelState.IsValid)
            {
                return false;
            }

            if (!Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                modelState.AddModelError(
                    nameof(BuildSingleTransactionRequest.AssetId),
                    "Invalid asset");
            }

            if (!self.ValidateAddress(request.ToAddress, out var toAddress))
            {
                modelState.AddModelError(
                    nameof(BuildSingleTransactionRequest.ToAddress),
                    "Invalid destination adddress");
            }

            for (int i = 0; i < request.Inputs.Count; i++)
            {
                if (self.ValidateAddress(request.Inputs[i].FromAddress, out var fromAddress))
                {
                    items[i].from = fromAddress;
                }
                else
                {
                    modelState.AddModelError(
                        $"{nameof(BuildTransactionWithManyInputsRequest.Inputs)}[{i}].{nameof(TransactionInputContract.FromAddress)}",
                        "Invalid sender adddress");
                }

                items[i].to = toAddress;

                try
                {
                    var coins = Conversions.CoinsFromContract(request.Inputs[i].Amount, asset.DecimalPlaces);
                    items[0].amount = Money.FromUnit(coins, asset.Unit);
                }
                catch (ConversionException ex)
                {
                    modelState.AddModelError(
                        $"{nameof(BuildTransactionWithManyInputsRequest.Inputs)}[{i}].{nameof(TransactionInputContract.Amount)}",
                        ex.Message);
                }
            }

            return modelState.IsValid;
        }

        public static bool IsValidRequest(this IBlockchainService self, ModelStateDictionary modelState,
            BuildTransactionWithManyOutputsRequest request,
            out (BitcoinAddress from, BitcoinAddress to, Money amount)[] items,
            out Asset asset)
        {
            items = new(BitcoinAddress from, BitcoinAddress to, Money amount)[request.Outputs.Count];
            asset = null;

            if (!modelState.IsValid)
            {
                return false;
            }

            if (!Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                modelState.AddModelError(
                    nameof(BuildTransactionWithManyOutputsRequest.AssetId),
                    "Invalid asset");
            }

            if (!self.ValidateAddress(request.FromAddress, out var fromAddress))
            {
                modelState.AddModelError(
                    nameof(BuildTransactionWithManyOutputsRequest.FromAddress),
                    "Invalid sender adddress");
            }

            for (int i = 0; i < request.Outputs.Count; i++)
            {
                items[i].from = fromAddress;

                if (self.ValidateAddress(request.Outputs[i].ToAddress, out var toAddress))
                {
                    items[i].to = toAddress;
                }
                else
                {
                    modelState.AddModelError(
                        $"{nameof(BuildTransactionWithManyOutputsRequest.Outputs)}[{i}].{nameof(TransactionOutputContract.ToAddress)}",
                        "Invalid sender adddress");
                }

                try
                {
                    var coins = Conversions.CoinsFromContract(request.Outputs[i].Amount, asset.DecimalPlaces);
                    items[0].amount = Money.FromUnit(coins, asset.Unit);
                }
                catch (ConversionException ex)
                {
                    modelState.AddModelError(
                        $"{nameof(BuildTransactionWithManyOutputsRequest.Outputs)}[{i}].{nameof(TransactionOutputContract.Amount)}",
                        ex.Message);
                }
            }

            return modelState.IsValid;
        }

        public static bool IsValidRequest(this IBlockchainService self, ModelStateDictionary modelState, 
            BroadcastTransactionRequest request,
            out Transaction transaction,
            out ICoin[] coins)
        {
            (transaction, coins) = (null, null);

            if (!modelState.IsValid)
            {
                return false;
            }

            try
            {
                (transaction, coins) = Serializer.ToObject<(Transaction, ICoin[])>(request.SignedTransaction.Base64ToString());
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
