using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    internal class ClientDisconnectException : Exception
    {

    }

    internal class Program
    {
        private const string ServerIP = "127.168.0.3";
        private const int ServerPort = 802;
        private static Encoding ServerEncoding = Encoding.UTF8;

        private static IPEndPoint IP_Point = null;
        private static TcpListener Server = null;

        private static TcpClient Client = null;
        private static NetworkStream ClientStream = null;

        static void Main(string[] args)
        {
            try
            {
                IP_Point = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);

                Server = new TcpListener(IP_Point);

                Server.Start();

                Console.WriteLine(
                    "Сервер запущен\n\n" +
                    "IP address: " + ServerIP + "\n" +
                    "Port: " + Convert.ToString(ServerPort) + "\n" +
                    "Кодировка: " + ServerEncoding.EncodingName + "\n"
                    );

                while (true)
                {
                    try
                    {
                        WaitConnect();

                        EchoAction();
                    }

                    catch (ClientDisconnectException)
                    {
                        ClientStream?.Close();
                        Client?.Close();

                        Console.WriteLine("Клиент отлючился\n");
                    }

                    catch (Exception error)
                    {
                        ClientStream?.Close();
                        Client?.Close();

                        Console.WriteLine("Возникла ошибка:\n" + error.Message + "\nКлиент был отключен.\n");
                    }
                }
            }
            
            catch (Exception error)
            {
                Console.WriteLine(
                    "Ошибка инициализации сервера:\n" + error + "\n\n" +
                    "Данные сервера:\n" +
                    "IP address: " + ServerIP + "\n" +
                    "Port: " + Convert.ToString(ServerPort) + "\n\n" +
                    "Программа будет закрыта. Для продолжения нажмите любую клавишу...\n"
                    );

                Console.ReadKey();
            }
        }

        private static void WaitConnect()
        {
            try
            {
                Console.WriteLine("Ожидание входящего подключения...\n");

                Client = Server.AcceptTcpClient();

                ClientStream = Client.GetStream();

                Console.WriteLine(
                    "Клиент подключен\n" + 
                    "IP address: " + 
                    IPAddress.Parse(((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString()) + "\n");
            }

            catch (Exception error)
            {
                throw new Exception("Ошибка подключения:\n" + error.Message);
            }
        }

        private static void EchoAction()
        {
            byte[] ReceivedData = new byte[256];
            int NumberOfReceivedBytes;

            StringBuilder MessageBuilder = new StringBuilder();

            string MessageFromClient;

            while (true)
            {
                do
                {
                    NumberOfReceivedBytes = ClientStream.Read(ReceivedData, 0, ReceivedData.Length);
                    MessageBuilder.Append(ServerEncoding.GetString(ReceivedData, 0, NumberOfReceivedBytes));
                }
                while (ClientStream.DataAvailable);

                MessageFromClient = MessageBuilder.ToString();

                if (MessageFromClient == String.Empty)
                {
                    throw new ClientDisconnectException();
                }

                else
                {
                    Console.WriteLine(MessageFromClient);

                    byte[] MessageToClient = ServerEncoding.GetBytes(MessageBuilder.ToString());

                    ClientStream.Write(MessageToClient, 0, MessageToClient.Length);

                    for(int i = 0; i < NumberOfReceivedBytes; i++)
                    {
                        ReceivedData[i] = 0;
                    }
                    
                    MessageBuilder.Clear();
                }
            }
        }
    }
}
