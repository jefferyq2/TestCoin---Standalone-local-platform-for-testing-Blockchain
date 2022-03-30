using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCoin.Common;


namespace TestCoin.Blockcode
{
    class BlockTester
    {
        List<BlockController> nodes = new List<BlockController>();
        List<String> nodeaddresses = new List<String>();
        Func<String,bool> print;
        Func<String, bool> print2;
        Func<int, bool> runningNodes;
        bool testing = false;
        bool stopped = false;

        public bool logSetting = false;

        List<Pair> hashPairs = new List<Pair>();

        Blockchain mainNode;

        int totalTrans = 0;

        int firstBlockIndex = -1;

        int confirmedTrans = 0;

        int totalBlocks = 0;

        List<DateTime> blockMineTimes = new List<DateTime>();
        List<int> times = new List<int>();

        DateTime? lastMineTime = null;
        int lastMineIndex = 0;

        public String feedback;

        public BlockTester(int nodesAmount, bool throttle, int minePercent,int TPM, Func<String,bool> print, Func<String,bool> print2, Func<int,bool> updateRun, bool nodeStop, Blockchain bc)
        {
            BlockController.FeedbackCallback = print;
            BlockController.FeedbackCallback2 = print2;
            BlockController.stopAll = StopTestingMine;
            BlockController.reportNewBlock = newBlock;
            BlockController.reportNewTran = newTran;
            Block.testerCB = TesterCallback;
            this.mainNode = bc;
            this.print = print;
            this.print2 = print2;
            this.runningNodes = updateRun;
            BlockController.getAddress = getRandomAddress;
            nodes = PopulateNodes(nodesAmount, throttle, minePercent,TPM,nodeStop);
            print2("Nodes Created");
        }

        public List<BlockController> PopulateNodes(int nodesAmount, bool throttle, int minePercent,int TPM, bool mineStop)
        {
            List<BlockController> list = new List<BlockController>();
            BlockController blockC;
            double TPMperNode = TPMCalc(TPM,nodesAmount);

            for(int i = 0; i < nodesAmount; i++)
            {

                blockC = new BlockController(throttle,TPMperNode,MineCalc(minePercent),mineStop);
                nodeaddresses.Add(blockC.publicID);
                list.Add(blockC);
                runningNodes(list.Count); 
            }

            return list;
        }

        public bool MineCalc(int percent)
        {
            Random ran = new Random(DateTime.Now.Millisecond);
            int roll = ran.Next(1, 100);
            if (percent >= roll)
            {
                return true;
            }
            return false;
        }

        public double TPMCalc(int Tpm, int nodeCount)
        {
            double nodeTPM = ((double) Tpm)/((double) nodeCount);

            return nodeTPM;


        }

        public void ReconfigNodes(bool throttle, int minePercent, int TPM, bool nodeStop)
        {
            foreach (BlockController bc in nodes)
            {
                bool mine = MineCalc(minePercent);

                double realTpm = TPMCalc(TPM, nodes.Count);
               
                bc.ableToMine = mine;

                if (!stopped)
                {
                    bc.canMine = mine;
                }
                bc.throttle = throttle;
                bc.TPM = realTpm;
                bc.mineStop = nodeStop;
                
            }
            print2("Reconfigurated nodes");
        }

        public void StartTesting()
        {
            if (!testing)
            {
                testing = true;
                stopped = false;

                foreach (BlockController bc in nodes)
                {
                    bc.Start();
                    
                }


                runningNodes(nodes.Count);
                print2("Testing Started");
            }
        }


        public bool StopTesting()
        {
            if (testing)
            {
                testing = false;
                foreach (BlockController bc in nodes)
                {
                    bc.stopped = true;
                    bc.testChain.stopMining = true;
                }
                stopped = true;
                runningNodes(0);
                print2("Testing Stopped");
            }
            return true;
        }

        public bool StopTestingMine(int nodeInt)
        {
            if (testing)
            {
                stopped = true;
                testing = false;
                foreach (BlockController bc in nodes)
                {
                    bc.stopped = true;
                    bc.testChain.stopMining = true;
                }
                runningNodes(0);
                print2("Testing Stopped via mine, node: " + nodeInt);
            }
            return true;
        }

        public String getRandomAddress(String currAddress)
        {
            if (nodeaddresses.Count > 1)
            {
                Random ran = new Random(DateTime.Now.Millisecond);
                int roll = ran.Next(0, nodeaddresses.Count);
                String addr = nodeaddresses[roll];
                if (addr.Equals(currAddress))
                {
                    return getRandomAddress(currAddress);
                }
                return addr;
            }
            return "error";
        }

        public int ReadLast(int nodeValue, bool read = true)
        {
            try
            {
                
                nodes[nodeValue - 1].testChain.lastBlockCheck();
                Block b = nodes[nodeValue - 1].testChain.RLastBlock;
                if (read)
                {
                    PrintBlock(b, "");
                }
                return b.index;
                //reader.lastThread((20000+nodeValue));

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return -1;
        }

        public void ReadLastAll()
        {
            String text = "Nodes "; 
            for(int i = 1; i <= nodes.Count; i++)
            {
                text += i + ":" + ReadLast(i,false) + ", ";
            }
            print(text);
        }

        public static void SyncFiles()
        {
            String path = Directory.GetCurrentDirectory().ToString() + "\\Storage";

            int workingDirCount = 50;
            for (int i = 1; i < workingDirCount; i++) {
                try
                {
                    File.Copy(path + "\\20000\\Testchain.tcn", path + "\\" + (20000 + i) + "\\Testchain.tcn",true);
                }
                catch(Exception e)
                {

                }

            }
        }

        public bool PrintCb(String intent)
        {
            //print(intent);
            return true;
        }

        public bool PrintBlock(Block b, String intent)
        {
            print(b.simpleRead());
            return true;
        }

        public bool PrintBlocks(List<Block> blocks, String intent)
        {
            print(readBlocks(blocks));
            return true;
        }


        public String readBlocks(List<Block> blocks)
        {
            String blockChainInfo = String.Empty;
            foreach (Block b in blocks)
            {
                blockChainInfo += b.simpleRead();
            }

            return blockChainInfo;
        }

        public void readTranPool(int nodeValue, bool detail)
        {

            try
            {
                String tpoolContents;
                List<Transaction> tp = nodes[nodeValue-1].testChain.pendingTrans;

                

               
                tpoolContents = "Node " + (20000 + nodeValue) + " has " + tp.Count + " trans in pool";

                if (detail)
                {
                    tpoolContents = "\n";
                    int counter = 0;
                    foreach (Transaction t in tp)
                    {
                        counter++;
                        tpoolContents += counter + ".  Hash: " + t.hashAddress.Substring(0, 4) + "\n"; 
                    }
                }

                print(tpoolContents);
            }
            catch (Exception)
            {
                print("Invalid Node");
            }

        }

        public bool newTran()
        {
            totalTrans++;
            return true;
        }

        public bool newBlock(Block b)
        {
            if (b.index > lastMineIndex)
            {
                TesterCallback(b.hashAttempts, b.index,b.transactions.Count);
                lastMineIndex = b.index;
                confirmedTrans += (b.transactions.Count);
                totalTrans++;
                DateTime newTime = DateTime.Now;
                blockMineTimes.Add(newTime);
                if (lastMineTime != null)
                {
                    TimeSpan span = newTime.Subtract((DateTime)lastMineTime);
                    print2("Time to mine block " + b.index + ": " + span.Minutes + " mins " + span.Seconds % 60 + " seconds ");
                    times.Add((int)span.TotalSeconds);
                }
                else
                {
                    firstBlockIndex = b.index;
                }
                lastMineTime = newTime;
            }
            return true;
        }

        public void ShowGraph()
        {
            Analysis graph = new Analysis(times,totalTrans,confirmedTrans);
            graph.Show();
            graph.isLog = logSetting;
            graph.Configure();
        }

        public void ShowGraphBlock(int confirms)
        {
            List<Block> blocks = mainNode.getSlice(firstBlockIndex, mainNode.lastBlockint);
            if (blocks == null)
            {
                return;
            }
            Analysis graph = new Analysis(blocks, confirms);
            graph.Show();
            graph.isLog = logSetting;
            graph.configBlocks(confirms);
        }

        public void ShowGraphHash()
        {
            if (hashPairs.Count > 0)
            {
                Analysis graph = new Analysis(hashPairs);
                graph.Show();
                graph.isLog = logSetting;
                graph.ConfigHash();

            }
        }

        public bool TesterCallback(int hashCount, int blockIndex, int tran = 0)
        {
            int check = CheckInPairs(blockIndex);

            if (check == -1)
            {
                Pair p = new Pair(blockIndex, hashCount, tran);
                hashPairs.Add(p);
            }
            else
            {
                hashPairs[check].add(hashCount);
            }

            return true;
        }

        public int CheckInPairs(int index)
        {
            int c = 0;
            foreach (Pair p in hashPairs)
            {
                if (p.index == index)
                {
                    return c;
                }
                c++;
            }

            return -1;
        }
    }
}
