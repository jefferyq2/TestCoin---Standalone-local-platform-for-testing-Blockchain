using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCoin.Common
{
    public class Packet
    {
        public string ip = "127.0.0.1";
        public int port = 20000; //default values for ip, port and content
        public string content = "";


        public Packet(string ip, int port, string content)
        {
            this.ip = ip;
            this.port = port;
            this.content = content;
        }

    }
}
