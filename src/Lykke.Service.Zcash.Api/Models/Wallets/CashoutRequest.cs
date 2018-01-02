using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Lykke.Service.Zcash.Api.Core;
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

        public IDestination     Destination     { get; private set; }
        public Asset            Asset           { get; private set; }
        public Money            Money           { get; private set; }
        public BitcoinAddress[] SignerAddresses { get; private set; }

        [OnDeserialized]
        public void Init(StreamingContext streamingContext = default)
        {
            Asset = Constants.Assets.ContainsKey(AssetId) ? 
                Constants.Assets[AssetId] : 
                Asset.Zec;

            Money = decimal.TryParse(Amount, out decimal money) ?
                new Money(money, Asset.Unit) : 
                null;

            try
            {
                Destination = BitcoinAddress.Create(To);
            }
            catch
            {
                Destination = null;
            }

            if (Signers != null && Signers.Any())
            {
                try
                {
                    SignerAddresses = Signers.Select(a => BitcoinAddress.Create(To)).ToArray();
                }
                catch
                {
                    SignerAddresses = null;
                }
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var result = new List<ValidationResult>();

            if (Destination == null)
            {
                result.Add(new ValidationResult("Invalud address", new[] { nameof(To) }));
            }

            if (Money == null)
            {
                result.Add(new ValidationResult("Amount must be greater than zero", new[] { nameof(Amount) }));
            }

            if (Signers != null && Signers.Any() && SignerAddresses == null) 
            {
                result.Add(new ValidationResult("Invalud signer address(es)", new[] { nameof(Signers) }));
            }

            return result;
        }
    }
}
