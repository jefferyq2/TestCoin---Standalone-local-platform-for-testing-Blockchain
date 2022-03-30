using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCoin.Blockcode;
using TestCoin.Common;
using System.Threading;

namespace TestCoin.Connections
{
    class MessageReader
    {
        int senderPort;
        string senderIP;
        String ip;
        //String content; // do not make this global !!!!!!!!!!!!!!!
        Reader reader;

        Form1 form;

        Blockchain chain;

        bool shuttingDown = false;
        int shutDownCount = 0;
        int shutDownCap = 0;
        Func<bool> ShutDown;

        List<Block> chainStore = null;

        Block blockStore = null;

        Func<bool> Pinger;

        ConnectionPool conPool;

        int invalidHit = 0;
        

        private string thisIP = "127.0.0.01";
        private int thisPort = 20000;

        private List<Block> fixChain = new List<Block>(); //If the chain has accidently forked, this variable is used to put it back on main chain
        public bool hasForked = false; //will only accept fork fix messages 
        private Connection forkCon; //only connection that node will communicate with when fork fixing
        private int workingIndex;



        public MessageReader(Form1 f, Blockchain chain, Func<bool> Pinger, ConnectionPool conPool)
        {
            this.form = f;
            this.chain = chain;
            this.Pinger = Pinger;
            this.conPool = conPool;
            this.reader = new Reader(PrintCallBack, BlockCallBack, ChainCallBack);
        }

        public void ReadMessage(String message, out int port, out String ip, out String msgContent)
        {
            String temp = message;
            String[] list = message.Split('|');
            try
            {
                ip = list[0].Split('#')[1];
                port = Int32.Parse(list[1].Split('#')[1]);
                msgContent = list[2].Split('#')[1];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                ip = "";
                port = -1;
                msgContent = null;
            }
        }

        public String PrepareMessage(String senderIP, int senderPort, String message)
        {
            String newmessage = "IP#" + senderIP + "|Port#" + senderPort + "|Msg#" + message + "|";

            return newmessage;
        }

        public void Deconstruct(string message, string thisIP, int thisPort, out string response, out List<Packet> messages, out int port, out string IP)
        {
            String content;
            messages = new List<Packet>();
            ReadMessage(message, out senderPort, out ip, out content); //breaks down message into parts
            
            port = senderPort;
            IP = ip;
            this.senderPort = port;
            this.senderIP = ip;

            this.thisIP = thisIP;
            this.thisPort = thisPort;



            conPool.RefreshCon();

            if (port == -1)
            {
                port = thisPort;
                IP = thisIP;
                INVALID(out response);
                return;
            }

            InvalidCheck(message);

            if (content.Contains("PING"))
            {
                PING(content, out response, out messages);
            }
            else if (content.Contains("FORK"))
            {
                FORK(content, out response, out messages);
            }
            else if (content.Contains("UPDATE"))
            {
                UPDATE(content, out response, out messages);
            }
            else if (content.Contains("SHUTDOWN"))
            {
                SHUTDOWN(content, out response, out messages);
            }
            else if (content.Contains("REQUEST"))
            {
                REQUEST(content, out response, out messages);
            }
            else if (content.Contains("RESPONSE"))
            {
                RESPONSE(content, out response, out messages);
            }
            else if (content.Contains("CONNECT"))
            {
                CONNECT(content, out response, out messages);
            }
            else
            {
                INVALID(out response);
            }
        }

        private void PING(String content, out string response, out List<Packet> messages)
        {
            response = "";
            messages = new List<Packet>();
            if (content.Equals("PING"))
            {
                
                response = "PING.RESPONSE";
            }
            else if (content.Contains("RESPONSE"))
            {
                Pinger(); //toggles ping
                //stop ping counter
            }
            else
            {
                INVALID(out response);
            }
        }

        private void REQUEST(String content, out string response, out List<Packet> messages)
        {
            response = "";
            messages = new List<Packet>();
            if (content.Contains("BLOCK"))
            {
                if (content.Contains("TOTAL"))
                {
                    Block lastBlock = GetLastBlock();
                    response = "RESPONSE.BLOCK.TOTAL." + lastBlock.index;
                }
                else if (content.Contains("BLOCKNUMBER"))
                {
                    try
                    {
                        int blocknum = Int32.Parse(Common.Common.splitAt(content, ".")[3]);
                        int lastIndex = GetLastBlock().index;
                        if (lastIndex >= blocknum)
                        {

                            Block block = GetBlocksAt(blocknum, blocknum)[0];
                            response = "RESPONSE.BLOCK.RESPONSE." + blocknum + "." + block.loginfo();
                        }
                        else
                        {
                            response = "RESPONSE.BLOCK.INVALID." + blocknum;
                        }


                    }
                    catch (Exception e)
                    {
                        INVALID(out response);
                    }
                    
                }
                else
                {
                    INVALID(out response);
                }
            }
            else if (content.Contains("TRANSACTIONPOOL"))
            {
                response = "RESPONSE.TRANSACTIONPOOL.";
                response += TransactionPool.PoolToString(chain.pendingTrans);
            }
            else if (content.Contains("NODE.POOL"))
            {
                response = "RESPONSE.NODE.POOL.";
                if (conPool.vitalNode)
                {
                    response += conPool.VitalNodePick();
                }
                else
                {
                    response += conPool.ToString();
                }
            }
            else
            {
                INVALID(out response);
            }

        }

        private void RESPONSE(String content, out string response, out List<Packet> messages)
        {
            response = "";
            messages = new List<Packet>();
            if (content.Contains("NODE"))
            {
                if (content.Contains("TOTAL"))
                {
                    int total;
                    string val = Common.Common.splitAt(content, "TOTAL.")[1];
                    if (Int32.TryParse(val, out total))
                    {
                        //do method(total)
                    }
                    else
                    {
                        INVALID(out response);
                    }
                }
                else if (content.Contains("POOL"))
                {
                    //POOL.{IP,PORT|IP,PORT} --wanna grab the ip ports inside
                    List<Connection> conList = ConnectionPool.ToConnections(content);
                    if (!conPool.Full())
                    {
                        if (conList.Count >= ConnectionPool.connectionLimit / 3)
                        {
                            conList = ConnectionPool.RandomSample(conList, ConnectionPool.connectionLimit / 3);
                        }
                        int counter = 0;
                        foreach (Connection con in conList)
                        {
                            if (!conPool.Contains(con) && !con.Equals(thisIP,thisPort))
                            {
                                if (!conPool.Full(counter))
                                {
                                    Packet p = new Packet(con.IP, con.port, "CONNECT");
                                    messages.Add(p);
                                    counter++;
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                    }


                }
                else
                {
                    INVALID(out response);
                }
            }
            else if (content.Contains("BLOCK"))
            {
                if (content.Contains("TOTAL"))
                {
                    //TOTAL.number --get that number
                    //normally used to enquire size of blockchain and pick up blocks
                    int total;
                    string val = Common.Common.splitAt(content, "TOTAL.")[1];
                    if (Int32.TryParse(val, out total))
                    {
                        
                        int lastLocalIndex = GetLastBlock().index;
                        if (lastLocalIndex < total)
                        {
                            //set off method(total) that should request remaining blocks that chain does not have
                            //pick random node in con pool to send message to

                            Connection con = conPool.pickRandom();
                            string message = "REQUEST.BLOCK.BLOCKNUMBER." + (lastLocalIndex + 1);

                            Packet p = new Packet(con.IP, con.port, message);
                            messages.Add(p);
                                 
                        }
                    }
                    else
                    {
                        INVALID(out response);
                    }
                }
                else if (content.Contains("BLOCK.RESPONSE"))
                {
                    //RESPONSE.x.blockcontents --wanna grab that contents
                    string val = Common.Common.splitAt(content, ".RESPONSE.")[1];
                    string[] info = Common.Common.splitAt(val, ".Block Index:");
                    info[1] = "Block Index:" + info[1];
                    int blocknum;
                    try
                    {
                        blocknum = Int32.Parse(info[0]);
                        //Console.WriteLine("block.response");
                        Block block = Reader.ParseBlock(info[1]);
                        if (block.index == 0)
                        {
                            try
                            {
                                Block firstBlock = GetBlocksAt(0, 0)[0];
                                if (Block.Compare(block, firstBlock,"first"))
                                {
                                    Packet p = new Packet(senderIP,senderPort, "REQUEST.BLOCK.TOTAL");
                                    messages.Add(p);
                                }
                                else
                                {
                                    if (block.StrongValidation() && HashTools.verifyMerkleRoot(block.merkleRoot, block) && block.validateTransactions())
                                    {
                                        chain.SaveChainInfo(new List<Block>() { block });
                                        chain.updateLastNum(block);
                                        Packet p = new Packet(senderIP, senderPort, "REQUEST.BLOCK.TOTAL");
                                        messages.Add(p);

                                    }
                                }

                            }
                            catch(Exception e)//means no data
                            {
                                if (block.StrongValidation() && HashTools.verifyMerkleRoot(block.merkleRoot,block) && block.validateTransactions())
                                {
                                    chain.SaveChainInfo(new List<Block>() { block });
                                    Packet p = new Packet(senderIP, senderPort, "REQUEST.BLOCK.TOTAL");
                                    messages.Add(p);
                                }
                            }
                        }
                        else
                        {
                            Block lastBlock = GetLastBlock();
                            if (block.index == lastBlock.index + 1)
                            {
                                List<Block> tempChain;
                                if (chain.isChainValid(out tempChain, block))
                                {
                                    chain.SortTranPool(block);
                                    chain.AppendSaveChainInfo(block); //if valid with chain, add to chain
                                    Packet p = new Packet(senderIP, senderPort, "REQUEST.BLOCK.TOTAL");
                                    messages.Add(p);
                                }
                            }
                        }

                        //add block to chain with reference to given position
                    }
                    catch(Exception e)
                    {
                        INVALID(out response);
                    }

                }
                else if (content.Contains("INVALID"))
                {
                    if (invalidHit > 5)
                    {
                        invalidHit = 0;

                        if (invalidHit == 1) {
                            Connection con = conPool.pickRandom();
                            string message = "REQUEST.BLOCK.TOTAL";

                            Packet p = new Packet(con.IP, con.port, message);
                            messages.Add(p);

                        }
                    }

                    else {
                        string val = Common.Common.splitAt(content, "INVALID.")[1];
                        try
                        {
                            int blocknum = Int32.Parse(val);
                            string message = "REQUEST.BLOCK.BLOCKNUMBER." + blocknum;
                            conPool.badConnections.Add(new Connection(senderIP, senderPort));
                            Connection newCon = conPool.pickRandom();
                            if (newCon != null)
                            {
                                Packet p = new Packet(newCon.IP, newCon.port, message);
                                messages.Add(p);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            INVALID(out response);
                        }
                    }
                }
                else
                {
                    INVALID(out response);
                }
            }
            else if (content.Contains("TRANSACTIONPOOL"))
            {
                //parse contents add to transaction pool.
                List<Transaction> tranList = TransactionPool.StringToPool(content);
                foreach(Transaction tran in tranList)
                {
                    chain.AddToPending(tran);
                }
            }
            else
            {
                INVALID(out response);
            }
        }


        private void SHUTDOWN(String content, out string response, out List<Packet> messages)
        {
            response = "";
            messages = new List<Packet>();
            if (content.Contains("NODE"))
            {
                if (content.Contains("POOL"))
                {
                    string text = Common.Common.splitAt(content, "RESPONSE.")[1];
                    List<Connection> conList = ConnectionPool.ToConnections(text);
                    //depending on setup of nodes may attempt to connect to connections sent.

                    try
                    {
                        Connection con = new Connection(ip, senderPort);

                        if (conPool.Contains(con))
                        {
                            conPool.Remove(con);
                        }

                        response = "SHUTDOWN.ACK";

                        if (!conPool.vitalNode)
                        {
                            if (!conPool.Full())
                            {
                                if (conList.Count >= ConnectionPool.connectionLimit/3)
                                {
                                    conList = ConnectionPool.RandomSample(conList, ConnectionPool.connectionLimit /3);
                                }
                                
                                int counter = 0;
                                foreach (Connection cons in conList)
                                {
                                    if (!conPool.Contains(cons) && !cons.Equals(thisIP, thisPort))
                                    {
                                        if (!conPool.Full(counter))
                                        {
                                            Packet p = new Packet(cons.IP, cons.port, "CONNECT");
                                            messages.Add(p);
                                            counter++;
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        INVALID(out response);
                    }
                }
                
                
            }
            else if (content.Contains("ACK"))
            {
                if (shuttingDown)
                {
                    shutDownCount++;
                    if (shutDownCap == shutDownCount)
                    {
                        ShutDown();
                        //shutdown callback
                    }
                }
            }
            else
            {
                INVALID(out response);
            }
        }

        private void UPDATE(String content, out string response, out List<Packet> messages)
        {
            response = "";
            messages = new List<Packet>();
            String temp = content;
            if (content.Contains("NEWBLOCK"))
            {
                if (content.Contains("BLOCKNUMBER"))
                {
                    //includes a blockcontents worth grabbing
                    //Console.WriteLine("updatenewblock");
                    string text = Common.Common.splitAt(content, "BLOCKNUMBER.")[1];
                    Block block = Reader.ParseBlock(text);

                    //add block procedure
                    //first check if block itself is valid
                    if (block.StrongValidation())
                    {
                        int lastBlockIndex = GetLastBlock().index;
                        //check if block is new if so propagate message and append
                        if (block.index == lastBlockIndex + 1)
                        {
                            List<Block> tempChain;
                            if (chain.isChainValid(out tempChain, block))
                            {
                                chain.stopMining = true;
                                chain.SortTranPool(block);
                                chain.AppendSaveChainInfo(block); //if valid with chain, add to chain
                                chain.updateLastNum(block);
                                string message = "UPDATE.NEWBLOCK.BLOCKNUMBER." + block.loginfo();
                                Connection sender = new Connection(senderIP, senderPort);
                                foreach (Connection con in conPool.pool)
                                {
                                    if (!sender.Equals(con.IP, con.port))
                                    {
                                        Packet p = new Packet(con.IP, con.port, message);
                                        messages.Add(p);
                                    }
                                }
                            }
                            else if (!GetLastBlock().prevHash.Equals(block.prevHash))
                            {
                                if (lastBlockIndex < block.index)
                                {
                                    fixChain = new List<Block>();
                                    fixChain.Add(block);
                                    forkCon = new Connection(senderIP, senderPort);
                                    workingIndex = block.index;
                                    hasForked = true;
                                    Packet p = new Packet(senderIP, senderPort, "FORK.REQUEST." + (workingIndex - 1));
                                }
                            }
                        }
                        else if (!GetLastBlock().prevHash.Equals(block.prevHash))
                        {
                            if (lastBlockIndex < block.index)
                            {
                                fixChain = new List<Block>();
                                fixChain.Add(block);
                                forkCon = new Connection(senderIP, senderPort);
                                workingIndex = block.index;
                                hasForked = true;
                                Packet p = new Packet(senderIP, senderPort, "FORK.REQUEST." + (workingIndex - 1));
                                messages.Add(p);
                            }
                        }
                        else
                        {
                            try
                            {
                                /*
                                if (Block.Compare(block, GetBlocksAt(block.index, block.index,)[0]))
                                { //if not new check if block already exists in chain (then just ack)

                                }
                                */
                            }
                            catch (Exception e)
                            {

                            }
                        }



                        //if prev hash matches last block block in that index exists do nothing (ack)

                        //if prev hash doesnt match last block request last block from that node 
                    }



                    response = "UPDATE.NEWBLOCK.ACK"; //regardless of validity of block should still send ack
                
                }
                else if (content.Contains("ACK"))
                {

                }
                else
                {
                    INVALID(out response);
                }
            }
            else if (content.Contains("TRANSACTION"))
            {
                //get the transactioncontents
                if (content.Contains("ACK"))
                {

                }
                else
                {
                    string text = Common.Common.splitAt(content, "TRANSACTION.")[1];
                    Transaction tran = Transaction.Parse(text);
                    bool success = chain.AddToPending(tran);

                    response = "UPDATE.TRANSACTION.ACK";
                    if (success)
                    {
                        string message = "UPDATE.TRANSACTION." + tran.getFullDetails();
                        Connection sender = new Connection(senderIP, senderPort);
                        foreach (Connection con in conPool.pool)
                        {
                            if (!sender.Equals(con.IP, con.port))
                            {
                                Packet p = new Packet(con.IP, con.port, message);
                                messages.Add(p);
                            }
                        }
                    }

                }
                
            }
            else
            {
                INVALID(out response);
            }
        }


        private void CONNECT(String content, out string response, out List<Packet> messages)
        {

            response = "";
            messages = new List<Packet>();
            if (content.Equals("CONNECT"))
            {
                if (conPool.TryAddConnection(ip, senderPort))
                {
                    response = "CONNECT.ACK";
                }
                else
                {
                    response = "CONNECT.FULL";
                }
            }
            else if (content.Contains("ACK"))
            {
                conPool.TryAddConnection(ip, senderPort);
                if (senderPort == 20000)
                {
                    Packet p = new Packet(senderIP, senderPort, "REQUEST.NODE.POOL");
                    messages.Add(p);
                }
                //add node ip to connection list
            }
            else if (content.Contains("FULL"))
            {
                //cannot add :(
            }
            else
            {
                INVALID(out response);
            }

        }

        private void FORK(String content, out string response, out List<Packet> messages)
        {
            response = "";
            messages = new List<Packet>();
            if (content.Contains("REQUEST"))
            {
                try
                {
                    int blocknum = Int32.Parse(Common.Common.splitAt(content, "REQUEST.")[1]);
                    int lastIndex = GetLastBlock().index;
                    if (lastIndex >= blocknum)
                    {

                        Block block = GetBlocksAt(blocknum, blocknum)[0];

                        if (block != null)
                        {
                            response = "FORK.RESPONSE." + blocknum + "." + block.loginfo();
                        }
                        else
                        {
                            INVALID(out response);
                        }
                    }
                    else
                    {
                        INVALID(out response);
                    }


                }
                catch (Exception e)
                {
                    INVALID(out response);
                }
            }
            else if (content.Contains("RESPONSE") && hasForked && forkCon.Equals(senderIP,senderPort))
            {
                string val = Common.Common.splitAt(content, ".RESPONSE.")[1];
                string[] info = Common.Common.splitAt(val, ".Block Index:");
                info[1] = "Block Index:" + info[1];
                int blocknum;

                try
                {
                    //Console.WriteLine("fork");
                    blocknum = Int32.Parse(info[0]);
                    Block block = Reader.ParseBlock(info[1]);
                    int lastLocalIndex = GetLastBlock().index;
                    if (block.index == workingIndex - 1 && block.StrongValidation())
                    {
                        fixChain.Insert(0, block);
                        if (!(lastLocalIndex <= workingIndex - 2))
                        {
                            Block lastBlock = GetBlocksAt(workingIndex - 2, workingIndex - 2)[0];

                            if (block.prevHash.Equals(lastBlock.hash))
                            {
                                hasForked = false;
                                List<Block> newChain = GetBlocksAt(0, workingIndex - 2);
                                newChain.AddRange(fixChain);
                                chain.updateLastNum(newChain.Last());
                                chain.SaveChainInfo(newChain); //long save procedure but as it's an overwrite there is no way around


                            }
                            else
                            {
                                workingIndex--;
                                Packet p = new Packet(senderIP, senderPort, "FORK.REQUEST." + (workingIndex - 1));
                                messages.Add(p);
                            }
                        }
                        else
                        {
                            workingIndex--;
                            Packet p = new Packet(senderIP, senderPort, "FORK.REQUEST."+(workingIndex-1));
                            messages.Add(p);
                        }
                    }
                        
                        
                    

                    //add block to chain with reference to given position
                }
                catch (Exception e)
                {
                    INVALID(out response);
                }
            }
            else if (content.Contains("ACK"))
            {
                response = "";
            }

        }

        private void INVALID(out string response)
        {
            response = "INVALID";
            //message invalid, do something about it
        }


        public bool PrintCallBack(string msg)
        {
            //Console.WriteLine(msg); //Might as well leave out
            return true;
        }

        public bool BlockCallBack(Block block, string intent)
        {
            blockStore = block;
            return true;
        }

        public bool ChainCallBack(List<Block> chain, string intent)
        {
            chainStore = chain;
            return true;
        }


        public Block GetLastBlock()
        {
            chain.lastBlockCheck();
            return chain.RLastBlock;


            /*
            reader.lastThread(thisPort);

            while (blockStore == null)
            {
                Thread.Sleep(10);
            }

            Block lastBlock = blockStore;

            blockStore = null;
            

            return lastBlock;
            */
        }


        public List<Block> GetBlocksAt(int start, int fin)
        {
            reader.SliceThread(start, fin, "Slice",thisPort);

            int count = 0;

            while (chainStore == null)
            {
                Thread.Sleep(10);
                count += 10;
                if (count >= 1000)
                {
                    throw new Exception();
                }
            }

            List<Block> blocks = chainStore;

            chainStore = null;

            return blocks;
        }

        public List<Packet> Shutdown(Func<bool> func)
        {
            ShutDown = func;
            List<Packet> messages = new List<Packet>();
            shuttingDown = true;
            if (conPool.pool.Count == 0)
            {
                func();
                return messages;
            }
            String messageContent = "SHUTDOWN.NODE.POOL.RESPONSE."+conPool.ToString();
            Packet p;
            int counter = 0;
            foreach (Connection con in conPool.pool)
            {
                p = new Packet(con.IP, con.port, messageContent);
                messages.Add(p);
                counter++;
            }

            shutDownCap = counter;

            Thread _timeoutThread = new Thread(TimeOut);
            _timeoutThread.IsBackground = true;
            _timeoutThread.Start();

            return messages;
        }

        public void TimeOut()
        {
            Thread.Sleep(3000);
            ShutDown();
        }
        
        public void InvalidCheck(string message)
        {
            if (message.Contains(".INVALID"))
            {
                invalidHit++;
            }
            else
            {
                if (invalidHit > 0)
                {
                    invalidHit--;
                }
            }
        }


    }
}
