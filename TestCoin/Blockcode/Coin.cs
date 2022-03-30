using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TestCoin.Blockcode
{
    public class Coin
    {
        public String coinHash; //each coin has a unique hash allowing it to be traced, therefore coins actually exist in this blockchain
        public String ownerAddress;
        public String prevOwnerAddress; //previous owner address required for confirmation
        public DateTime stamp;
        public double seed;
        public String data; //Data can be stored in coins, 1 char per 0.001 TestCoin
        public double value; //value of coin from 0.0 to 1


        public Coin(String address)
        {
            ownerAddress = address;
            value = 1;
            Random random = new Random(DateTime.Now.Millisecond);
            stamp = DateTime.Now;
            seed = random.NextDouble();
            coinHash = CreateHash();


        }

        public Coin(String address, double value)
        {
            ownerAddress = address;
            this.value = value;
            Random random = new Random(DateTime.Now.Millisecond);
            stamp = DateTime.Now;
            seed = random.NextDouble();
            coinHash = CreateHash();
        }

        private String CreateHash() //makes hash out of timestamp, value and random seed
        {

            SHA256 hashSys = SHA256Managed.Create();
            Byte[] hashByte = hashSys.ComputeHash(Encoding.ASCII.GetBytes(stamp.ToString() + seed.ToString() + value.ToString()));
            String temphash = String.Empty;
            foreach (Byte x in hashByte)
            {
                temphash += String.Format("{0:x2}", x);
            }
            return temphash;
        }

        public bool ValidateCoin()
        {
            return CreateHash().Equals(coinHash);
        }

        public bool pumpData(String data)
        {
            if (data.Length > value / 1000)
            {
                return false;
            }
            else
            {
                this.data = data;
                return true;
            }
        }

        /// <summary>
        /// Splits a coins value into two parts of two values
        /// One part has the value of the splitValue
        /// The other part has the value of remainder of the coin.
        /// Cannot/Shouldn't be called by itself
        /// </summary>
        /// <param name="splitValue"></param>
        public bool splitCoin(double splitValue, out Coin coin1, out Coin coin2)
        {
            if (value > splitValue)
            {
                double storeVal = value - splitValue;
                coin1 = new Coin(ownerAddress, splitValue);
                coin2 = new Coin(ownerAddress, storeVal);
                Console.WriteLine("Successful Split");
                value = 0;
                return true; //this coin now needs to be replaced by coin1, just in case this doesnt happen value is changed to 0 to invalidate the coin.
                //technially a 0 value coin can exist, however if the coin was rehash it wouldn't match with its current hash value; so it is invalidated.
            }
            else
            {
                coin1 = null;
                coin2 = null;
                Console.WriteLine("Split Failure");
                return false;
            }
        }
    }
}
