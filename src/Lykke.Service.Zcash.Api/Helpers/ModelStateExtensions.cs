using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Zcash.Api.Core;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Zcash.Api.Helpers
{
    public static class ModelStateExtensions
    {
        public static bool IsValidAddress(this ModelStateDictionary self, IBlockchainService blockchainService, string address)
        {
            if (blockchainService.ValidateAddressAsync(address).Result)
            {
                return true;
            }
            else
            {
                self.AddModelError(nameof(address), $"{address} is not a valid Zcash transparent (t-) address");
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
            IBlockchainService blockchainService,
            out (string from, string to, decimal amount)[] items,
            out Asset asset)
        {
            items = new(string from, string to, decimal amount)[1];
            asset = null;

            if (!self.IsValid)
            {
                return false;
            }

            if (blockchainService.ValidateAddressAsync(request.FromAddress).Result)
            {
                items[0].from = request.FromAddress;
            }
            else
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.FromAddress),
                    "Invalid sender adddress");
            }

            if (blockchainService.ValidateAddressAsync(request.ToAddress).Result)
            {
                items[0].to = request.ToAddress;
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
                    items[0].amount = Conversions.CoinsFromContract(request.Amount, asset.DecimalPlaces);
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
            IBlockchainService blockchainService,
            out (string from, string to, decimal amount)[] items,
            out Asset asset)
        {
            items = new(string from, string to, decimal amount)[request.Inputs.Count];
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

            if (!blockchainService.ValidateAddressAsync(request.ToAddress).Result)
            {
                self.AddModelError(
                    nameof(BuildSingleTransactionRequest.ToAddress),
                    "Invalid destination adddress");
            }

            for (int i = 0; i < request.Inputs.Count; i++)
            {
                if (blockchainService.ValidateAddressAsync(request.Inputs[i].FromAddress).Result)
                {
                    items[i].from = request.Inputs[i].FromAddress;
                }
                else
                {
                    self.AddModelError(
                        $"{nameof(BuildTransactionWithManyInputsRequest.Inputs)}[{i}].{nameof(BuildingTransactionInputContract.FromAddress)}",
                        "Invalid sender adddress");
                }

                items[i].to = request.ToAddress;

                if (asset != null)
                {
                    try
                    {
                        items[i].amount = Conversions.CoinsFromContract(request.Inputs[i].Amount, asset.DecimalPlaces);
                    }
                    catch (ConversionException ex)
                    {
                        self.AddModelError(
                            $"{nameof(BuildTransactionWithManyInputsRequest.Inputs)}[{i}].{nameof(BuildingTransactionOutputContract.Amount)}",
                            ex.Message);
                    }
                }
            }

            return self.IsValid;
        }

        public static bool IsValidRequest(this ModelStateDictionary self,
            BuildTransactionWithManyOutputsRequest request,
            IBlockchainService blockchainService,
            out (string from, string to, decimal amount)[] items,
            out Asset asset)
        {
            items = new(string from, string to, decimal amount)[request.Outputs.Count];
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

            if (!blockchainService.ValidateAddressAsync(request.FromAddress).Result)
            {
                self.AddModelError(
                    nameof(BuildTransactionWithManyOutputsRequest.FromAddress),
                    "Invalid sender adddress");
            }

            for (int i = 0; i < request.Outputs.Count; i++)
            {
                items[i].from = request.FromAddress;

                if (blockchainService.ValidateAddressAsync(request.Outputs[i].ToAddress).Result)
                {
                    items[i].to = request.Outputs[i].ToAddress;
                }
                else
                {
                    self.AddModelError(
                        $"{nameof(BuildTransactionWithManyOutputsRequest.Outputs)}[{i}].{nameof(BuildingTransactionOutputContract.ToAddress)}",
                        "Invalid sender adddress");
                }

                if (asset != null)
                {
                    try
                    {
                        items[0].amount = Conversions.CoinsFromContract(request.Outputs[i].Amount, asset.DecimalPlaces);
                    }
                    catch (ConversionException ex)
                    {
                        self.AddModelError(
                            $"{nameof(BuildTransactionWithManyOutputsRequest.Outputs)}[{i}].{nameof(BuildingTransactionOutputContract.Amount)}",
                            ex.Message);
                    }
                }
            }

            return self.IsValid;
        }

        public static bool IsValidRequest(this ModelStateDictionary self,
            BroadcastTransactionRequest request,
            IBlockchainService blockchainService)
        {
            if (!self.IsValid)
            {
                return false;
            }

            if (!blockchainService.ValidateSignedTransactionAsync(request.SignedTransaction).Result)
            {
                self.AddModelError(
                    nameof(BroadcastTransactionRequest.SignedTransaction),
                    "Invalid signed transaction data");
            }

            return self.IsValid;
        }

        public static BlockchainErrorResponse ToBlockchainErrorResponse(this ModelStateDictionary self)
        {
            var response = new BlockchainErrorResponse
            {
                ModelErrors = new Dictionary<string, List<string>>(),
                ErrorCode = BlockchainErrorCode.Unknown
            };

            foreach (var state in self)
            {
                var messages = state.Value.Errors
                    .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
                    .Select(e => e.ErrorMessage)
                    .Concat(state.Value.Errors
                        .Where(e => string.IsNullOrWhiteSpace(e.ErrorMessage))
                        .Select(e => e.Exception.Message))
                    .ToList();

                if (messages.Any())
                {
                    response.ModelErrors.Add(state.Key, messages);
                }
            }

            return response;
        }
    }
}
