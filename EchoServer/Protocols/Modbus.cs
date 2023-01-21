using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer.Protocols
{
    internal class Modbus : Protocol
    {
        public enum MessageType
        {
            ReadCoilStatus = 1,
            ReadDiscreateInputs = 2,

            ReadHoldingRegisters = 3,
            ReadInputRegisters = 4,

            ForceSingleCoil = 5,
            PresetSingleRegister = 6,

            ForceMultipleCoils = 15,
            PresetMultipleRegisters = 16
        }

        ModbusMessage ResponseMessage;

        public Modbus()
        {
            ProtocolName = "Modbus TCP";
        }

        public override void Init(NetworkStream Client, Encoding GlobalEncoding)
        {
            base.Init(Client, GlobalEncoding);

            ReceiveBuffer = new byte[50];            
        }

        public override void ReceiveData()
        {
            try
            {
                do
                {
                    NumberOfReceivedBytes = Client.Read(ReceiveBuffer, NumberOfReceivedBytes, 40);
                }
                while (Client.DataAvailable);

                if (NumberOfReceivedBytes <= 0)
                {
                    throw new ClientDisconnectException();
                }

                for (int i = 0; i < NumberOfReceivedBytes; i++)
                {
                    Console.Write(ReceiveBuffer[i] + "  ");
                }

                Console.WriteLine("\n");
            }

            // Исключение из за отключения клиента обрабатывается выше по стеку
            catch (ClientDisconnectException)
            {
                throw new ClientDisconnectException();
            }

            catch (Exception error)
            {
                throw new Exception("Ошибка приема данных по протоколу Modbus TCP.\n" + error.Message);
            }
        }

        public override void SendAnswer()
        {
            try
            {
                MessageType Type = ModbusMessage.GetMessageType(ReceiveBuffer);
                                
                switch (Type)
                {
                    case MessageType.ReadInputRegisters:
                        ResponseMessage = new ReadMessage();
                        break;

                    case MessageType.PresetSingleRegister:
                        ResponseMessage = new WriteMessage();
                        break;

                    default:
                        throw new Exception("Неподдерживаемая функция Modbus. Код: " + (short)Type);
                }

                byte[] ReceivedData = new byte[NumberOfReceivedBytes];
                Array.Copy(ReceiveBuffer, ReceivedData, ReceivedData.Length);

                byte[] Response = ResponseMessage.GetResponseMessage(ReceivedData);

                Client.Write(Response, 0, Response.Length);

                for (int i = 0; i < NumberOfReceivedBytes; i++)
                {
                    ReceiveBuffer[i] = 0;
                }

                NumberOfReceivedBytes = 0;
            }

            catch (Exception error)
            {
                throw new Exception("Ошибка отпраки данных по протоколу Modbus TCP.\n" + error.Message);
            }
        }

        private abstract class ModbusMessage
        {
            public abstract byte[] GetResponseMessage(byte[] ReceivedMessage);

            public static MessageType GetMessageType(byte[] ReceivedMessage)
            {
                switch(ReceivedMessage[7])
                {
                    case 1:
                        return MessageType.ReadCoilStatus;

                    case 2:
                        return MessageType.ReadDiscreateInputs;

                    case 3:
                        return MessageType.ReadHoldingRegisters;

                    case 4:
                        return MessageType.ReadInputRegisters;

                    case 5:
                        return MessageType.ForceSingleCoil;

                    case 6:
                        return MessageType.PresetSingleRegister;

                    case 15:
                        return MessageType.ForceMultipleCoils;

                    case 16:
                        return MessageType.PresetMultipleRegisters;

                    default:
                        throw new Exception("Неизвестный код функции Modbus: " + ReceivedMessage[7]);
                }
            }
        }

        private class WriteMessage : ModbusMessage
        {
            public override byte[] GetResponseMessage(byte[] ReceivedMessage)
            {
                byte[] Response = new byte[ReceivedMessage.Length];

                Array.Copy(ReceivedMessage, Response, ReceivedMessage.Length);

                return Response;
            }
        }

        private class ReadMessage : ModbusMessage
        {
            public override byte[] GetResponseMessage(byte[] ReceivedMessage)
            {
                byte[] Response = new byte[ReceivedMessage.Length];

                Array.Copy(ReceivedMessage, Response, 4);

                Response[4] = 0;
                Response[5] = 4;
                Response[6] = ReceivedMessage[6];
                Response[7] = ReceivedMessage[7];
                Response[8] = 2;

                Random Value = new Random();

                Response[9] = (byte)Value.Next(0, 255);
                Response[10] = (byte)Value.Next(0, 255);

                return Response;
            }
        }
    }
}
