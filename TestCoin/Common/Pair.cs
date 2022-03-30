using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCoin.Common
{
    public class Pair
    {
        public int index;
        public int hashCount = 0;
        public int tranCount;

        public Pair(int index, int count, int tc = 0)
        {
            this.index = index;
            this.hashCount = count;
            if (tc > 0)
            {
                this.tranCount = tc;
            }
        }

        public void add(int count)
        {
            hashCount += count;
        }


    }
}
