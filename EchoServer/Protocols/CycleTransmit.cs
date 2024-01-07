using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer.Protocols
{
    internal class CycleTransmit : Protocol
    {
        private int Counter = 0;

        public override void Init(NetworkStream Client, Encoding GlobalEncoding)
        {
            base.Init(Client, GlobalEncoding);

            Counter = 0;
        }

        public override void ReceiveData()
        {
            throw new NotImplementedException();
        }

        public override void SendAnswer()
        {
            byte[] Bytes = GlobalEncoding.GetBytes(
                Counter.ToString() + "\t" + 
                Counter.ToString() + "\t" + 
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t" +
                Counter.ToString() + "\t"
                );

            Client.Write(Bytes, 0, Bytes.Length);

            Counter++;

            if (Counter >= 100)
            {
                Counter = 0;
            }
        }
    }
}
