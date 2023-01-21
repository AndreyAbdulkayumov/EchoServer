using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer.Protocols
{
    internal class NoProtocol : Protocol
    {
        private StringBuilder MessageBuilder = new StringBuilder();

        private string MessageFromClient;

        public NoProtocol()
        {
            ProtocolName = "Без протокола";
        }

        public override void Init(NetworkStream Client, Encoding GlobalEncoding)
        {
            base.Init(Client, GlobalEncoding);

            ReceiveBuffer = new byte[256];            
        }

        public override void ReceiveData()
        {
            try
            {
                do
                {
                    NumberOfReceivedBytes = Client.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);
                    MessageBuilder.Append(GlobalEncoding.GetString(ReceiveBuffer, 0, NumberOfReceivedBytes));
                }
                while (Client.DataAvailable);

                MessageFromClient = MessageBuilder.ToString();

                if (MessageFromClient == String.Empty)
                {
                    throw new ClientDisconnectException();
                }
            }

            // Исключение из за отключения клиента обрабатывается выше по стеку
            catch (ClientDisconnectException)
            {
                throw new ClientDisconnectException();
            }

            catch (Exception error)
            {
                throw new Exception("Ошибка приема данных.\n" + error.Message);
            }
        }

        public override void SendAnswer()
        {
            try
            {
                Console.WriteLine(MessageFromClient);

                byte[] MessageToClient = GlobalEncoding.GetBytes(MessageBuilder.ToString());

                Client.Write(MessageToClient, 0, MessageToClient.Length);

                for (int i = 0; i < NumberOfReceivedBytes; i++)
                {
                    ReceiveBuffer[i] = 0;
                }

                MessageBuilder.Clear();
            }

            catch (Exception error)
            {
                throw new Exception("Ошибка отпраки данных.\n" + error.Message);
            }
        }
    }
}
