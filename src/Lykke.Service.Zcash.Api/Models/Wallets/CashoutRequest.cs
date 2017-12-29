using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Lykke.Service.Zcash.Api.Core.Domain;
using Lykke.Service.Zcash.Api.Core.Services;
using NBitcoin;

namespace Lykke.Service.Zcash.Api.Models.Wallets
{
    [DataContract]
    public class CashoutRequest : IValidatableObject
    {
                    
        [DataMember]           public Guid     OperationId { get; set; }
        [DataMember, Required] public string   To          { get; set; }
        [DataMember, Required] public string   AssetId     { get; set; }
        [DataMember, Required] public string   Amount      { get; set; }
        [DataMember]           public string[] Signers     { get; set; }

        public IDestination Destination { get; private set; }
        public Asset        Asset       { get; private set; }
        public Money        Money       { get; private set; }

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
            var blockchainService = (IBlockchainService)validationContext.GetService(typeof(IBlockchainService));
            if (blockchainService == null)
            {
                throw new InvalidOperationException($"Unable to get {nameof(IBlockchainService)} from service container");
            }

            var result = new List<ValidationResult>();

            if (!blockchainService.IsValidAddress(To))
            {
                result.Add(new ValidationResult("Invalud address", new[] { nameof(To) }));
            }

            if (Money == null)
            {
                result.Add(new ValidationResult("Amount must be greater than zero", new[] { nameof(Amount) }));
            }

            if (Signers != null && Signers.Any(a => !blockchainService.IsValidAddress(a)))
            {
                result.Add(new ValidationResult("Invalud signer address(es)", new[] { nameof(Signers) }));
            }

            return result;
        }
    }
}
