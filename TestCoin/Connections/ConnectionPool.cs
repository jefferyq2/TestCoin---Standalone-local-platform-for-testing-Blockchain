using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCoin.Common;

namespace TestCoin.Connections
{
    public class ConnectionPool
    {
        public List<Connection> pool = new List<Connection>();
        public static int connectionLimit = 6;
        public bool vitalNode = false; //decided in config

        public List<Connection> badConnections = new List<Connection>(); //bad connections are nodes which are not up to date


        public ConnectionPool()
        {
            vitalNode = bool.Parse(ConfigurationManager.AppSettings.Get("VitalNode"));
            
        }

        public bool Full(int i = 0)
        {
            return (connectionLimit <= pool.Count + i);
            
        }

        public Connection pickRandom()
        {
            Connection con = null;
            if (pool.Count == 0)
            {
                return null;
            }
            int attempts = 0;
            while (con == null || (Contains(con, badConnections) && attempts < 100)  ){
                Random ran = new Random();
                int r = ran.Next(pool.Count);
                con = pool[r];
                attempts++;
            }
            return con;
        }

        public void RefreshCon() //refreshes bad connections
        {
            List<int> indexRemove = new List<int>();
            for (int i = 0; i < badConnections.Count; i++)
            {
                badConnections[i].bCount++;
                if (badConnections[i].bCount >= 5)
                {
                    indexRemove.Add(i);
                }
            }

            if (indexRemove.Count >= 1)
            {
                for(int i = indexRemove.Count-1; i >= 0; i--)
                {
                    badConnections.RemoveAt(indexRemove[i]);
                }
            }
        }
        

        public bool TryAddConnection(string IP, int port)
        {
            if (!Contains(IP, port))
            {
                if (connectionLimit > pool.Count || vitalNode)
                {
                    AddConnection(IP, port);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private void AddConnection(string IP, int port)
        {
            Connection con = new Connection(IP, port);
            pool.Add(con);
        }


        public bool Contains(Connection con)
        {
            foreach (Connection c in pool)
            {
                if (c.IP.Equals(con.IP) && c.port.Equals(con.port))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains(Connection con, List<Connection> pool)
        {
            foreach (Connection c in pool)
            {
                if (c.IP.Equals(con.IP) && c.port.Equals(con.port))
                {
                    return true;
                }
            }

            return false;
        }


        public bool Contains(string IP, int port)
        {
            foreach (Connection c in pool)
            {
                if (c.IP.Equals(IP) && c.port.Equals(port))
                {
                    return true;
                }
            }

            return false;
        }


        public void Remove(Connection con)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].IP.Equals(con.IP) && pool[i].port.Equals(con.port))
                {
                    pool.RemoveAt(i);
                }
            }
        }

        public static List<Connection> ToConnections(string connectionList)
        {
            List<Connection> cons = new List<Connection>();

            //string format should be something.something.{IP,port;IP,port;....;}

            try
            {
                string connectionStrings = connectionList.Substring(connectionList.LastIndexOf('{'));
                connectionStrings = connectionStrings.Substring(1, connectionStrings.Length - 1);

                string[] connections = Common.Common.splitAt(connectionStrings, ";");
                connections = connections.Take(connections.Count() - 1).ToArray(); //remove empty index

                foreach (string str in connections)
                {
                    cons.Add(Connection.Parse(str));
                }
            }
            catch(Exception e)
            {
                //:(
            }

            return cons;
        }


        public override String ToString()
        {
            String conStr = "{";

            foreach (Connection con in pool)
            {
                conStr += con.IP;
                conStr += ",";
                conStr += con.port;
                conStr += ";";
            }

            conStr += "}";


            return conStr;
        }

        public String VitalNodePick()
        {
            //random alg to pick from connnection list
            int totalNodes = pool.Count;
            if (totalNodes < connectionLimit/4)
            {
                return ToString(pool);
            }
            else
            {
                List<Connection> randomPool = new List<Connection>();
                float amountToPick = connectionLimit/4;
                Random ran = new Random();
                int counter = 0;
                float chance;
                float roll;
                foreach (Connection con in pool)
                {
                    chance = amountToPick / (pool.Count - counter);
                    roll = ran.Next(10000) / 10000f;
                    if (roll < chance)
                    {
                        amountToPick--;
                        randomPool.Add(con);
                    }
                    counter++;

                }
                return ToString(randomPool);

            }
        }

        public static List<Connection> RandomSample(List<Connection> list, int amountToPick)
        {
            //random alg to pick from connnection list
            if (amountToPick >= list.Count)
            {
                return list;
            }

            else
            {
                List<Connection> randomPool = new List<Connection>();
                Random ran = new Random();
                int counter = 0;
                float chance;
                float roll;
                foreach (Connection con in list)
                {
                    chance = amountToPick / (list.Count - counter);
                    roll = ran.Next(10000) / 10000f;
                    if (roll < chance)
                    {
                        amountToPick--;
                        randomPool.Add(con);
                    }
                    counter++;

                }
                return randomPool;

            }
        }

        public static String ToString(List<Connection> list)
        {
            String conStr = "{";

            foreach (Connection con in list)
            {
                conStr += con.IP;
                conStr += ",";
                conStr += con.port;
                conStr += ";";
            }

            conStr += "}";


            return conStr;
        }
    }
}
