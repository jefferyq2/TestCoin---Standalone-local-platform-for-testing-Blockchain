using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCoin.Blockcode;
using System.Security.Cryptography;

namespace TestCoin.Common
{
    public static class HashTools
    {
        /// <summary>
        /// Takes byte array and returns hexadecimal string
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static String GetMerkleRoot(List<Transaction> transactions)
        {
            List<String> transactionHases = new List<String>();

            foreach (Transaction trans in transactions)
            {
                transactionHases.Add(trans.hashAddress);
            }
            return GetMerkleRoot(transactionHases);
        }


        public static String GetMerkleRoot(List<String> transHashes)
        {
            String combinedHash;
            List<String> tempHashes;
            bool isOdd = (0 != (transHashes.Count % 2));

            if (transHashes.Count == 1)
            {
                return combineHash(transHashes[0], transHashes[0]); //if there is only one hash then return product of its hashxhash
            }

            if (transHashes.Count == 0)
            {
                return "GenesisMerkle";
            }

            while (transHashes.Count != 1)
            {
                tempHashes = new List<String>();

                for (int i = 0; i < transHashes.Count; i += 2)
                {
                    if (i == transHashes.Count - 1)
                    {
                        combinedHash = combineHash(transHashes[i], transHashes[i]);
                    }
                    else
                    {
                        combinedHash = combineHash(transHashes[i], transHashes[i + 1]);
                    }

                    tempHashes.Add(combinedHash);
                }
                transHashes = tempHashes;
            }


            return transHashes[0];
        }


        public static bool verifyMerkleRoot(String givenMRoot, Block block)
        {
            return givenMRoot.Equals(GetMerkleRoot(block.transactions));
        }


        public static String combineHash(String hash1, String hash2)
        {
            Byte[] bytes1 = StringToByteArray(hash1);
            Byte[] bytes2 = StringToByteArray(hash2);

            SHA256 hashSys = SHA256Managed.Create();
            Byte[] combinedbytes = hashSys.ComputeHash(bytes1.Concat(bytes2).ToArray());

            return ByteArrayToString(combinedbytes);

        }

    }
}
