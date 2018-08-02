using System;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.Zcash.Api.Core;

namespace Lykke.Service.Zcash.Api.Helpers
{
    public static class BlockchainErrorExtensions
    {
        public static BlockchainErrorCode ToContract(this BlockchainException.ErrorCode self)
        {
            return ((BlockchainException.ErrorCode?)self).ToContract();
        }

        public static BlockchainErrorCode ToContract(this BlockchainException.ErrorCode? self)
        {
            switch (self)
            {
                case BlockchainException.ErrorCode.NotEnoughFunds:
                    return BlockchainErrorCode.NotEnoughBalance;

                case BlockchainException.ErrorCode.Dust:
                    return BlockchainErrorCode.AmountIsTooSmall;

                case BlockchainException.ErrorCode.Rejected:
                case BlockchainException.ErrorCode.Expired:
                    return BlockchainErrorCode.BuildingShouldBeRepeated;

                default:
                    return BlockchainErrorCode.Unknown;
            }
        }
    }
}
