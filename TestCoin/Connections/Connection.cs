using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCoin.Common;

namespace TestCoin.Connections
{
    public class Connection
    {
        public string IP;
        public int port;
        public int bCount = 0; //bad count


        public Connection()
        {
            IP = "127.0.0.1";
            port = 20000;
        }

        public Connection(string IP, int port)
        {
            this.IP = IP;
            this.port = port;
        }

        public bool Equals(string IP, int port)
        {
            return (this.IP == IP && this.port == port);
        }


        public static Connection Parse(string text)
        {
            Connection con = new Connection();
            

            try
            {
                String[] splitCon = Common.Common.splitAt(text, ",");
                con.IP = splitCon[0];
                con.port = Int32.Parse(splitCon[1]);
            }
            catch (Exception e)
            {

            }

            return con;

        }

    }
}
