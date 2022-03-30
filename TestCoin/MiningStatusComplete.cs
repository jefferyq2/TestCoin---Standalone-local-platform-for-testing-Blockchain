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
    public partial class MiningStatusComplete : Form
    {
        public MiningStatusComplete(String miningDetails)
        {
            InitializeComponent();
            publishDetails(miningDetails);
        }

        public void publishDetails(String details)
        {
            richTextBox1.Text = details;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
