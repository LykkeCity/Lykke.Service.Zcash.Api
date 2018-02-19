using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.Zcash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Zcash.Api.Services.Models;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json;

namespace Lykke.Service.Zcash.Api.Services
{
    public class BlockchainReader : IBlockchainReader
    {
        private readonly ILog _log;
        private readonly RPCClient _rpcClient;

        public BlockchainReader(ILog log, RPCClient rpcClient)
        {
            _log = log;
            _rpcClient = rpcClient;
        }

        public async Task<Utxo[]> ListUnspentAsync(int confirmationLevel, params string[] addresses)
        {
            return await SendRpcAsync<Utxo[]>(RPCOperations.listunspent, confirmationLevel, int.MaxValue, addresses);
        }

        public async Task<RawTransaction> GetRawTransactionAsync(string hash)
        {
            return await SendRpcAsync<RawTransaction>(RPCOperations.getrawtransaction, hash, 1);
        }

        public async Task<string> SendRawTransactionAsync(Transaction transaction)
        {
            return (await SendRpcAsync(RPCOperations.sendrawtransaction, transaction.ToHex())).ResultString;
        }

        public async Task<RecentResult> ListSinceBlockAsync(string lastBlockHash, int confirmationLevel)
        {
            return await SendRpcAsync<RecentResult>(RPCOperations.listsinceblock, lastBlockHash, confirmationLevel >= 1 ? confirmationLevel : 1, true);
        }

        public async Task<AddressInfo> ValidateAddressAsync(string address)
        {
            return await SendRpcAsync<AddressInfo>(RPCOperations.validateaddress, address);
        }

        public async Task ImportAddressAsync(string address)
        {
            await SendRpcAsync(RPCOperations.importaddress, address, string.Empty, false);
        }

        public async Task<T> SendRpcAsync<T>(RPCOperations command, params object[] parameters)
        {
            var result = await _rpcClient.SendCommandAsync(command, parameters);

            result.ThrowIfError();

            // NBitcoin can not deserialize shielded tx data,
            // that's why custom models are used widely instead of built-in NBitcoin commands;
            // additionaly in case of exception we save context to investigate later:

            try
            {
                return result.Result.ToObject<T>();
            }
            catch (JsonSerializationException jex)
            {
                await _log.WriteErrorAsync(nameof(SendRpcAsync), $"Command: {command}, Response: {result.ResultString}", jex);
                throw;
            }
        }

        public async Task<RPCResponse> SendRpcAsync(RPCOperations command, params object[] parameters)
        {
            var result = await _rpcClient.SendCommandAsync(new RPCRequest(command, parameters), false);

            result.ThrowIfError();

            return result;
        }
    }
}
