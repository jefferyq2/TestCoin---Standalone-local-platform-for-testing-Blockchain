using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestCoin
{
    public partial class MiningSettings : Form
    {

        MiningTools.MiningSetup miningSetup;
        bool toggleLock = false;

        public MiningSettings(MiningTools.MiningSetup miningSetup)
        {
            this.miningSetup = miningSetup;
            InitializeComponent();
            ImportMiningSetup();
        }

        private void ImportMiningSetup()
        {
            toggleLock = true;
            switch (miningSetup.pickupState)
            {
                case 1:
                    checkBox2.Checked = true;
                    checkBox2.Enabled = false;
                    break;
                case 2:
                    checkBox3.Checked = true;
                    checkBox3.Enabled = false;
                    break;
                default:
                    checkBox1.Checked = true;
                    checkBox1.Enabled = false;
                    break;
            }
            toggleLock = false;

            textBox1.Text = miningSetup.pickAddress;

            numericUpDown1.Value = (decimal)miningSetup.altruismLevel;

            numericUpDown2.Value = (decimal)miningSetup.maxTransactionsPickup;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!toggleLock)
            {
                toggleLock = !toggleLock;
                checkBox1.Checked = true;
                checkBox2.Checked = false;
                checkBox3.Checked = false;
                checkBox1.Enabled = false;
                checkBox2.Enabled = true;
                checkBox3.Enabled = true;
                toggleLock = !toggleLock;
            }

            
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!toggleLock)
            {
                toggleLock = !toggleLock;
                checkBox3.Checked = true;
                checkBox1.Checked = false;
                checkBox2.Checked = false;
                checkBox3.Enabled = false;
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;
                toggleLock = !toggleLock;
            }


        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!toggleLock)
            {
                toggleLock = !toggleLock;
                checkBox2.Checked = true;
                checkBox1.Checked = false;
                checkBox3.Checked = false;
                checkBox2.Enabled = false;
                checkBox1.Enabled = true;
                checkBox3.Enabled = true;
                toggleLock = !toggleLock;
            }


        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            int state = 0; //default = 0
            if (checkBox1.Checked) { state = 0; } else if (checkBox2.Checked) { state = 1; } else { state = 2; }
            miningSetup.pickupState = state;
            miningSetup.altruismLevel = (double)numericUpDown1.Value;
            miningSetup.maxTransactionsPickup = (double)numericUpDown2.Value;
            miningSetup.pickAddress = textBox1.Text;
            miningSetup.threadsUsed = (int)numericUpDown3.Value;

            Close();

        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
