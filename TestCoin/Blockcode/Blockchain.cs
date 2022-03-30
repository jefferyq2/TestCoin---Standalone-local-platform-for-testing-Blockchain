using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using TestCoin.Common;
using System.Diagnostics;

namespace TestCoin.Blockcode
{
    public class Blockchain
    {

        public List<Transaction> pendingTrans;
        public int transactionSizeLimit = 100; //100 transaction in memory at once, roughly 40-50kb of text
        float difficulty = 5; //should gradually get harder (default for first 10 blocks)
        float minDifficultly = 4; //difficulty cannot get lower than 4 (trivial otherwise)
        List<Block> chain;
        double mineReward;
        int currBlock = 0; //block being inspected
        public bool isMining = false;
        public int blockMinedInt = -1;
        List<Transaction> leftoverTrans;
        Stopwatch mineTime = new Stopwatch();

        public Func<Block,Blockchain,bool> MiningDone = null;

        public Func<Transaction,bool> transactionCallback;
        public Func<Block, bool> newBlockCallback;

        public bool stopMining = false;

        bool diff = true; //by default difficulty is used

        List<Block> validChain = null;
        Reader reader;

        public Block lastBlock = null; //latest block on chain
        public Block RLastBlock = null;
        Block lastBlockCopy = null;
        public int lastBlockint = -1;

        public bool autosave = true;

        private List<Block> lastBlocksStore;
        private List<Block> chainCallStore = null;

        public int portNum = 20000;


        public Blockchain(bool autoload, bool diffi)
        {
            reader = new Reader(PrintCallBack, BlockCallBack, ChainCallBack);
            reader.genCallback = NoChainFoundCallBack;
            chain = new List<Block>();
            pendingTrans = new List<Transaction>();
            diff = diffi;
            if (!autoload)
            {
                Block genBlock = new Block(0, new List<Transaction> { });//new Transaction("Genesis", "Genesis", 0)
                genBlock.mineBlock(difficulty, this, genBlock);
                chain.Add(genBlock);
                AssignLastBlock(genBlock);

            }

        }

        public void AssignLastBlock(Block b = null)
        {
            if (lastBlock != null)
            {
                lastBlock = b;
                RLastBlock = b;
            }
            else
            {
                lastBlock = chain[chain.Count - 1];
                RLastBlock = lastBlock;
            }
        }

        public void CreateGenesis()
        {
            Block genBlock = new Block(0, new List<Transaction> { });
            genBlock.mineBlock(difficulty, this, genBlock);
            chain.Add(genBlock);
            AssignLastBlock(genBlock);
        }

        public void LoadBlockchain(List<Block> chain)
        {
            lastBlock = chain[chain.Count - 1]; //last member of chain
            RLastBlock = lastBlock;
        }

        public void readChainFromFile()
        {

        }

        public void readSliceFromFile()
        {

        }

        public bool ValidationCallBack(List<Block> blockchain, string intent)
        {
            validChain = blockchain;
            reader.chainCallback = ChainCallBack;
            return true;
        }

        public bool ChainCallBack(List<Block> blockchain, string intent)
        {
            if (blockchain.Count == 11)
            {
                lastBlocksStore = blockchain;
            }
            else
            {
                chainCallStore = blockchain;
            }
            return true;
        }

        public bool PrintCallBack(string message)
        {
            //Console.WriteLine(message);
            return true;
        }

        public bool NoChainFoundCallBack()
        {
            validChain = chain;
            reader.chainCallback = ChainCallBack;
            return true;
        }

        public bool BlockCallBack(Block block, string intent)
        {
            lastBlock = block;
            reader.chainCallback = ChainCallBack;
            return true;
        }

        public bool DiffCallBack(List<Block> last10, string intent)
        {
            lastBlocksStore = last10;
            return true;
        }



        public bool minePendingTrans(String rewardAddress, MiningTools.MiningSetup settings, out String blockDetails, bool throttle = false)
        {
            blockDetails = "";
            if (!isMining )
            {
                try
                {
                    isMining = true;
                    lastBlockCheck();
                    
                    if (portNum == 20000)
                    {
                        stopMining = false;
                    }

                    
                    List<Transaction> chosenTrans = settings.findTransactions(pendingTrans, out leftoverTrans); //transactions chosen to be included in this block
                    double feeTotal = CountFees(chosenTrans);
                    //validate all pending trans before starting
                    if (diff)
                    {
                        difficulty = calcDifficulty(lastBlockint);

                        if (difficulty == -1)
                        {
                            isMining = false;
                            return false;
                        }
                    }
                    mineReward = Block.calculateReward(RLastBlock.index + 1) + feeTotal;
                    chosenTrans.Add(new Transaction("TestCoin Mine Rewards", rewardAddress, mineReward)); //adds miner reward to block
                                                                                                          //ValidatePendingTrans(); //invalid transactions are removed




                    blockMinedInt = RLastBlock.index + 1;
                    Block block = new Block(RLastBlock.index + 1, chosenTrans, RLastBlock.hash, mineReward, rewardAddress, feeTotal, MiningDone,throttle);

                    mineTime = Stopwatch.StartNew();
                    block.mineBlock(difficulty, this, block, settings.threadsUsed, portNum);



                    blockDetails = "Mining started at difficulty: " + difficulty + ".....";

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    blockDetails = e.ToString();
                }

            }
            else
            {
                blockDetails = "You are already mining";
            }

            return false;
            //empties pending transactions and sets up pending transfer to reward miner address

            

        }

        public void updateLastNum(Block b)
        {
            
            if (b.index > lastBlockint)
            {
                lastBlockint = b.index;
                RLastBlock = b;
            }
        }

        public void lastBlockCheck()
        {

            

            if (lastBlock == null)
            {
                reader.lastThread(portNum);
                lastBlockCopy = lastBlock;
                while (lastBlockCopy == lastBlock)
                {
                    Thread.Sleep(10);
                }
                RLastBlock = lastBlock;
                lastBlockint = RLastBlock.index;
            }
            if (lastBlockint != RLastBlock.index)
            {
                reader.lastThread(portNum);
                lastBlockCopy = lastBlock;
                while (lastBlockCopy == lastBlock)
                {
                    Thread.Sleep(10);
                }
                RLastBlock = lastBlock;
                lastBlockint = RLastBlock.index;
            }

        }

        private float calcDifficulty(int lastBlockIndex)
        {
            if (lastBlockIndex < 15)
            {
                return 4.5f;
            }
            else
            {
                //reader.chainCallback = DiffCallBack;

                reader.SliceThread(lastBlockIndex - 10, lastBlockIndex, "Slice", portNum);

                //reader.readBCThread();

                lastBlocksStore = null;

                while (lastBlocksStore == null)
                {
                    Thread.Sleep(10);
                }

                List<Block> lastblocks = lastBlocksStore;
                lastBlocksStore = null;

                if (lastblocks.Count <= 10)
                {
                    return -1;
                }

                List<float> diffList = new List<float>();

                float timeDiffSum = 0; //sum of all time differences in the last 10 blocks

                float lastDiff = lastblocks[lastblocks.Count - 1].difficulty;

                float difference = 0;

                float difficultyChangeSum = 0;

                for (int i = 1; i < 11; i++)
                {
                    difference = (float)(lastblocks[i].timestamp - lastblocks[i - 1].timestamp).TotalSeconds;

                    difficultyChangeSum += (lastblocks[i].difficulty - lastblocks[i - 1].difficulty);

                    if (difference > 150)
                    {
                        difference = 150; //difference cannot be higher than 2.5 minutes (Slightly prevents anomalies)
                    }
                    timeDiffSum += difference;
                    if (i == 10)
                    {
                        timeDiffSum += difference;
                        timeDiffSum += difference;
                        timeDiffSum += difference;
                        //gives latest block addition 4 times more relavence to diff calc
                    }
                }

                float avgTimeDiff = timeDiffSum / 13; //average time difference in last 10 blocks

                //10 second block time is what i want. Need to evaluate avgTimeDiff to 30 S

                float difficultySkewer = 30 - avgTimeDiff; //closer to 0 the better

                //consider current difficulty and how to adjust

                float bias = (1 * (difficultySkewer / 60));


                float difficultyChangeSkewer = difficultyChangeSum / 25;



                if (bias > 0.05f)
                {
                    bias = 0.05f;
                    if (difficultyChangeSkewer > 0)
                    {
                        bias = 0.05f - difficultyChangeSkewer;
                    }
                }
                else if (bias < -0.1f)
                {
                    bias = -0.1f;
                    if (difficultyChangeSkewer < 0)
                    {
                        bias = -0.1f + difficultyChangeSkewer;
                    }
                }

                float newDiff = lastDiff + bias;

                if (newDiff < minDifficultly)
                {
                    return minDifficultly;
                }


                return newDiff;
            }
        }

        public String addMinedBlock(Block block)
        {
            List<Block> newchain;
            mineTime.Stop();
            isMining = false;
            newBlockCallback(block);
            lastBlock = block;
            lastBlockint = lastBlock.index;
            if (isChainValid(out newchain, block))
            {
                if (autosave)
                {
                    //SaveChainInfo(newchain);
                    AppendSaveChainInfo(block);
                    updateLastNum(block);
                    SortTranPool(block);
                }
               
                RLastBlock = block;
                return "Time Taken " + mineTime.Elapsed + "\n" + block.getMineInfo();
            }
            return "Block mined invalid so cannot be added to chain";


        }

        public void SortTranPool(Block b)
        {
            List<Transaction> pT = new List<Transaction>(pendingTrans);
            List<Transaction> leftOvers = new List<Transaction>();
            bool check = true;
            foreach (Transaction t in pT)
            {
                check = true;
                foreach (Transaction tran in b.transactions)
                {
                    if (tran.hashAddress.Equals(t.hashAddress))
                    {
                        check = false;
                    }
                }
                if (check)
                {
                    leftOvers.Add(t);
                }
            }
            pendingTrans = leftOvers;
        }

        public void GenBlockMined(List<Block> block)
        {
            if (autosave)
            {
                SaveChainInfo(block); //use save here, opposed to append as it creates the file
            }
        }




        private double CountFees(List<Transaction> transactions)
        {
            double fees = 0;
            foreach (Transaction tran in transactions)
            {
                fees += tran.fee;
            }
            return fees;
        }


        public void ValidatePendingTrans()
        {
            foreach (Transaction trans in pendingTrans)
            {
                if (trans.fromAdd.Equals("TestCoin Mine Rewards"))
                {
                    trans.GenerateCoins(mineReward);
                }
                trans.SyncronizeTrans();
            }
        }


        private void CreateTransaction(Transaction transaction)
        {
            //add confirmation
            this.pendingTrans.Add(transaction);
        }

        public bool CreateUserTransaction(String publicAdd, String privKey, String receiveID, double amount, double fee, out String outputMessage, int config = 0)
        {
            if (Wallet.Wallet.ValidatePrivateKey(privKey, publicAdd)) {
                if (getBalance(publicAdd, true) - amount - fee >= 0)
                {
                    Transaction newTransaction = new Transaction(publicAdd, receiveID, amount, privKey, fee);
                    pendingTrans.Add(newTransaction);
                    if (config == 0)
                    {
                        outputMessage = "Success: New transaction created. \nSending: " + amount + "\nFees: " + (decimal)fee + "\nFrom: " + publicAdd + "\nTo: " + receiveID;
                        outputMessage += "\n\nTransaction Hash: " + newTransaction.hashAddress + "\n" + "Signature: " + newTransaction.signature;
                    }
                    else
                    {
                        outputMessage = newTransaction.hashAddress;
                    }
                    transactionCallback(newTransaction);
                    return true;
                }
                else
                {
                    outputMessage = "Error: Wallet does not have enough funds to send transaction";
                    return false;
                }
            }
            else
            {
                outputMessage = "Error: Private Key is invalid, transaction cannot be completed";
                return false;
            }


        }


        private List<Block> commonChainReader()
        {
            reader.chainCallback = ValidationCallBack;

            reader.readBCThread(portNum);

            while (validChain == null)
            {
                Thread.Sleep(10);
            }

            List<Block> readChain = validChain;
            validChain = null;
            return readChain;
        }


        public double getBalance(String address, bool getAvaialibleBalance = false)
        {
            double balance = 0;

            List<Block> rChain = commonChainReader();

            try
            {

                foreach (Block block in rChain)
                {
                    foreach (Transaction trans in block.transactions)
                    {
                        if (trans.fromAdd == address)
                        {
                            balance -= trans.amount;
                            balance -= trans.fee;
                        }
                        if (trans.toAdd == address)
                        {
                            balance += trans.amount;
                        }
                    }
                }
                if (getAvaialibleBalance)
                {
                    foreach (Transaction trans in pendingTrans)
                    {
                        if (trans.fromAdd == address)
                        {
                            balance -= trans.amount;
                            balance -= trans.fee;
                        }
                        if (trans.toAdd == address)
                        {
                            //balance += trans.amount; dont add pending transactions to balance
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Balance Error; returning 0");
                return 0;
            }

            return balance;
        }


        public String getTransactionDetails(String address, bool incoming = true, bool outgoing = true, bool blockTransactions = true, bool pendingTransactions = true)
        {
            String transactionDetails = "Transactions: \n\n";
            String incomingTran = "Incoming Transactions: \n\n";
            String outgoingTran = "Outgoing Transactions: \n\n";
            String incomingPending = "Incoming Pending Transactions: \n\n";
            String outgoingPending = "Outgoing Pending Transactions: \n\n";
            bool addressBool = address.Equals("");

            List<Block> rChain = commonChainReader();

            if (pendingTransactions)
            {
                foreach (Transaction trans in pendingTrans)
                {
                    if ((trans.fromAdd.Equals(address) || addressBool) && outgoing)
                    {
                        outgoingPending += trans.getFullDetails();
                    }
                    if ((trans.toAdd.Equals(address) || addressBool) && incoming)
                    {
                        incomingPending += trans.getFullDetails();
                    }
                }
            }

            if (blockTransactions)
            {
                foreach (Block block in rChain)
                {
                    foreach (Transaction trans in block.transactions)
                    {
                        if ((trans.fromAdd.Equals(address) || addressBool) && outgoing)
                        {
                            outgoingTran += trans.getFullDetails();
                        }
                        if ((trans.toAdd.Equals(address) || addressBool) && incoming)
                        {
                            incomingTran += trans.getFullDetails();
                        }
                    }
                }
            }


            if (pendingTransactions)
            {
                if (incoming)
                {
                    transactionDetails += incomingPending;
                }
                if (outgoing)
                {
                    transactionDetails += outgoingPending;
                }

            }
            if (blockTransactions)
            {
                if (incoming)
                {
                    transactionDetails += incomingTran;
                }
                if (outgoing)
                {
                    transactionDetails += outgoingTran;
                }
            }

            return transactionDetails;

        }

        public Block getLatestBlock()
        {
            return chain[chain.Count - 1];
        }

        public bool isChainValid(out List<Block> tempChain, Block block = null, List<Block> extraChain = null)
        {
            tempChain = null;
            try
            {
                reader.chainCallback = ValidationCallBack;

                reader.readBCThread(portNum);

                while (validChain == null)
                {
                    Thread.Sleep(10);
                }

                tempChain = validChain;
                validChain = null;


                if (block != null)
                {
                    //extend functionailty more if required
                    tempChain.Add(block);

                }
                else if (extraChain != null)
                {
                    tempChain.AddRange(extraChain);
                }

                for (int i = 1; i < tempChain.Count; i++)
                {
                    Block currBlock = tempChain[i];
                    Block prevBlock = tempChain[i - 1];

                    if (!currBlock.StrongValidation())
                    {
                        return false;
                    }
                    if (currBlock.prevHash != prevBlock.hash)
                    {
                        if (block == null)
                        {
                            fixChain(i);
                        }
                        else if (block.index > i)
                        {
                            fixChain(i);
                        }
                        return false;
                    }
                    if (!HashTools.verifyMerkleRoot(tempChain[i].merkleRoot, tempChain[i]))
                    { //error caughts?
                        return false;
                    }
                    if (!currBlock.validateTransactions())
                    {
                        return false;
                    }
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public void fixChain(int i)
        {
            Console.WriteLine("Chain is invalid, attempting to fix");
            List<Block> validBlocks = lastBlocksStore;
            reader.SliceThread(0, i - 1, "slice", portNum);

            chainCallStore = null;

            while (chainCallStore == null)
            {
                Thread.Sleep(10);
            }
            validBlocks = chainCallStore;
            chainCallStore = null;
            SaveChainInfo(validBlocks);
            lastBlockint = i-1;
            lastBlockCheck();
            Console.WriteLine("UPDATED INVALID CHAIN TO VALID MEMBERS: " + (i-1));
        }

        public String getChainInfo()
        {
            String blockChainInfo = String.Empty;
            foreach (Block b in chain)
            {
                blockChainInfo += b.loginfo();
            }

            return blockChainInfo;
        }

        public String getChainInfo(List<Block> blocks)
        {
            String blockChainInfo = String.Empty;
            foreach (Block b in blocks)
            {
                blockChainInfo += b.loginfo();
            }

            return blockChainInfo;
        }


        public String SaveChainInfo(List<Block> newchain = null) //ideally make an append save too
        {
            if (newchain == null)
            {
                newchain = chain;
            }
            //String saveLocation = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString() + "\\Testchain.tcn";
            String saveLocation = FindPath();

            using (StreamWriter file = new StreamWriter(saveLocation)) //tcn = testchain
            {
                foreach (Block b in newchain)
                {
                    file.Write(b.loginfo());
                }


                

                return saveLocation;
            }

        }

        public String AppendSaveChainInfo(Block block = null) //Superior to SaveChainInfo as uses less memory, using SaveChainInfo is bad
        {
            try
            {
                if (block == null)
                {
                    return SaveChainInfo(); //thinking emojie
                }

                
                //String AppendLocation = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString() + "\\Testchain.tcn";
                String AppendLocation = FindPath();

                if (block.hash.Equals(RLastBlock.hash))
                {
                    return AppendLocation;
                }

                if (File.Exists(AppendLocation))
                {

                    waitForFile(AppendLocation);
                    
                    using (StreamWriter file = File.AppendText(AppendLocation))
                    {
                        file.Write(block.loginfo()); //appending > overwriting
                    }

                    lastBlockint = block.index;
                    RLastBlock = block;

                    return AppendLocation;
                }
                else
                {
                    return SaveChainInfo(); //boooo
                }
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }

        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void waitForFile(String path)
        {
            while (!IsFileReady(path))
            {
                Thread.Sleep(2);
            }
        }


        public String getBlockInfo(int index)
        {
            if (index >= chain.Count || index < 0)
            {
                return ("Block " + index + " does not exist");
            }
            currBlock = index;

            return chain[index].loginfo();
        }

        public String nextBlockInfo()
        {
            currBlock++;
            return getBlockInfo(currBlock);
        }


        public String prevBlockInfo()
        {
            if (currBlock > 0)
            {
                currBlock--;
            }
            return getBlockInfo(currBlock);
        }


        public int getCurrBlock()
        {
            return currBlock;
        }


        public void StopMining()
        {
            if (!isMining)
            {
                return;
            }
            stopMining = true;
            while (isMining)
            {
                Thread.Sleep(20);
            }
            stopMining = false;

            Console.WriteLine("Mining Stopped");


        }

        public void ToggleMiningVar()
        {
            isMining = false;
        }

        public bool AddToPending(Transaction tran)
        {
            if (!PendingContainCheck(tran))
            {
                if (transactionSizeLimit > pendingTrans.Count)
                {
                    pendingTrans.Add(tran);
                    return true;
                }
                else
                {
                    return TransPoolLowAlg(tran);
                }
            }
            return false;
        }

        public bool PendingContainCheck(Transaction tran)
        {

            foreach (Transaction t in pendingTrans)
            {
                if (t.Compare(tran))
                {
                    return true;
                }
            }
            return false;

        }

        private bool TransPoolLowAlg(Transaction newTran)
        {
            Transaction lowFee = null;
            foreach (Transaction tran in pendingTrans)
            {
                if (lowFee == null)
                {
                    lowFee = tran;
                }
                else if (lowFee.fee > tran.fee)
                {
                    lowFee = tran;
                }
            }
            if (lowFee.fee < newTran.fee)
            {
                Console.WriteLine("Removed:\n" + lowFee.getFullDetails());
                pendingTrans.Remove(lowFee);
                pendingTrans.Add(newTran);
                Console.WriteLine("Added:\n" + newTran.getFullDetails());
                return true;
            }
            return false;
        }


        public String FindPath()
        {
            try
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory().ToString() + "\\Storage");
            }
            catch (Exception e)
            {

            }

            try
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory().ToString() + "\\Storage\\" + portNum);
            }
            catch (Exception e)
            {

            }

            String path = Directory.GetCurrentDirectory().ToString() + "\\Storage\\" + portNum + "\\Testchain.tcn";
            return path;
        }


        public void PendingTransRemoval(Block block)
        {
            bool removeHere;
            for (int j = pendingTrans.Count - 1; j >= 0; j--)
            {
                removeHere = false;
                for (int i = 0; i < block.transactions.Count; i++)
                {
                    if (block.transactions[i].Compare(pendingTrans[j]))
                    {
                        removeHere = true;
                    }
                }
                if (removeHere)
                {
                    pendingTrans.RemoveAt(j);
                }
            }

        }

        public List<Block> getSlice(int start, int fin)
        {
            reader.SliceThread(start, fin, "Slice", portNum);

            //reader.readBCThread();

            chainCallStore = null;

            int counter = 0;

            while (chainCallStore == null)
            {
                Thread.Sleep(10);
                counter++;
                if (counter > 10)
                {
                    return null;
                }
            }

            List<Block> lastblocks = chainCallStore;
            chainCallStore = null;

            return lastblocks;
        }


    }
}
