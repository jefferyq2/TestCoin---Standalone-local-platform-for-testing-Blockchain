using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using TestCoin.Common;

namespace TestCoin.Blockcode
{
    public class Transaction
    {
        public String hashAddress; //hashaddress used to find transaction
        public String signature;
        public String fromAdd; //from address
        public String toAdd; //to address
        public DateTime timestamp;
        public double amount;
        public double fee;
        public List<Coin> coins; //coins involved in transaction, must match amount
        public bool isValid;



        /// <summary>
        /// Creates transaction
        /// </summary>
        /// <param name="fromAdd"></param> address of sender
        /// <param name="toAdd"></param> address of reciever
        /// <param name="amount"></param> amount of test coins being sent (can be a 0 tx)
        /// <param name="fee"></param> transaction fee (miner creating blocks will be more likely to pick up your block with a higher fee as they get it) can be 0
        /// <param name="privateKey"></param>
        public Transaction(String fromAdd, String toAdd, double amount, String privateKey = "", double fee = 0)
        {
            timestamp = DateTime.Now; //Recievetime?
            this.fromAdd = fromAdd;
            this.toAdd = toAdd;
            this.amount = amount;
            this.fee = fee;
            coins = new List<Coin>();
            this.hashAddress = createHash(this);
            this.signature = Wallet.Wallet.CreateSignature(fromAdd, privateKey, hashAddress);
        }

        public Transaction(String fromAdd, String toAdd, double amount, double fee, String hash, String signature, DateTime? time = null)
        {
            this.fromAdd = fromAdd;
            this.toAdd = toAdd;
            this.amount = amount;
            this.fee = fee;
            this.hashAddress = hash;
            this.signature = signature;
            if (time == null)
            {
                timestamp = DateTime.Now;
            }
            else
            {
                timestamp = (DateTime) time;
            }
            
        }


        public static String createHash(Transaction trans)
        {
            SHA256 hashSys = SHA256Managed.Create();
            String timeStamp = trans.timestamp.ToString("dd/MM/yyyy HH:mm:ss.ff");
            Byte[] hashByte = hashSys.ComputeHash(Encoding.UTF8.GetBytes(trans.fromAdd + trans.toAdd + trans.amount + trans.fee + trans.timestamp.ToString("dd/MM/yyyy HH:mm:ss.ff")));//+ trans.timestamp.ToString()
            String temphash = string.Empty;
            foreach (byte x in hashByte)
            {
                temphash += String.Format("{0:x2}", x);
            }
            return temphash;
        }

        public bool Compare(Transaction t)
        {
            bool b1 = (fromAdd.Equals(t.fromAdd));
            bool b2 = (toAdd.Equals(t.toAdd));
            bool b3 = (amount.Equals(t.amount));
            bool b4 = (fee.Equals(t.fee));
            bool b5 = (signature.Equals(t.signature));
            bool b6 = (hashAddress.Equals(t.hashAddress));

            return (b1 && b2 && b3 && b4 && b5 && b6);
        }

        public void GenerateCoins(double size)
        {
            for (int i = 0; i < size; i++)
            {
                Coin coin = new Coin(toAdd);
                coins.Add(coin);
            }
        }

        public bool SyncronizeTrans()
        {

            return VerifyCoins();

        }
            

        /// <summary>
        /// Transaction must be verified before being pushed to blockchain
        /// </summary>
        /// <returns></returns>
        public bool VerifyCoins()
        {
            double amount = 0;
            foreach(Coin coin in coins)
            {
                amount += coin.value;
                if (!coin.ValidateCoin()) //ensure coin hasnt been tampered with
                {
                    return false;
                }
            }
            if (amount == this.amount)
            {
                return true;
            }
            return false;
        }

        public String getTransactionDetails()
        {
            String tranDetail = String.Empty;
            tranDetail += "From: " + fromAdd + "\n";
            tranDetail += "To: " + toAdd + "\n";
            tranDetail += "Amount: " + amount + "\n";
            tranDetail += "Fee: " + (decimal)fee + "\n\n";

            return tranDetail;

        }

        public String getFullDetails()
        {
            String details = String.Empty;
            details += "Transaction Hash: " + hashAddress + "\n";
            details += "Signature: " + signature + "\n";
            details += "Timestamp: " + timestamp.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n";
            details += "Transfered " + amount + " TestCoin \n";
            details += "Fee: " + fee + " TestCoin \n";
            details += "To: " + toAdd + "\n";
            details += "From: " + fromAdd + "\n\n";

            return details;
        }

        public bool validateTransaction()
        {
            return hashAddress.Equals(createHash(this));
        }


        public static Transaction Parse(string transText)
        {
            Transaction transaction = null;

            String fromAdd = "";
            String toAdd = "";
            double amount = 0;
            double fee = 0;
            String hash = "";
            String sig = "";
            DateTime? time = null;

            String[] text = Common.Common.splitAt(transText, "newline");

            hash = Common.Common.splitAt(text[0],"Hash: ")[1];

            String tempS;

            for (int i = 1; i < text.Length; i++)
            {
                if (text[i].Contains("Signature: "))
                {
                    sig = Common.Common.splitAt(text[i], "Signature: ")[1];
                }
                else if (text[i].Contains("Timestamp: "))
                {
                    tempS = Common.Common.splitAt(text[i], "Timestamp: ")[1];
                    time = DateTime.ParseExact(tempS, "dd/MM/yyyy HH:mm:ss.ff", null);
                }
                else if (text[i].Contains("Transfered "))
                {
                    tempS = Common.Common.splitAt(text[i], "Transfered ")[1];
                    amount = Double.Parse(Common.Common.splitAt(tempS, "TestCoin")[0]);
                }
                else if (text[i].Contains("Fee: "))
                {
                    tempS = Common.Common.splitAt(text[i], "Fee: ")[1];
                    fee = Double.Parse(Common.Common.splitAt(tempS, "TestCoin")[0]);
                }
                else if (text[i].Contains("To: "))
                {
                    toAdd = Common.Common.splitAt(text[i], "To: ")[1];
                }
                else if (text[i].Contains("From: "))
                {
                    fromAdd = Common.Common.splitAt(text[i], "From: ")[1];
                }
            }

            transaction = new Transaction(fromAdd, toAdd, amount, fee, hash, sig,time);

            return transaction;
        }
    }
}
