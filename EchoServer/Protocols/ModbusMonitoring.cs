using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace EchoServer.Protocols
{
    internal class ModbusMonitoring : Modbus
    {
        private const int HeaderPrefixLength = 6;
        private const int MaxFrameLength = 260;
        private const int SinePointCount = 20;

        private readonly ushort[] inputRegisters = new ushort[20];
        private readonly Random random = new Random();
        private int sinePoint;

        public ModbusMonitoring()
        {
            ProtocolName = "Modbus TCP (мониторинг)";
            UpdateInputRegisters();
        }

        public override void Init(NetworkStream client, Encoding globalEncoding)
        {
            base.Init(client, globalEncoding);
            ReceiveBuffer = new byte[MaxFrameLength];
        }

        public override void ReceiveData()
        {
            // Поле Length в MBAP включает Unit ID и PDU.
            NumberOfReceivedBytes = 0;
            ReadUntil(HeaderPrefixLength);
            int length = ReadUInt16(ReceiveBuffer, 4);
            if (ReadUInt16(ReceiveBuffer, 2) != 0 || length < 2 || HeaderPrefixLength + length > MaxFrameLength)
            {
                throw new InvalidDataException("Некорректный заголовок Modbus TCP.");
            }

            ReadUntil(HeaderPrefixLength + length);
        }

        private void ReadUntil(int frameOffset)
        {
            while (NumberOfReceivedBytes < frameOffset)
            {
                int read = Client.Read(ReceiveBuffer, NumberOfReceivedBytes, frameOffset - NumberOfReceivedBytes);
                if (read == 0)
                {
                    throw new ClientDisconnectException();
                }
                NumberOfReceivedBytes += read;
            }
        }

        public override void SendAnswer()
        {
            byte[] response = BuildResponse();
            Client.Write(response, 0, response.Length);
            UpdateInputRegisters();
        }

        private byte[] BuildResponse()
        {
            if (ReceiveBuffer[7] != (byte)MessageType.ReadInputRegisters)
            {
                return ExceptionResponse(1); // Illegal Function
            }
            if (NumberOfReceivedBytes != 12)
            {
                return ExceptionResponse(3); // Illegal Data Value
            }

            int start = ReadUInt16(ReceiveBuffer, 8);
            int count = ReadUInt16(ReceiveBuffer, 10);
            if (count < 1 || count > 125)
            {
                return ExceptionResponse(3);
            }
            if (start + count > inputRegisters.Length)
            {
                return ExceptionResponse(2); // Illegal Data Address
            }

            int byteCount = count * 2;
            byte[] response = CreateResponse(3 + byteCount);
            response[8] = (byte)byteCount;
            for (int i = 0; i < count; i++)
            {
                ushort value = inputRegisters[start + i];
                response[9 + 2 * i] = (byte)(value >> 8);
                response[10 + 2 * i] = (byte)value;
            }
            return response;
        }

        private byte[] CreateResponse(int length)
        {
            byte[] response = new byte[HeaderPrefixLength + length];
            Array.Copy(ReceiveBuffer, response, 8);
            response[4] = (byte)(length >> 8);
            response[5] = (byte)length;
            return response;
        }

        private byte[] ExceptionResponse(byte code)
        {
            byte[] response = CreateResponse(3);
            response[7] |= 0x80;
            response[8] = code;
            return response;
        }

        private static int ReadUInt16(byte[] buffer, int offset)
        {
            return (buffer[offset] << 8) | buffer[offset + 1];
        }

        private void UpdateInputRegisters()
        {
            inputRegisters[0] = unchecked((ushort)(inputRegisters[0] + 1));
            inputRegisters[1] = (ushort)(ushort.MaxValue - inputRegisters[0]);
            SetFloat(2, random.Next(1000) / 100f);
            SetFloat(4, random.Next(10000) / 100f);
            SetFloat(6, (float)(2.5 * Math.Sin(2 * Math.PI * sinePoint / SinePointCount)));
            sinePoint = (sinePoint + 1) % SinePointCount;
            inputRegisters[8] = (ushort)random.Next(65536);
            inputRegisters[9] = (ushort)random.Next(65536);
        }

        private void SetFloat(int address, float value)
        {
            int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            inputRegisters[address] = (ushort)bits;
            inputRegisters[address + 1] = (ushort)(bits >> 16);
        }
    }
}
