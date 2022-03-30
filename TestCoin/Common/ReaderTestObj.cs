using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestCoin.Common
{
    public class ReaderTestObj
    {
        public int node;
        private bool _inUse;
        public String activeKey = "";
        public int timeout = 0;
        public bool inUse
        {
            get { return _inUse; }
            set
            {
                _inUse = value;

            }
        }

        public List<String> queue = new List<String>();

        public ReaderTestObj(int value)
        {
            node = 20000+value;
            _inUse = false;
        }

        


        public static List<ReaderTestObj> createList()
        {
            List<ReaderTestObj> list = new List<ReaderTestObj>();

            for (int i = 1; i <= 100; i++)
            {
                list.Add(new ReaderTestObj(i));
            }

            return list;
        }

        public static String generateHash(String function)
        {
            String hash;
            Byte[] bytes = new byte[16];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            hash = function + BitConverter.ToString(bytes);

            return hash;
            
        }

        public bool checkReady(String key)
        {
            if (!queue.Contains(key) || queue.Count == 0)
            {
                return true;
            }
            if (_inUse && timeout < 250)
            {
                if (queue.Count > 0)
                {
                    if (key.Equals(queue[0]))
                    {
                        timeout++;
                    }
                }
                return false;
            }
            else
            {
                if (queue.Count > 0)
                {
                    timeout = 0;
                    activeKey = queue[0];
                    queue.RemoveAt(0);
                    _inUse = true;
                }
                        
                if (activeKey.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }

        public void Finish()
        {
            _inUse = false;
        }

        public bool waitUntilReady(String func)
        {
            String key = generateHash(func);
            int localTimeout = 0; 
            if (!queue.Contains(key))
            {
                queue.Add(key);
            }
            while (!checkReady(key))
            {
                Thread.Sleep(1);
                localTimeout++;
                if (localTimeout >= 500)
                {
                    Clean(key);
                }
            }
            return true;
        }

        public void Clean(String key)
        {
            if (queue.Count > 0)
            {
                int index = queue.IndexOf(key);
                try
                {
                    if (queue.Contains(key))
                    {
                        queue.RemoveRange(0, index);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("?");
                }
            }

            return;

        }

        
    }
}
