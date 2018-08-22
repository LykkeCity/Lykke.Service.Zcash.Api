using System;

namespace Lykke.Service.Zcash.Api.Core
{
    public class BlockchainException : Exception
    {
        public BlockchainException(ErrorCode errorCode, string message) : base(message)
        {
            Error = errorCode;
        }

        public ErrorCode Error { get; }

        public enum ErrorCode
        {
            NotEnoughFunds,
            Dust,
            Rejected,
            Expired
        }
    }
}
