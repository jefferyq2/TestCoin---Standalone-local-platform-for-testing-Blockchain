using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using TestCoin.Common;
using TestCoin.Blockcode;
using System.Diagnostics;
using System.Configuration;

namespace TestCoin.Connections
{
    public class ConnectionController
    {
        string serverIP = "127.0.0.1";
        int defaultPort = 20000;
        String localIP;
        String publicIP;
        bool flag = false;
        bool started = false;
        List<Packet> msgQueue = new List<Packet>();
        List<string> respondQueue = new List<string>();
        Func<string, bool> callback;
        Func<int, bool> portCallback;
        MessageReader reader;

        public ConnectionPool conPool = new ConnectionPool();

        private TcpListener server = null;

        private Form1 form;

        private Blockchain chain;

        public bool pinging = false;

        Func<string, bool> Invoke;

        Func<int, bool> autoLoader;


        public ConnectionController()
        {
            GetLocalIP();
            GetPublicIP();
            StartUp();
        }

        public ConnectionController(Func<string,bool> cb, Func<int,bool> pCb, Func<string,bool> invoker, Form1 f, Blockchain bC, Func<int,bool> autoLoader)
        {
            GetLocalIP();
            GetPublicIP();
            callback = cb;
            portCallback = pCb;
            Invoke = invoker;
            form = f;
            chain = bC;
            chain.transactionCallback = NewTransaction;
            chain.newBlockCallback = NewBlockMined;
            reader = new MessageReader(form, chain, TogglePing, conPool);
            this.autoLoader = autoLoader;
            StartUp();
        }


        private void GetLocalIP()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
        }

        private void GetPublicIP()
        {
            publicIP = new WebClient().DownloadString("http://icanhazip.com"); 
        }

        public void StartUp()
        {
            if (!started)
            {
                Thread _responseThread = new Thread(ResponseThread);
                Thread _sendThread = new Thread(SendThread);
                Thread _listener = new Thread(StartListener);
                started = true;
                _responseThread.IsBackground = true;
                _sendThread.IsBackground = true;
                _listener.IsBackground = true;
                _responseThread.Start();
                _sendThread.Start();
                _listener.Start();
            }
        }

        protected void ResponseThread()
        {
            while (true)
            {
                Thread.Sleep(10); //sleep for 10 ms
                if (respondQueue.Count >= 1)
                {
                    callback(respondQueue[0]);
                    if (!respondQueue[0].Equals(String.Empty))
                    {
                        if (!respondQueue[0].Contains("#"))
                        {
                            //do nothing right?
                        }
                        else
                        {
                            ProcessMessage(respondQueue[0], true);
                        }
                    }
                    respondQueue.RemoveAt(0);
                }
            }

            //Common.Common.alertBox(message);
            //while (true)
            //{
                
            //}
        }

        protected void SendThread()
        {

            String messageToSend;
            int port = this.defaultPort;
            while (true)
            {
                
                Thread.Sleep(10); //sleep for 10 ms
                if (msgQueue.Count >= 1)
                {
                    //respondQueue.Add(msgQueue[0]);
                    //SendMessage(msgQueue[0]);
                    messageToSend = reader.PrepareMessage(serverIP,defaultPort,msgQueue[0].content);
                    Connect(msgQueue[0].ip, messageToSend, msgQueue[0].port);

                    //Console.WriteLine("To: " + msgQueue[0].port + "//" + messageToSend);
                    msgQueue.RemoveAt(0);
                }
            }
        }


        private string ProcessMessage(string message, bool queued)
        {
            string response; //Response send to other node
            List<Packet> additionalMessages; //additional commands which may need to be performed
            int senderPort;
            string senderIP;
            reader.Deconstruct(message, serverIP, defaultPort, out response, out additionalMessages, out senderPort, out senderIP);

            if (!response.Equals("") && queued && !response.Contains("INVALID"))
            {
                Packet packet = new Packet(senderIP, senderPort, response);
                msgQueue.Add(packet);
            }

            foreach (Packet pack in additionalMessages)
            {
                msgQueue.Add(pack);
            }

            return response;
        }

        private void SendMessage(String message = "null")
        {
            message = message ?? "null"; //if message is null then text is "null"

            TcpClient client = new TcpClient(serverIP, defaultPort);

            NetworkStream stream = client.GetStream();


            int byteCount = Encoding.ASCII.GetByteCount(message);

            byte[] sendData = new byte[byteCount];

            sendData = Encoding.ASCII.GetBytes(message);

            stream.Write(sendData, 0, sendData.Length);

            stream.Close();
            client.Close();
        }

        public void PopMessage(String message, int port, String ip)
        {
            Packet p = new Packet(ip, port, message);
            msgQueue.Add(p);
        }


        public void PopPackets(List<Packet> packets)
        {
            foreach (Packet p in packets)
            {
                PopPacket(p);
            }
        }

        public void PopPacket(Packet p)
        {
            msgQueue.Add(p);
        }

        public void ShutDownGracefully(Func<bool> func)
        {
            List<Packet> packets = reader.Shutdown(func);
            PopPackets(packets);
        }

        private void StartListener()
        {
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = defaultPort;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();
                Console.WriteLine("Running listener on port: {0}", defaultPort);
                //MessageSendTest();
                //MessageThread();
                ServerRun();

            }
            catch (SocketException e)
            {
                if (defaultPort < 20999 && e.ErrorCode == 10048)
                {
                    defaultPort++;
                    StartListener();
                }
                else
                {
                    Console.WriteLine("SocketException: {0}", e);
                }

            }
        }


        private void ServerRun()
        {
            try
            {
                // Buffer for reading data
                Byte[] bytes = new Byte[64000];
                String data = null;

                portCallback(defaultPort);

                chain.portNum = defaultPort;

                autoLoader(defaultPort);

                bool local = bool.Parse(ConfigurationManager.AppSettings.Get("LocalOnly"));
                if (local && defaultPort == 20000)
                {
                    conPool.vitalNode = true;
                }
                else if (local)
                {
                    Packet connectPacket = new Packet(serverIP, 20000, "CONNECT");
                    msgQueue.Add(connectPacket);
                    Packet chainCheck = new Packet(serverIP, 20000, "REQUEST.BLOCK.BLOCKNUMBER.0"); //request genesis block to see if chain is valid
                    msgQueue.Add(chainCheck);
                }


                // Enter the listening loop.
                while (true)
                {
                    //Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    //Console.WriteLine("Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    try
                    {
                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            //Console.WriteLine("Received: {0}", data);


                            //respondQueue.Add(data);
                            callback(data);

                            //process message here

                            //get response

                            // Process the data sent by the client.
                            //data = data.ToUpper();

                            if (!data.Contains("#"))
                            {
                                Console.WriteLine("hash error");

                                String badResponse = reader.PrepareMessage(serverIP, defaultPort, "INVALID");

                                byte[] badMsg = Encoding.ASCII.GetBytes(badResponse);

                                stream.Write(badMsg, 0, badMsg.Length);

                            }
                            else
                            {

                                String response = data;

                                response = ProcessMessage(data, false);

                                response = reader.PrepareMessage(serverIP, defaultPort, response);

                                byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);

                                // Send back a response.
                                stream.Write(msg, 0, msg.Length);
                                //Console.WriteLine("Sent: {0}", response);
                                Invoke("SENDING: " + response);
                            }

                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("LC: " + defaultPort);
                        //Console.WriteLine("Listener Crash: "+ defaultPort + "  Error:" + e.ToString());
                        //
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }



        public void Connect(String server, String message, int portR)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = portR;
                TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);
                Invoke("SENDING: " + message);

                //Console.WriteLine("Sent: {0}", message);

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[64000];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Console.WriteLine("Received: {0}", responseData);

                respondQueue.Add(responseData);

                // Close everything.
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exit Error: " + e.ToString());
            }

        }

        public void Ping(string ip, string port)
        {

            if (!pinging)
            {
                int portNum;
                if (Int32.TryParse(port,out portNum))
                {
                    String message = "PING";
                    Packet packet = new Packet(ip, portNum, message);
                    Thread _pingThread = new Thread(new ParameterizedThreadStart(DoPing));
                    _pingThread.IsBackground = true;
                    _pingThread.Start(packet);
                }
                else
                {
                    Invoke("Error: port invalid");
                }
                
            }
            else
            {
                Invoke("Error/Waiting for another ping reply...");
            }
        }

        private void DoPing(Object obj)
        {
            int timeout = 5000; //5 Seconds
            msgQueue.Add((Packet)obj);
            Stopwatch timer = Stopwatch.StartNew();
            pinging = true;
            while (pinging && timeout > 0)
            {
                timeout--;
                Thread.Sleep(1);
            }
            timer.Stop();
            if (pinging)
            {
                pinging = false;
                Invoke("Timeout on Ping");
            }
            else
            {
                Invoke("Ping Recieved, Time Taken: " + timer.Elapsed);
            }

        }

        public bool TogglePing()
        {
            pinging = false;
            return true;
        }

        public bool NewTransaction(Transaction tran)
        {
            String message = "UPDATE.TRANSACTION." + tran.getFullDetails();
            Packet p;
            foreach (Connection con in conPool.pool)
            {
                p = new Packet(con.IP, con.port, message);
                PopPacket(p);
            }
            return true;
        }

        public bool NewBlockMined(Block block)
        {
            String message = "UPDATE.NEWBLOCK.BLOCKNUMBER." + block.loginfo();
            Packet p;
            foreach (Connection con in conPool.pool)
            {
                p = new Packet(con.IP, con.port, message);
                PopPacket(p);
            }
            return true;
        }

        public bool checkFork()
        {
            return reader.hasForked;
        }
        
    }



    
}
