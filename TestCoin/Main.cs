using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCoin.Blockcode;
using TestCoin.Wallet;
using TestCoin.Connections;
using System.Configuration;
using System.Collections.Specialized;
using System.Threading;

namespace TestCoin
{

    //acts as form and controller
    public partial class Form1 : Form 
    {
        public bool printActive = true;
        Blockchain testChain;
        MiningTools.MiningSetup miningSetup = new MiningTools.MiningSetup();
        ConnectionController conControl;
        Common.Reader reader;

        BlockTester blockTester = null;

        bool AutoSave = bool.Parse(ConfigurationManager.AppSettings.Get("AutoSave"));
        bool AutoLoad = bool.Parse(ConfigurationManager.AppSettings.Get("AutoLoad"));
        bool AutoFill = bool.Parse(ConfigurationManager.AppSettings.Get("AutoFill"));
        bool CreateGen = bool.Parse(ConfigurationManager.AppSettings.Get("CreateGenesis"));

        int port = 20000;
        bool portReady = false;

        bool Difficulty = bool.Parse(ConfigurationManager.AppSettings.Get("DifficultyActive"));

        public Form1()
        {
            InitializeComponent();
            testChain = new Blockchain(AutoLoad,Difficulty);
            conControl = new ConnectionController(ProcessMessage,PortUpdate,AppServerPrint,this,testChain,AutoLoader);
            reader = new Common.Reader(Print, BlockRead, BlockchainRead);
            reader.genCallback = NoFileFound;


            checkBox1.Checked = AutoFill;
            checkBox2.Checked = AutoSave;

            if (AutoLoad)
            {
                 //loads chain from file
            }
            
        }

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


        public bool Print(String message)
        {
            richTextBox1.Invoke(new Action(() => richTextBox1.Text = message)); //thread line of good
            return true; 
        }

        public bool ServerPrint(String message)
        {
            richTextBox2.Invoke(new Action(() => richTextBox2.Text = message)); //bypasses thread restrictions somehow, this is convienient.
            richTextBox2.Invoke(new Action(() => richTextBox2.SelectionStart = richTextBox2.Text.Length));
            richTextBox2.Invoke(new Action(() => richTextBox2.ScrollToCaret()));
            return true;
        }

        public bool AppServerPrint(String message) //appends
        {
            if (printActive)
            {
                message = "\n" + message;
                richTextBox2.Invoke(new Action(() => richTextBox2.Text += message)); //bypasses thread restrictions somehow, this is convienient.
                richTextBox2.Invoke(new Action(() => richTextBox2.SelectionStart = richTextBox2.Text.Length));
                richTextBox2.Invoke(new Action(() => richTextBox2.ScrollToCaret()));
            }
            return true;
        }

        public void publish(String data, bool append = false)
        {
            if (append){
                richTextBox1.Text = richTextBox1.Text + data;
            }
            else
            {
                richTextBox1.Text = data;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            publish(testChain.getTransactionDetails("",true,false,false,true));


        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Equals(String.Empty))
            {
                richTextBox1.Text = "Please enter Wallet ID for balance";
            }
            else if (textBox1.Text.Equals("TestCoin Mine Rewards"))
            {
                publish("ID: " + textBox1.Text + "\n\n");
                publish("A total of " + (testChain.getBalance(textBox1.Text, true)*-1) + " TestCoins have been awarded. (Including Fees)\n\n", true);
                publish(testChain.getTransactionDetails(textBox1.Text), true);
            }
            else
            {
                publish("ID: " + textBox1.Text + "\n\n");
                publish("Balance: " + testChain.getBalance(textBox1.Text,true) + " Availible TestCoins\n",true);
                publish("Balance: " + testChain.getBalance(textBox1.Text) + " TestCoins including pending transaction\n\n", true);
                publish(testChain.getTransactionDetails(textBox1.Text), true);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// Mine to wallet method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Equals(String.Empty)){
                richTextBox1.Text = "Please enter Walled ID to start mining"; 
            }
            else
            {
                String mineDetails;
                testChain.minePendingTrans(textBox2.Text, miningSetup,out mineDetails);
                publish(mineDetails);
            }
        }

        public static void miningComplete(Block block, Blockchain blockchain)
        {
            String mineDetails = blockchain.addMinedBlock(block);
            MiningStatusComplete MSC = new MiningStatusComplete(mineDetails);
            MSC.ShowDialog();    
        }
        

        public static void genBlockMined(Block block, Blockchain genChain)
        {
            genChain.GenBlockMined(new List<Block> { block });
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            String privateKey;
            Wallet.Wallet wallet = new Wallet.Wallet(out privateKey);
            publish("Your Wallet PublicID is " + wallet.publicID + "\n" +
                "Your Private Key is " + privateKey + "\n" +
                "Do not lose your private key, without it you will be unable to make transactions."
                );

            if (checkBox1.Checked)
            {
                textBox1.Text = wallet.publicID;
                textBox2.Text = wallet.publicID;
                textBox4.Text = wallet.publicID;
                textBox5.Text = privateKey;
            }

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (textBox4.Text.Equals(String.Empty) || textBox5.Text.Equals(String.Empty))
            {
                publish("Enter both publicID and private key forms for validation");
            }
            else
            {
                bool isValid = Wallet.Wallet.ValidatePrivateKey(textBox5.Text, textBox4.Text);
                if (isValid)
                {
                    publish("Private key is valid for " + textBox4.Text);
                }
                else
                {
                    publish("Private key is invalid");
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if(textBox4.Text.Equals(String.Empty) || textBox5.Text.Equals(String.Empty) || textBox6.Text.Equals(String.Empty))
            {
                publish("Enter publicID, private key and reciever ID forms for validation");
                return;
            } 
            if (Wallet.Wallet.ValidatePrivateKey(textBox5.Text, textBox4.Text))
            {
                String outputMessage;

                //creates new transaction and adds to pending transactions (if valid)
                bool validTransaction = testChain.CreateUserTransaction(textBox4.Text, textBox5.Text, textBox6.Text, (double)numericUpDown1.Value, (double) numericUpDown2.Value, out outputMessage);

                if (validTransaction)
                {
                    publish("Transaction was successful and has been added to pending transactions \n" + outputMessage);
                }
                else
                {
                    publish("Transaction failed \n" + outputMessage);
                }
            }
            else
            {
                publish("Private key is invalid");
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            List<Block> throwaway;
            if (testChain.isChainValid(out throwaway))
            {
                publish("Blockchain is valid");
            }
            else
            {
                publish("Blockchain is invalid");
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button12_Click(object sender, EventArgs e)
        {
                miningSetup.ShowDialog();
        }

        

        public bool ProcessMessage(String message)
        {
            if (printActive)
            {
                String newMessage = "RECIEVED: " + message;
                AppServerPrint(newMessage);

            }
            return false; 
        }

        public bool PortUpdate(int port)
        {
            String message = "Listening on Port: " + port;
            PortChange(message);
            return true;
        }

        public bool PortChange(String messsage)
        {
            try
            {
                label9.Invoke(new Action(() => label9.Text = messsage)); //bypasses thread restrictions somehow, this is convienient.
            }
            catch(Exception e)
            {

            }
            return true;
        }


        public bool BlockRead(Block block, string intent)
        {
            //do something with recieved block
            Print(block.loginfo());
            return true;
        }

        public bool BlockchainRead(List<Block> blockchain, string intent)
        {
            if (intent.Equals("New Chain"))
            {
                testChain.LoadBlockchain(blockchain);
                Print("Loaded Blockchain from file");
            }
            else if (intent.Contains("Slice:"))
            {
                //test method replace below
                Print(testChain.getChainInfo(blockchain));
            }
            //do something with blockchain
            return true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            testChain.autosave = checkBox2.Checked;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Slicer(textBox7.Text, textBox8.Text);
        }


        private void Slicer(String box7, String box8 )
        {
            try
            {
                int finish;
                int start = Int32.Parse(box7);
                if (Int32.TryParse(box8, out finish))
                {
                    reader.SliceThread(start, finish, "test",port);
                }
                else if (box8.Equals("L"))
                {

                    reader.lastThread(port);
                }
                else
                {
                    reader.readBThread(start,port);
                }

            }
            catch (Exception ex)
            {
                publish("Input invalid");
            }
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

        private void button17_Click(object sender, EventArgs e) //Read All
        {
            Slicer("0", "10000000"); //haha yes
        }

        private void button18_Click(object sender, EventArgs e) //Read Last
        {
            Slicer("0", "L"); //Give the Slicer the L
        }

        private void button19_Click(object sender, EventArgs e)
        {
            testChain.StopMining();
        }


        private void button23_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = "";
        }

        private void button20_Click(object sender, EventArgs e)
        {
            sendMessage("CONNECT");
        }

        private void button24_Click(object sender, EventArgs e)
        {
            string ip = textBox10.Text;
            string port = textBox9.Text;
            conControl.Ping(ip,port);
        }

        private void sendMessage(string message)
        {
            try
            {
                string ip = textBox10.Text;
                int port = Int32.Parse(textBox9.Text);
                conControl.PopMessage(message, port, ip);
            }
            catch (Exception error)
            {
                Print(error.ToString());
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            sendMessage("REQUEST.BLOCK.TOTAL");
        }

        private void button14_Click(object sender, EventArgs e)
        {
            sendMessage("REQUEST.BLOCK.BLOCKNUMBER." + textBox11.Text);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            sendMessage("REQUEST.TRANSACTIONPOOL");
        }

        private void button26_Click(object sender, EventArgs e)
        {
            sendMessage("REQUEST.NODE.POOL");
        }

        private void button27_Click(object sender, EventArgs e)
        {
            ProcessMessage(conControl.conPool.ToString());
        }

        private void button21_Click(object sender, EventArgs e)
        {
            sendMessage(textBox12.Text);
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Do you really want to close program?", "Exit", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                e.Cancel = true;
                conControl.ShutDownGracefully(ShutDown);
            }
            else
            {
                e.Cancel = true;
            }
        }

        public bool ShutDown()
        {
            Environment.Exit(Environment.ExitCode);
            
            return true;
        }

        private void button28_Click(object sender, EventArgs e) //Start Nodes
        {
            if (blockTester != null)
            {
                blockTester.StartTesting();
            }
        }

        private void button2_Click(object sender, EventArgs e) //Create Nodes
        {
            if (blockTester == null)
            {
                int nodes = (int)numericUpDown3.Value;
                bool throttle = checkBox4.Checked;
                int mineP = 0;
                if (checkBox3.Checked)
                {
                    mineP = 100;
                }
                int TPM = (int) numericUpDown4.Value;
                bool mineStop = checkBox5.Checked;

                blockTester = new BlockTester(nodes, throttle, mineP, TPM, PrintTest1, PrintTest2,UpdateRunningNodes,mineStop,testChain);
            }
        }

        private void button29_Click(object sender, EventArgs e) //Stop Nodes
        {
            if (blockTester != null)
            {
                blockTester.StopTesting();
            }
        }

        private void button3_Click(object sender, EventArgs e) //Delete nodes
        {
            if (blockTester != null)
            {
                blockTester.StopTesting();
                PrintTest1("Nodes Deleted");
            }
            blockTester = null;
        }

        public bool PrintTest1(String message)
        {
            richTextBox3.Invoke(new Action(() => richTextBox3.Text += message + "\n")); //thread line of good
            richTextBox3.Invoke(new Action(() => richTextBox3.SelectionStart = richTextBox3.Text.Length));
            richTextBox3.Invoke(new Action(() => richTextBox3.ScrollToCaret()));
            return true;
        }

        public bool PrintTest2(String message)
        {
            richTextBox4.Invoke(new Action(() => richTextBox4.Text += message + "\n")); //thread line of good
            richTextBox4.Invoke(new Action(() => richTextBox4.SelectionStart = richTextBox4.Text.Length));
            richTextBox4.Invoke(new Action(() => richTextBox4.ScrollToCaret()));
            return true;
        }

        public bool UpdateRunningNodes(int num)
        {
            label11.Invoke(new Action(() => label11.Text = "Nodes: " + num));
            return true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            BlockTester.SyncFiles();

        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (blockTester!= null)
            {
                int value = (int)numericUpDown5.Value;
                if (value == 0)
                {
                    blockTester.ReadLastAll();
                }
                else
                {
                    blockTester.ReadLast(value);
                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (blockTester!= null)
            {
                bool throttle = checkBox4.Checked;
                int mineP = 0;
                if (checkBox3.Checked)
                {
                    mineP = 100;
                }
                int TPM = (int)numericUpDown4.Value;
                bool mineStop = checkBox5.Checked;
                blockTester.ReconfigNodes(throttle, mineP, TPM, mineStop);
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (blockTester != null)
            {
                int value = (int)numericUpDown5.Value;
                bool detail = checkBox6.Checked;
                blockTester.readTranPool(value,detail);
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button30_Click(object sender, EventArgs e)
        {
            if (blockTester != null)
            {
                blockTester.ShowGraph();
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            if (blockTester != null)
            {
                int value = (int) numericUpDown6.Value;
                blockTester.ShowGraphBlock(value);
            }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            printActive = checkBox7.Checked;
        }

        private void button32_Click(object sender, EventArgs e)
        {
            if (blockTester != null)
            {
                blockTester.ShowGraphHash();
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (blockTester != null)
            {
                blockTester.logSetting = checkBox8.Checked;
            }
        }

        private void button33_Click(object sender, EventArgs e)
        {
            reader.DeleteAll();

            this.Close();
        }
    }

    }
