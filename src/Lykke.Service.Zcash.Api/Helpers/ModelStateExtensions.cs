﻿using System;
using Common;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.WindowsAzure.Storage.Table;
using NBitcoin;
using NBitcoin.JsonConverters;
using Newtonsoft.Json;
using CoreUtils = Lykke.Service.Zcash.Api.Core.Utils;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public static class ModelStateExtensions
    {
        public static bool IsValidAddress(this ModelStateDictionary self, string address)
        {
            if (CoreUtils.ValidateAddress(address, out var _))
            {
                return true;
            }
            else
            {
                self.AddModelError(nameof(address), "Address must be a valid Zcash transparent (t-) address");
                return false;
            }
        }

        public static bool IsValidContinuation(this ModelStateDictionary self, string continuation)
        {
            if (string.IsNullOrEmpty(continuation))
            {
                return true;
            }

            try
            {
                JsonConvert.DeserializeObject<TableContinuationToken>(Common.Utils.HexToString(continuation));
                return true;
            }
            catch
            {
                self.AddModelError("continuation", "Invalid continuation token");
                return false;
            }
        }

        public static bool IsValidOperationId(this ModelStateDictionary self, Guid operationId)
        {
            if (operationId == Guid.Empty)
            {
                self.AddModelError(nameof(operationId), "Operation identifier must not be empty GUID");
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool IsValidRequest(this ModelStateDictionary self,
            BuildSingleTransactionRequest request,
            out (BitcoinAddress from, BitcoinAddress to, Money amount)[] items,
            out Asset asset)
        {
            items = new(BitcoinAddress from, BitcoinAddress to, Money amount)[1];
            asset = null;

            if (!self.IsValid)
            {
                return false;
            }

            if (CoreUtils.ValidateAddress(request.FromAddress, out var fromAddress))
            {
                items[0].from = fromAddress;
            }
            else
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.FromAddress),
                    "Invalid sender adddress");
            }

            if (CoreUtils.ValidateAddress(request.ToAddress, out var toAddress))
            {
                items[0].to = toAddress;
            }
            else
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.ToAddress),
                    "Invalid destination adddress");
            }

            if (string.IsNullOrWhiteSpace(request.AssetId) || !Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.AssetId),
                    "Invalid asset");
            }
            else
            {
                try
                {
                    var coins = Conversions.CoinsFromContract(request.Amount, asset.DecimalPlaces);
                    items[0].amount = Money.FromUnit(coins, asset.Unit);
                }
                catch (ConversionException ex)
                {
                    self.AddModelError(
                        nameof(BuildSingleTransactionRequest.Amount),
                        ex.Message);
                }
            }

            return self.IsValid;
        }

        public static bool IsValidRequest(this ModelStateDictionary self,
            BuildTransactionWithManyInputsRequest request,
            out (BitcoinAddress from, BitcoinAddress to, Money amount)[] items,
            out Asset asset)
        {
            items = new(BitcoinAddress from, BitcoinAddress to, Money amount)[request.Inputs.Count];
            asset = null;

            if (!self.IsValid)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.AssetId) || !Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.AssetId),
                    "Invalid asset");
            }

            if (!CoreUtils.ValidateAddress(request.ToAddress, out var toAddress))
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.ToAddress),
                    "Invalid destination adddress");
            }

            for (int i = 0; i < request.Inputs.Count; i++)
            {
                if (CoreUtils.ValidateAddress(request.Inputs[i].FromAddress, out var fromAddress))
                {
                    items[i].from = fromAddress;
                }
                else
                {
                    self.AddModelError(
                        $"{nameof(BuildTransactionWithManyInputsRequest.Inputs)}[{i}].{nameof(TransactionInputContract.FromAddress)}",
                        "Invalid sender adddress");
                }

                items[i].to = toAddress;

                if (asset != null)
                {
                    try
                    {
                        var coins = Conversions.CoinsFromContract(request.Inputs[i].Amount, asset.DecimalPlaces);
                        items[i].amount = Money.FromUnit(coins, asset.Unit);
                    }
                    catch (ConversionException ex)
                    {
                        self.AddModelError(
                            $"{nameof(BuildTransactionWithManyInputsRequest.Inputs)}[{i}].{nameof(TransactionInputContract.Amount)}",
                            ex.Message);
                    }
                }
            }

            return self.IsValid;
        }

        public static bool IsValidRequest(this ModelStateDictionary self,
            BuildTransactionWithManyOutputsRequest request,
            out (BitcoinAddress from, BitcoinAddress to, Money amount)[] items,
            out Asset asset)
        {
            items = new(BitcoinAddress from, BitcoinAddress to, Money amount)[request.Outputs.Count];
            asset = null;

            if (!self.IsValid)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.AssetId) || !Constants.Assets.TryGetValue(request.AssetId, out asset))
            {
                self.AddModelError(
                    nameof(BuildTransactionWithManyOutputsRequest.AssetId),
                    "Invalid asset");
            }

            if (!CoreUtils.ValidateAddress(request.FromAddress, out var fromAddress))
            {
                self.AddModelError(
                    nameof(BuildTransactionWithManyOutputsRequest.FromAddress),
                    "Invalid sender adddress");
            }

            for (int i = 0; i < request.Outputs.Count; i++)
            {
                items[i].from = fromAddress;

                if (CoreUtils.ValidateAddress(request.Outputs[i].ToAddress, out var toAddress))
                {
                    items[i].to = toAddress;
                }
                else
                {
                    self.AddModelError(
                        $"{nameof(BuildTransactionWithManyOutputsRequest.Outputs)}[{i}].{nameof(TransactionOutputContract.ToAddress)}",
                        "Invalid sender adddress");
                }

                if (asset != null)
                {
                    try
                    {
                        var coins = Conversions.CoinsFromContract(request.Outputs[i].Amount, asset.DecimalPlaces);
                        items[0].amount = Money.FromUnit(coins, asset.Unit);
                    }
                    catch (ConversionException ex)
                    {
                        self.AddModelError(
                            $"{nameof(BuildTransactionWithManyOutputsRequest.Outputs)}[{i}].{nameof(TransactionOutputContract.Amount)}",
                            ex.Message);
                    }
                }
            }

            return self.IsValid;
        }

        public static bool IsValidRequest(this ModelStateDictionary self,
            BroadcastTransactionRequest request,
            out Transaction transaction,
            out ICoin[] coins)
        {
            (transaction, coins) = (null, null);

            if (!self.IsValid)
            {
                return false;
            }

            try
            {
                (transaction, coins) = Serializer.ToObject<(Transaction, ICoin[])>(request.SignedTransaction.Base64ToString());
            }
            catch (Exception ex)
            {
                self.AddModelError(
                    nameof(BroadcastTransactionRequest.SignedTransaction),
                    ex.Message);
            }

            return self.IsValid;
        }
    }
}
