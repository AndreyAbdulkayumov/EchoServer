using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace EchoServer.Protocols
{
    internal class NoProtocol : Protocol, IEcho
    {
        private readonly bool _isByteView;

        private List<byte> _receivedBytes = new List<byte>();

        public NoProtocol(bool isByteView)
        {
            _isByteView = isByteView;

            if (isByteView)
            {
                ProtocolName = "Без протокола (отображаются байты)";
            }

            else
            {
                ProtocolName = "Без протокола (отображается строка)";
            }
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

                    if (NumberOfReceivedBytes == 0)
                    {
                        throw new ClientDisconnectException();
                    }

                    _receivedBytes.AddRange(ReceiveBuffer.Take(NumberOfReceivedBytes));
                }
                while (Client.DataAvailable);

                string message = _isByteView ?
                    string.Join(" ", _receivedBytes.Select(x => x.ToString("X2"))) :
                    GlobalEncoding.GetString(ReceiveBuffer, 0, NumberOfReceivedBytes);

                Console.WriteLine(message);

                for (int i = 0; i < ReceiveBuffer.Length; i++)
                {
                    ReceiveBuffer[i] = 0;
                }
            }

            // Исключение из-за отключения клиента обрабатывается выше по стеку
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
                Client.Write(_receivedBytes.ToArray(), 0, _receivedBytes.Count());

                _receivedBytes.Clear();
            }

            catch (Exception error)
            {
                throw new Exception("Ошибка отпраки данных.\n" + error.Message);
            }
        }
    }
}
