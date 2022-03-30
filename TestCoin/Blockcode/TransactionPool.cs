using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCoin.Common;

namespace TestCoin.Blockcode
{
    class TransactionPool //Contains Methods for parsing transaction pools from/into text.
    {
        



        public static String PoolToString(List<Transaction> pool)
        {
            String str = "{";
            foreach (Transaction tran in pool)
            {
                str += tran.getFullDetails();
                str += ";";
            }

            str += "}";

            return str;
        }

        public static List<Transaction> StringToPool(String tranText)
        {

            List<Transaction> transactions = new List<Transaction>();

            try
            {

                string tranStrings = tranText.Substring(tranText.LastIndexOf('{'));
                tranStrings = tranStrings.Substring(1, tranStrings.Length - 1);

                string[] trans = Common.Common.splitAt(tranStrings, ";");
                trans = trans.Take(trans.Count() - 1).ToArray(); //remove empty index

                foreach (string str in trans)
                {
                    transactions.Add(Transaction.Parse(str));
                }


            }
            catch (Exception e)
            {
                //>:(
            }

            return transactions;

        }

        



    }
}
