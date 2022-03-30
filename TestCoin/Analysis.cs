using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TestCoin.Blockcode;
using TestCoin.Common;

namespace TestCoin
{
    public partial class Analysis : Form
    {
        int config = 0; // 1=time ; 2=confirm ; 3=hash ;
        List<int> times;
        int min;
        int max;
        double mean;
        int sum;
        int range;
        int blockCount;
        int totalTran;
        int confirmTran;
        double confirmPercent;

        public bool isLog = false;

        int confirmCounter;

        List<Pair> pairs;

        List<Block> blocks;

        public Analysis(List<int> times, int totalTran, int confirmTran)
        {
            InitializeComponent();

            this.times = times;
            this.totalTran = totalTran;
            this.confirmTran = confirmTran;
            config = 1;
            calcStats();
        }

        public Analysis(List<Block> blocks, int confirmInt)
        {
            InitializeComponent();
            this.blocks = blocks;
            this.confirmCounter = confirmInt;
            //configBlocks(confirmInt);
            config = 2;
        }

        public Analysis(List<Pair> p)
        {
            InitializeComponent();
            pairs = p;
            config = 3;
        }

        public void configBlocks(int confirm)
        {
            label1.Text = ("Blocks: " + blocks.Count);
            label2.Text = ("Confirmed count: " + confirm);


            double sum = 0;
            double avg;
            double max = -1;
            double min = -1;

            var chart = chart1.ChartAreas[0];
            chart.AxisX.IntervalType = DateTimeIntervalType.Number;

            //chart.AxisX.LabelStyle.Format = "abc";
            //chart.AxisY.LabelStyle.Format = "Seconds";

            

            chart.AxisX.Title = "Transactions";
            chart.AxisY.Title = "Time for " + confirm +  " confirm (Seconds)";
            if (isLog)
            {
                chart.AxisY.Title = "Logged Time for " + confirm + " confirm (Seconds)";
            }


            chart.AxisX.Minimum = 0;
            //chart.AxisY.Maximum = blockCount;

            chart.AxisY.Minimum = 0;
            //chart.AxisY.Maximum = max;

            chart.AxisX.Interval = 1;

            chart1.Series.Add("Times");
            chart1.Series["Times"].ChartType = SeriesChartType.Spline;
            chart1.Series["Times"].Color = Color.Red;
            chart1.Series[0].IsVisibleInLegend = false;

            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            int counter = 0;
            int tranCount = 0;
            DateTime? tranTime = null;
            DateTime confirmTime;
            TimeSpan diff;
            double wait;
            Block target;

            foreach (Block b in blocks)
            {
                foreach(Transaction t in b.transactions)
                {
                    if (blocks.Count > counter + confirm)
                    {
                        target = blocks[counter + confirm];
                        tranTime = t.timestamp;
                        confirmTime = target.timestamp;
                        diff = confirmTime.Subtract((DateTime)tranTime);
                        wait = diff.TotalSeconds;
                        if (wait < 0)
                        {
                            wait = 0;
                        }
                        if (isLog)
                        {
                            chart1.Series["Times"].Points.AddXY(tranCount, logMax(wait));
                        }
                        else
                        {
                            chart1.Series["Times"].Points.AddXY(tranCount, wait);
                        }
                        sum += wait;
                        tranCount++;
                        if (min == -1)
                        {
                            min = wait;
                            max = wait;
                        }
                        if (wait > max)
                        {
                            max = wait;
                        }
                        if (wait < min)
                        {
                            min = wait;
                        }
                    }
                }
                counter++;
            }

            avg = sum / tranCount;

            label3.Text = ("Max Wait: " + max.ToString("G3") + " seconds");
            label4.Text = ("Min Wait: " + min.ToString("G3") + " seconds");
            label5.Text = ("Mean Wait: " + avg.ToString("G3") + " seconds");
            label6.Text = ("");

            label7.Text = ("");
            label8.Text = ("");
            label9.Text = ("");
        }


        public void calcStats()
        {
            min = -1;
            max = -1;
            sum = 0;
            foreach (int value in times)
            {
                sum += value;
                if (value > max || max == -1)
                {
                    max = value;
                }
                if (value < min || min == -1)
                {
                    min = value;
                }
            }
            mean = (double)sum / (double)times.Count;
            range = max - min;
            blockCount = times.Count;

            confirmPercent = 100 * ((double)confirmTran / (double)totalTran);
        }

        public void Configure()
        {
            label1.Text = ("Max: " + max + " seconds");
            label2.Text = ("Min: " + min + " seconds");
            label3.Text = ("Mean: " + Math.Round(mean, 3) + " seconds");
            label4.Text = ("Range: " + range + " seconds");
            label5.Text = ("Total Time: " + sum + " seconds");
            label6.Text = ("Total Blocks " + blockCount);

            label7.Text = ("Total Trans: " + totalTran);
            label8.Text = ("Confirmed Trans: " + confirmTran);

            label9.Text = (Math.Round(confirmPercent, 3) + "% Confirmed");

            label8.Text = ("");
            label9.Text = ("");

            var chart = chart1.ChartAreas[0];
            chart.AxisX.IntervalType = DateTimeIntervalType.Number;

            //chart.AxisX.LabelStyle.Format = "abc";
            //chart.AxisY.LabelStyle.Format = "Seconds";

            chart.AxisX.Title = "Blocks";
            chart.AxisY.Title = "Time (Seconds)";
            if (isLog)
            {
                chart.AxisY.Title = "Logged Time (Seconds)";
            }


            chart.AxisX.Minimum = 1;
            chart.AxisY.Maximum = blockCount;

            chart.AxisY.Minimum = 0;
            chart.AxisY.Maximum = max;

            chart.AxisX.Interval = 1;

            chart1.Series.Add("Times");
            chart1.Series["Times"].ChartType = SeriesChartType.Spline;
            chart1.Series["Times"].Color = Color.Red;
            chart1.Series[0].IsVisibleInLegend = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            int counter = 1;

            foreach (int value in times)
            {
                if (isLog)
                {
                    chart1.Series["Times"].Points.AddXY(counter, logMax(value));
                }
                else
                {
                    chart1.Series["Times"].Points.AddXY(counter, value);
                }
                counter++;
            }
        }

        public void ConfigHash()
        {
            

            var chart = chart1.ChartAreas[0];
            chart.AxisX.IntervalType = DateTimeIntervalType.Number;

            //chart.AxisX.LabelStyle.Format = "abc";
            //chart.AxisY.LabelStyle.Format = "Seconds";

            chart.AxisX.Title = "Blocks";
            chart.AxisY.Title = "Hashes";
            if (isLog)
            {
                chart.AxisY.Title = "Logged Hashes";
            }


            chart.AxisX.Minimum = 0;
            //chart.AxisY.Maximum = blockCount;

            chart.AxisY.Minimum = 0;
            //chart.AxisY.Maximum = max;

            chart.AxisX.Interval = 1;

            chart1.Series.Add("Hashes");
            chart1.Series["Hashes"].ChartType = SeriesChartType.Spline;
            chart1.Series["Hashes"].Color = Color.Red;
            chart1.Series[0].IsVisibleInLegend = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            int counter = 0;

            int totalT = 0;
            int maxTran = 15;

            double meanWTran;
            double meanBlock;
            int min = -1;
            int max = -1;

            double meanTran15;

            double totalHashCount = 0;

            foreach (Pair p in pairs)
            {
                if (isLog)
                {
                    chart1.Series["Hashes"].Points.AddXY(counter, logMax(p.hashCount));
                }
                else
                {
                    chart1.Series["Hashes"].Points.AddXY(counter, p.hashCount);
                }               
                totalHashCount += p.hashCount;
                totalT += p.tranCount;
                counter++;
                if (min == -1)
                {
                    max = p.hashCount;
                    min = p.hashCount;
                }
                if (p.hashCount > max)
                {
                    max = p.hashCount;
                }
                if (p.hashCount < min)
                {
                    min = p.hashCount;
                }
            }

            meanBlock = totalHashCount /(double) counter;

            meanWTran = totalHashCount / (double)totalT;

            meanTran15 = totalHashCount /(double)((double)maxTran*counter);


            label1.Text = ("Max Hashes: " + max);
            label2.Text = ("Min Hashes: " + min);
            label3.Text = ("Mean Hashes/Block: " + Math.Round(meanBlock, 1));
            label4.Text = ("Mean Hashes/Tran: " + Math.Round(meanWTran,1));
            label5.Text = ("Mean Hashes/MaxTrans : " + Math.Round(meanTran15,1));
            label6.Text = ("Total Blocks: " + counter);

            label7.Text = ("Total Trans: " + totalT);
            label8.Text = ("");
            label9.Text = ("");
        }

        public double logMax(double number)
        {
            return Math.Max(0, Math.Log(number));
        }

    }
}
