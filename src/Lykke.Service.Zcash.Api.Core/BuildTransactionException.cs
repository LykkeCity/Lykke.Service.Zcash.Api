using System;

namespace Lykke.Service.Zcash.Api.Core
{
    public class BuildTransactionException : Exception
    {
        public BuildTransactionException(ErrorCode errorCode, string address, decimal amount) : base($"{Enum.GetName(typeof(ErrorCode), errorCode)} {{{address}:{amount}}}.")
        {
            Error = errorCode;
        }

        public ErrorCode Error { get; }

        public enum ErrorCode
        {
            NotEnoughFunds,
            Dust
        }
    }
}
