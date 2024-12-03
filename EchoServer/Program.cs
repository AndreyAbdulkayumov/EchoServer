using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        private static int TransmitInterval_ms = 100;

        static async Task Main(string[] args)
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

                        if (SelectedProtocol is IEcho)
                        {
                            EchoAction();
                        }
                        
                        else
                        {
                            await CycleTransmitAction();
                        }
                    }

                    catch (ClientDisconnectException)
                    {
                        Console.WriteLine("Клиент отлючился\n");
                    }

                    catch (Exception error)
                    {
                        Console.WriteLine("Возникла ошибка:\n" + error.Message + "\nКлиент был отключен.\n");
                    }

                    finally
                    {
                        ClientStream?.Close();
                        Client?.Close();
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
                "0 - Без протокола (отображается строка)\n" +
                "1 - Без протокола (отображаются байты)\n" +
                "2 - Modbus TCP\n" +
                "3 - Цикличная передача\n"
                );

            ConsoleKeyInfo PressedKey;

            while (true)
            {
                PressedKey = Console.ReadKey();

                if (PressedKey.Key == ConsoleKey.D0 || PressedKey.Key == ConsoleKey.NumPad0)
                {
                    SelectedProtocol = new NoProtocol(false);
                    Console.WriteLine("\n");
                    break;
                }

                else if (PressedKey.Key == ConsoleKey.D1 || PressedKey.Key == ConsoleKey.NumPad1)
                {
                    SelectedProtocol = new NoProtocol(true);
                    Console.WriteLine("\n");
                    break;
                }

                else if (PressedKey.Key == ConsoleKey.D2 || PressedKey.Key == ConsoleKey.NumPad2)
                {
                    SelectedProtocol = new Modbus();
                    Console.WriteLine("\n");
                    break;
                }

                else if (PressedKey.Key == ConsoleKey.D3 || PressedKey.Key == ConsoleKey.NumPad3)
                {
                    SelectedProtocol = new CycleTransmit();
                    Console.WriteLine("\n");

                    Console.WriteLine("Введите интервал передачи в мс.");

                ENTER_NUMBER:

                    string Number = Console.ReadLine();

                    if (Int32.TryParse(Number, out TransmitInterval_ms) == false)
                    {
                        Console.WriteLine("Введен неправильный формат числа.");

                        goto ENTER_NUMBER;
                    }

                    if (TransmitInterval_ms <= 0)
                    {
                        Console.WriteLine("Можно вводить только числа больше нуля.");

                        goto ENTER_NUMBER;
                    }

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

        private static async Task CycleTransmitAction()
        {
            while (true)
            {
                SelectedProtocol.SendAnswer();

                await Task.Delay(TransmitInterval_ms);
            }
        }
    }
}
