﻿using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Lykke.Service.Zcash.Api.Services.Models
{
    public class Utxo
    {
        public string TxId { get; set; }
        public uint Vout { get; set; }
        public string Address { get; set; }
        public string ScriptPubKey { get; set; }
        public decimal Amount { get; set; }
        public long Confirmations { get; set; }

        public Money Money
        {
            get { return Money.Coins(Amount); }
        }

        public ICoin AsCoin()
        {
            return new Coin(uint256.Parse(TxId), Vout, Money, new Script(Encoders.Hex.DecodeData(ScriptPubKey)));
        }

        public OutPoint AsOutPoint()
        {
            return new OutPoint(uint256.Parse(TxId), Vout);
        }

        public TxIn AsTxIn()
        {
            return new TxIn(AsOutPoint());
        }
    }
}
