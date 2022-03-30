using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCoin.Blockcode;

namespace TestCoin.MiningTools
{
    public class MiningSetup
    {
        public double altruismLevel; //min fee to pickup (how generous you want to be) 
        public double maxTransactionsPickup;
        public String pickAddress; //makes sure that transactions involving this address are picked up, nice to ensure your own transactions are going to go through
        public int pickupState; //0 pick highest first (default), 1 pick newest first, 2 pick old first 

        public int threadsUsed; //Decide how many threads you want to used while mining (1-8). The more used the higher the hash rate, but more processing is required. 

        MiningSettings miningSettings;

        public bool isNew;

        public MiningSetup()
        {
            altruismLevel = 0; //kind by default
            maxTransactionsPickup = 15;
            pickAddress = "";
            pickupState = 0;
            threadsUsed = 8;
            miningSettings = new MiningSettings(this);
        }




        public void ShowDialog()
        {  
            miningSettings.ShowDialog();
        }

        /// <summary>
        /// Takes pending transactions and returns a list with transactions that agree with the mining setup
        /// </summary>
        /// <param name="pendingTransactions"></param>
        /// <returns></returns>
        public List<Transaction> findTransactions(List<Transaction> pendingTs, out List<Transaction> leftoverTransactions)
        {
            List<Transaction> pendingTransactions = new List<Transaction>(pendingTs); //makes copy not reference
            int transactionsFilled = 0;
            switch (pickupState)
            {
                case 2:
                    pendingTransactions.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));
                    break;
                case 1:
                    pendingTransactions.Sort((x, y) => y.timestamp.CompareTo(x.timestamp));
                    break;
                default:
                    pendingTransactions.Sort((x, y) => y.fee.CompareTo(x.fee));
                    break;

            } 
            List<Transaction> chosenTransactions = new List<Transaction>();
            leftoverTransactions = new List<Transaction>();
            List<Transaction> leftoverTransactions2 = new List<Transaction>();
            foreach(Transaction trans in pendingTransactions)
            {
                if (trans.fromAdd.Equals(pickAddress) || trans.toAdd.Equals(pickAddress)){
                    chosenTransactions.Add(trans);
                    transactionsFilled++;
                }
                else
                {
                    leftoverTransactions.Add(trans);
                }
            }
            foreach(Transaction trans in leftoverTransactions)
            {
                if (trans.fee >= altruismLevel && transactionsFilled < maxTransactionsPickup && !(trans.fromAdd.Equals(pickAddress) || trans.toAdd.Equals(pickAddress)))
                {
                    chosenTransactions.Add(trans);
                    transactionsFilled++;
                }
                else
                {
                    leftoverTransactions2.Add(trans);
                }
            }
            leftoverTransactions = leftoverTransactions2;
            return chosenTransactions;

        }
    }
}
