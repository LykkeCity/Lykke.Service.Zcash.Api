using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Models.Transaction
{
    [DataContract]
    public class CashoutRequest : IValidatableObject
    {
        [Required] public string   To      { get; set; }
        [Required] public string   AssetId { get; set; }
        [Required] public string   Amount  { get; set; }
                   public string[] Signers { get; set; }
                   public Money    Money   { get; private set; }

        [OnDeserialized]
        public void Init(StreamingContext streamingContext = default)
        {
            if (ulong.TryParse(Amount, out ulong satoshis))
                Money = Money.Satoshis(satoshis);
            else
                Money = null;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var walletService = (IWalletService)validationContext.GetService(typeof(IWalletService));
            if (walletService == null)
            {
                throw new InvalidOperationException($"Unable to get {nameof(IWalletService)} from service container");
            }

            var result = new List<ValidationResult>();

            if (!walletService.IsValid(To))
            {
                result.Add(new ValidationResult("Invalud address", new[] { nameof(To) }));
            }

            if (Money == null)
            {
                result.Add(new ValidationResult("Amount must be greater than zero", new[] { nameof(Amount) }));
            }

            if (Signers != null && Signers.Any(a => !walletService.IsValid(a)))
            {
                result.Add(new ValidationResult("Invalud signer address(es)", new[] { nameof(Signers) }));
            }

            return result;
        }
    }
}
