﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
using EchoServer.Protocols;

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

        private static Protocol SelectedProtocol = null;

        static void Main(string[] args)
        {
            try
            {
                IP_Point = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);

                Server = new TcpListener(IP_Point);

                SelectProtocol();

                Server.Start();

                Console.WriteLine(
                    "Сервер запущен\n\n" +
                    "IP address: " + ServerIP + "\n" +
                    "Port: " + Convert.ToString(ServerPort) + "\n" +
                    "Кодировка: " + ServerEncoding.EncodingName + "\n" +
                    "Протокол: " + SelectedProtocol.ProtocolName + "\n"
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

                SelectedProtocol.Init(ClientStream, ServerEncoding);

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

        private static void SelectProtocol()
        {
            Console.WriteLine(
                "Выберите протокол взаимодействия:\n" +
                "0 - Без протокола\n" +
                "1 - Modbus TCP\n"
                );

            ConsoleKeyInfo PressedKey;

            while (true)
            {
                PressedKey = Console.ReadKey();

                if (PressedKey.Key == ConsoleKey.D0 || PressedKey.Key == ConsoleKey.NumPad0)
                {
                    SelectedProtocol = new NoProtocol();
                    Console.WriteLine("\n");
                    break;
                }

                else if (PressedKey.Key == ConsoleKey.D1 || PressedKey.Key == ConsoleKey.NumPad1)
                {
                    SelectedProtocol = new Modbus();
                    Console.WriteLine("\n");
                    break;
                }

                Console.WriteLine("\nВыбран неизвестный протокол взаимодействия. Введите номер протокола заново.");
            }
        }

        private static void EchoAction()
        {
            while (true)
            {
                SelectedProtocol.ReceiveData();

                SelectedProtocol.SendAnswer();                
            }
        }
    }
}
