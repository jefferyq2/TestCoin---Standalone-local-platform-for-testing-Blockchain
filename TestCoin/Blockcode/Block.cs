using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using TestCoin.Common;
using System.Threading;

namespace TestCoin.Blockcode
{
 
    /// <summary>
    /// A Block consists of an index (representing the block it is in the blockchain)
    /// A timestamp, showing when it was created
    /// An array of transactions
    /// The hash of the previous block in the chain
    /// A nonce value 
    /// The hash of this block
    /// </summary>
    public class Block
    {
        public int index;
        public DateTime timestamp;
        public List<Transaction> transactions;
        public String prevHash;
        public long nonce = 0;
        public String hash;
        public double reward;
        public double fees;
        public float difficulty;
        public String minerAddress;
        public String merkleRoot;
        public bool miningDone;
        public int portStore;

        public static Func<int,int,int, bool> testerCB = null;
        public bool hasCBd = false;

        private bool throttle = false;

        public Func<Block,Blockchain,bool> testCB = null;

        private List<char> acceptedSymbols; //first character after 0s in hash may be required to be one the acceptedSymbols depending on difficulty
        private int charCheck = 0; //character to be checked 

        public int hashAttempts = 0; //not vital information but useful to see how many attempts it took.


        public int extraNonce = 0;

        //test variables
        public long nonce0;
        public long nonce1;
        public long nonce2;
        public long nonce3; 

        public Block(int index, List<Transaction> transactions, String prevHash = "", double reward = 0, String minerAddress = "TestCoin Miner Reward", double fees = 0,Func<Block,Blockchain,bool> testcallback = null, bool throttle = false)
        {
            this.index = index;
            this.timestamp = DateTime.Now;
            this.transactions = transactions;
            this.prevHash = prevHash;
            this.fees = fees;
            this.reward = reward;
            this.minerAddress = minerAddress;
            this.merkleRoot = HashTools.GetMerkleRoot(this.transactions);
            this.testCB = testcallback;
            this.throttle = throttle;
        }
        /// <summary>
        /// Used for old blocks on the chain that have already been mined.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="transactions"></param>
        /// <param name="hash"></param>
        /// <param name="prevHash"></param>
        /// <param name=""></param>
        public Block(int index, DateTime time, List<Transaction> transactions, String hash, String prevHash, long nonce, String merkleRoot, double reward, String minerAddress, double fees, float difficulty = 5, int extraN = 0)
        {
            this.index = index;
            this.timestamp = time;
            this.transactions = transactions;
            this.hash = hash;
            this.prevHash = prevHash;
            this.nonce = nonce;
            this.merkleRoot = merkleRoot;
            this.reward = reward;
            this.minerAddress = minerAddress;
            this.fees = fees;
            this.difficulty = difficulty;
            this.extraNonce = extraN;

        }

        public void MineHash(object state)
        {
            bool symCheck = findAcceptedSymbols();
            ThreadObject tObj = (ThreadObject)state;
            String tempHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            long nonceLocal = 0;
            int ENonceLocal = tObj.extraNonce;
            int hashThrottleCount = 0;
            int hashThrottleLimit = 500;
            int hashThrottleSleep = 1500;
            int localEIncrement = tObj.eNonceIncrement;
            Blockchain bC = tObj.blockchain;
            charCheck = ((int)Math.Floor(difficulty));
            if (difficulty%1 == 0)
            {
                charCheck -= 1;
            }
            while (tempHash.Substring(0, (int)difficulty) != String.Join("", Enumerable.Repeat('0', (int)Math.Floor(difficulty)).ToArray()) || (!acceptedSymbols.Contains(tempHash[charCheck]))) //fix this!! should not cast to int be more clever
            {
                if (throttle)
                {
                    if (hashThrottleCount >= hashThrottleLimit)
                    {
                        Thread.Sleep(hashThrottleSleep);
                        hashThrottleCount = 0;
                    }
                    else
                    {
                        hashThrottleCount++;
                    }
                }

                hashAttempts++;
                nonceLocal++;
                tempHash = calcHash(nonceLocal,ENonceLocal);
                if (nonceLocal >= 2000000000000000)
                {
                    ENonceLocal += localEIncrement;
                    nonceLocal = 0; //unlikely but here just in case
                }
                if (miningDone)
                {
                    return;
                }
                if (bC.stopMining)
                {
                    miningDone = true;
                    bC.ToggleMiningVar();

                    if (testerCB != null && !hasCBd)
                    {
                        hasCBd = true;
                        testerCB(hashAttempts,index,0);
                    }

                    return;
                }
            }
            miningDone = true;
            hash = tempHash;
            //Console.WriteLine(nonceLocal);
            nonce = nonceLocal;
            extraNonce = ENonceLocal;
            if (tObj.block.index != 0)
            {
                if (testCB == null)
                {
                    Form1.miningComplete(tObj.block, tObj.blockchain);
                }
                else
                {
                    testCB(tObj.block, tObj.blockchain);
                }
            }
            else
            {
                Form1.genBlockMined(tObj.block, tObj.blockchain);
            }
            //((AutoResetEvent)state).Set();
        }

        public String calcHash(long calcNonce = -1,int extraN = -1)
        {
            SHA256 hashSys;
            hashSys = SHA256Managed.Create();
            calcNonce = calcNonce == -1 ? nonce : calcNonce; //used to distinguish between 
            extraN = extraN == -1 ? extraNonce : extraN;
            Byte[] hashByte = hashSys.ComputeHash(Encoding.UTF8.GetBytes((index.ToString() + timestamp.ToString() + reward + transactions.Count + prevHash + calcNonce + extraN + difficulty)));
            String temphash = string.Empty;
            foreach (byte x in hashByte)
            {
                temphash += String.Format("{0:x2}", x);
            }

            return temphash;
        }

        public void mineBlock(float difficulty, Blockchain bChain, Block block, int threads = 4, int port = -1)
        {
            portStore = port;
            int workerT, compleT, workerA, compleA;
            ThreadPool.GetMaxThreads(out workerT, out compleT);
            ThreadPool.GetAvailableThreads(out workerA, out compleA);
            AutoResetEvent ready = new AutoResetEvent(false);
            miningDone = false;
            //tempHash = calcHash();
            this.difficulty = difficulty;
            reward = calculateReward(index);

            nonce = 0;
            hashAttempts = 0;
            ThreadObject tObj;

            for (int i = 0; i < threads; i++)
            {
                tObj = new ThreadObject(bChain, block);
                tObj.state = i;
                tObj.calcENonce();
                ThreadPool.QueueUserWorkItem(new WaitCallback(MineHash), tObj);  
            }
            //while (!miningDone)
            //{
            //}
            //Console.WriteLine("Block mined: " + hash);
        }


        private bool findAcceptedSymbols()
        {
            //List<char> symbols =  new List<char> { '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'};
            List<char> symbols = new List<char> { '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};
            float remainder = difficulty % 1;
            //int scale = (int)((1 - remainder) / (1f / 35f));
            int scale =(int) ((1-remainder) / (1f / 15f));
            bool checker = false;

            List<char> temp = new List<char>();

            temp.Add('0');

            for (int i = 0; i < scale; i++)
            {
                temp.Add(symbols[i]);
                checker = true;
            }

            acceptedSymbols = temp;
            return checker;
            

        }

        /// <summary>
        /// Weird way of calculating rewards. Gives rewards until the 3200000th block.
        /// Uses Base 20 
        /// </summary>
        /// <returns></returns>
        public static int calculateReward(int index)
        {
            int rewarder = 20;
            int iValue = 0;

            for (int i = 1; i <= 5; i++)
            {
                if (index < rewarder)
                {
                    iValue = i;
                    break;
                }
                else rewarder = rewarder * rewarder;
            }

            if (iValue == 0)
            {
                return iValue;
            }

            int factor = (iValue - 1) * 10;

            if (factor < 0)
            {
                factor = 0;
            }

            return (40 - factor);
        }

        /// <summary>
        /// Validates if correct reward has been given
        /// </summary>
        /// <returns></returns>
        public bool validateReward()
        {
            return (reward == calculateReward(index));
        }


        /// <summary>
        /// Makes multiple checks that block is valid.
        /// Rehashes and compares to provided hash, also makes sure provided hash is legal.
        /// Also checks if reward if calculated correctly
        /// </summary>
        /// <returns></returns>
        public bool StrongValidation()
        {
            findAcceptedSymbols();
            String realHash = calcHash();

            if (!hash.Equals(realHash))
            {
                Console.WriteLine("Validation Fail regarding hash comparison");
                return false;
            }

            if (!EnsureLegalHash())
            {
                Console.WriteLine("Validation Fail regarding hash legality");
                return false;
            }


            if (!validateReward())
            {
                Console.WriteLine("Validation Fail regarding reward.");
                return false;
            }

            

            return true;
        }


        private bool EnsureLegalHash()
        {
            charCheck = ((int)Math.Floor(difficulty));
            if (difficulty % 1 == 0)
            {
                charCheck -= 1;
            }
            if (hash.Substring(0, (int)difficulty) != String.Join("", Enumerable.Repeat('0', (int)Math.Floor(difficulty)).ToArray()) || (!acceptedSymbols.Contains(hash[charCheck]))){
                return false;
            }

            return true;
        }


        public String loginfo()
        {
            String logString = String.Empty;
            logString += ("Block Index: " + index);
            logString += ("     Timestamp: " + timestamp + "\n");
            logString += (" Hash: " + hash + "\n");
            logString += (" Previous Hash: " + prevHash + "\n");
            logString += (" Extra Nonce: " + (int)extraNonce + "\n");
            logString += (" Nonce: " + (long)nonce + "\n");
            logString += (" MerkleRoot: " + merkleRoot + "\n");
            logString += (" Difficulty: " + (float)difficulty + "\n");
            logString += (" Reward: " + (reward) + " to " + minerAddress + "\n");
            logString += (" Transaction Fees Reward: " + (decimal)fees + "\n");
            logString += (" Transaction Details: " + "\n\n");
            //Console.WriteLine("Block Index: " + index); console printing kills performance, only do it for debugging
            foreach (Transaction trans in transactions)
            {
                logString += ("     Transaction Hash: " + trans.hashAddress + "\n");
                logString += ("     Signature: " + trans.signature + "\n");
                logString += ("     Timestamp: " + trans.timestamp.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n");
                logString += ("     Transfered " + trans.amount + " TestCoin" + "\n");
                logString += ("     Fee: " + (decimal)trans.fee + " TestCoin" + "\n");
                logString += ("     To: " + trans.toAdd + "\n");
                logString += ("     From: " + trans.fromAdd + "\n\n");
            }

            return logString;
        }


        public String getMineInfo()
        {
            String info = String.Empty;

            info += "Block " + index + " successfully mined \n";
            info += "Block hash: " + hash + "\n";
            info += "Reward: " + (reward) + " to address " + minerAddress + "\n";
            info += "Extra Nonce: " + (int)extraNonce + "\n";
            info += "Nonce: " + (long)nonce + "\n";
            info += "Hash Attempts: " + hashAttempts + "\n";
            info += "MerkleRoot: " + merkleRoot + "\n";
            info += "Difficulty: " + (float)difficulty + "\n";
            info += "Transaction Fees Reward: " + (decimal)fees + "\n";

            info += transactions.Count + " transactions occured in this block: \n \n";
            foreach(Transaction tran in transactions)
            {
                info += ("     Transaction Hash: " + tran.hashAddress + "\n");
                info += ("     Signature: " + tran.signature + "\n");
                info += ("     Timestamp: " + tran.timestamp.ToString("dd/MM/yyyy HH:mm:ss.ff") + "\n");
                info += ("     Transfered " + tran.amount + " TestCoin" + "\n");
                info += ("     Fee: " + (decimal)tran.fee + " TestCoin" + "\n");
                info += ("     To: " + tran.toAdd + "\n");
                info += ("     From: " + tran.fromAdd + "\n \n");
                
            }


            return info;
        }


        public String simpleRead()
        {
            String info = "";
            info += "Index: " + index + "\n";
            info += "Hash: " + hash.Substring(0, 12) + "\n";
            info += "Nonce: " + nonce + ", Diff: " + difficulty + "\n";
            info += "Transactions: " + transactions.Count + "   Reward: " + reward + "\n";



            return info;
        }

        public bool validateTransactions()
        {
            foreach (Transaction t in transactions)
            {
                if (!t.validateTransaction())
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Compare(Block b1, Block b2, string intent = "")
        {
            bool check = b1.hash.Equals(b2.hash);
            bool c1 = b1.StrongValidation();
            bool c2 = b2.StrongValidation(); //ensures hashes are same and blocks are valid

            return (check && c1 && c2);
        }
    }
}
