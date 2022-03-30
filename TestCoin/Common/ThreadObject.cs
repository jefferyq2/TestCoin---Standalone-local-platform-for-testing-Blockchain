using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestCoin.Common
{
    public class ThreadObject
    {
        public int state;
        public Blockcode.Block block;
        public Blockcode.Blockchain blockchain;
        public int extraNonce = 0;
        public int eNonceIncrement = 1; //increment on extra nonce per thread


        public ThreadObject(Blockcode.Blockchain bChain, Blockcode.Block block)
        {
            state = 0;
            this.block = block;
            blockchain = bChain;

        }

        public void calcENonce()
        {
            //cast minerAddress to int somehow
            MD5 md5hasher = MD5.Create();
            Byte[] hash = md5hasher.ComputeHash(Encoding.UTF8.GetBytes(block.minerAddress+state));
            int minerInt = Math.Abs(BitConverter.ToInt32(hash, 0)); //dont want negative value (although it doesnt matter)
            Random ran = new Random(minerInt + DateTime.Now.Millisecond); //create an extraNonce with the intention of making it very unlikely that work will be repeated
            extraNonce = ran.Next();
            if (extraNonce > 2000000000)
            {
                extraNonce = extraNonce - 2000000000;
            }
            eNonceIncrement = (minerInt % 37)+10; //nonce of increment of 10-46
            extraNonce = extraNonce + (eNonceIncrement * state);
        }

    }


    
}
