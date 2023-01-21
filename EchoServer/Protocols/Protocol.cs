using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer.Protocols
{
    internal abstract class Protocol
    {
        public byte[] ReceiveBuffer { get; protected set; }
        public string ProtocolName { get; protected set; } = "";

        protected int NumberOfReceivedBytes = 0;

        protected NetworkStream Client = null;
        protected Encoding GlobalEncoding = null;

        public virtual void Init(NetworkStream Client, Encoding GlobalEncoding)
        {
            this.Client = Client;
            this.GlobalEncoding = GlobalEncoding;
        }

        public abstract void ReceiveData();
        public abstract void SendAnswer();
    }
}
