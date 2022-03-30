using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCoin.Connections;
using TestCoin.Wallet;

namespace TestCoin.Blockcode
{
    class BlockController
    {
        public static Func<String, bool> FeedbackCallback;
        public static Func<String, bool> FeedbackCallback2;
        public static Func<String, String> getAddress;
        public static Func<int, bool> stopAll;
        public static Func<Block, bool> reportNewBlock;
        public static Func<bool> reportNewTran;
        bool pauseAfterMining = false;

        int blockMiningCount = 0;

        bool blockBeingProcessed = false;


        public bool mineStop = false;


        public String publicID;
        private String privateID;

        private bool firstTime = true;

        public bool throttle;
        public double TPM;
        public bool ableToMine;
        public bool canMine;
        bool stopMining = false;

        public bool stopped = false;
        bool genTran = true;

        public Blockchain testChain;
        MiningTools.MiningSetup miningSetup = new MiningTools.MiningSetup();
        ConnectionController conControl;
        Common.Reader reader;


        bool AutoSave = bool.Parse(ConfigurationManager.AppSettings.Get("AutoSave"));
        bool AutoLoad = bool.Parse(ConfigurationManager.AppSettings.Get("AutoLoad"));
        bool AutoFill = bool.Parse(ConfigurationManager.AppSettings.Get("AutoFill"));
        bool CreateGen = bool.Parse(ConfigurationManager.AppSettings.Get("CreateGenesis"));

        int port = 20000;
        bool portReady = false;

        bool Difficulty = bool.Parse(ConfigurationManager.AppSettings.Get("DifficultyActive"));








        public BlockController(bool throttle, double TPM, bool canMine, bool mineStop)
        {

            Wallet.Wallet wal = new Wallet.Wallet(out privateID);
            publicID = wal.publicID;
            this.throttle = throttle;
            this.TPM = TPM;
            this.ableToMine = canMine;
            this.canMine = canMine;
            this.mineStop = mineStop; 

            SetupChain();



            

        }


        public void Rest()
        {
            canMine = false;
        }

        public void Wake()
        {
            if (ableToMine)
            {
                canMine = true;
            }
        }

        public void Start()
        {
            if (firstTime)
            {
                firstTime = false;
                if (TPM > 0)
                {
                    Thread _TranGenThread = new Thread(AutoTransactionGenerate);
                    _TranGenThread.IsBackground = true;
                    _TranGenThread.Start();
                }
                if (ableToMine)
                {
                    Thread _MineThread = new Thread(AutoMine);
                    _MineThread.IsBackground = true;
                    _MineThread.Start();
                }
            }
            else
            {
                stopped = false;
                blockMiningCount = 0;
            }
        }


        public void SetupChain()
        {
            testChain = new Blockchain(AutoLoad, Difficulty);
            conControl = new ConnectionController(outputText, PortUpdate, outputText, null, testChain, AutoLoader);
            reader = new Common.Reader(outputText, GenericOutput, GenericOutput);
            reader.genCallback = NoFileFound;
            testChain.MiningDone = MineCallBack;

        }

        #region callbacks

        public bool AutoLoader(int port)
        {
            if (AutoLoad)
            {
                this.port = port;
                portReady = true;
                reader.readBCThread(port);

            }
            return AutoLoad;
        }

        public bool outputText(String text)
        {
            return true; //not implemented as most text not required
        }

        public bool PortUpdate(int port)
        {
            FeedbackCallback("Node setup on port " + port);
            return true;
        }


        public bool GenericOutput(Object a, String b)
        {
            return true;
        }

        public bool NoFileFound()
        {
            if (CreateGen)
            {
                while (!portReady)
                {
                    Thread.Sleep(10);
                }
                if (port == 20000)
                {
                    testChain.CreateGenesis();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                //code to request blockchain from network
                return false;
            }
        }

        public bool MineCallBack(Block block, Blockchain chain)
        {
            blockBeingProcessed = true;
            String mineDetails = testChain.addMinedBlock(block);
            reportNewBlock(block);
            //IMPLEMENT
            FeedbackCallback("New block: " + block.index + ", mined by node on port: " + testChain.portNum + "  Hash: " + block.hash.Substring(60) + "  T:" + block.transactions.Count + "  Diff: " + block.difficulty);

            if (mineStop)
            {
                stopAll(port);
            }

            Thread.Sleep(500);

            if (blockMiningCount > 0)
            {
                blockMiningCount--;
            }
            blockBeingProcessed = false;

            return true;
        }

        #endregion


        #region automation

        public void AutoTransactionGenerate()
        {
            double roll;
            int extra = TransformString(publicID);
            Random ran = new Random(DateTime.Now.Millisecond+extra);
            while (genTran)
            {
                Thread.Sleep(1000);
                if (!stopped)
                {
                    roll = ran.Next(1, 60000);
                    roll = roll / 1000;
                    if (TPM >= roll)
                    {
                        GenerateRandomTransaction();
                    }
                }
            }
        }
        /// <summary>
        /// Generates random amount using pub ID balance
        /// Finds random address
        /// Generates small fee/ no fee
        /// </summary>
        public void GenerateRandomTransaction()
        {
            double balance = testChain.getBalance(publicID);
            double amount;
            double fee;
            if (balance > 1)
            {
                amount = new Random(DateTime.Now.Millisecond).NextDouble() * (balance / 10);

            }
            else
            {
                amount = 0; //maybe dont allow this?
                //needs to mine or wait for a transaction
            }
            String reciever = getAddress(publicID);
            Random ran = new Random(DateTime.Now.Millisecond);
            int roll = ran.Next(1, 100);
            if (roll < 40)
            {
                fee = 0;
            }
            else
            {
                fee = amount / 10;
            }
            String output;
            testChain.CreateUserTransaction(publicID, privateID, reciever, amount, fee, out output, 1);
            if (output.Contains("Error"))
            {
                testChain.CreateUserTransaction(publicID, privateID, reciever, 0, 0, out output, 1);
            }
            reportNewTran();
            FeedbackCallback("New Tran, H: " + output.Substring(0, 4) + "  A: " + amount + "   F: " + fee);

        }

        public void AutoMine()
        {
            while (ableToMine)
            {
                Thread.Sleep(50);
                while (canMine && !stopped)
                {
                    if (testChain.isMining)
                    {
                        if (stopMining)
                        {
                            testChain.StopMining();
                        }
                    }
                    else if (!testChain.isMining && blockMiningCount == 0)
                    {
                        blockMiningCount++;
                        if (testChain.stopMining)
                        {
                            testChain.stopMining = false;
                        }
                        Mine();
                    }
                    else if (!testChain.isMining && blockBeingProcessed == false && blockMiningCount > 0)
                    {
                        blockMiningCount--;
                    }
                    Thread.Sleep(2000);
                }

            }
            //check for mining
        }

        public void Mine()
        {
            String mineDetails;
            try
            {
                testChain.minePendingTrans(publicID, miningSetup, out mineDetails,throttle);
            }
            catch(Exception e)
            {
                Console.WriteLine("CRASHOUT! \n" + e.ToString());
            }
            
        }


        #endregion


        #region random

        public int TransformString(String str)
        {
            int sum = 0;
            foreach (char c in str)
            {
                sum += c;
            }
            return sum;
        }

        #endregion
    }


}
