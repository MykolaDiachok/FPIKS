using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FPIKS;

namespace TestConsole
{
    class Program
    {
        const byte DLE = 0x10;
        const byte STX = 0x02;
        const byte ETX = 0x03;
        const byte ACK = 0x06;
        const byte NAK = 0x15;
        const byte SYN = 0x16;
        const byte ENQ = 0x05;
        //static SerialPort s;
        //static byte[] serialPortReceivedData;
        static List<byte> bBuffer = new List<byte>();
        //static uint Curr_number = 0; // счетчик
        //static byte status;
        //static byte result;

        static void Main(string[] args)
        {
            //    10 02 46 00 ba 10 03 a8 a8                        ..F.є..ЁЁ        

            FPIKS.FPIKS ics = new FPIKS.FPIKS();
            bool connect = ics.FPInit((byte)11,9600,500,500);
            ////ics.FPNullCheck();
            if (connect)
            {
                
                ics.FPDayReport(0);
                Dictionary<string, object> getinfo = ics.getDicInfo;

                Console.WriteLine(getinfo["Error"]);

                //Dictionary<string, object> KlefMem = ics.getKlefInfo;
            //    Dictionary<string, object> info = ics.getDicInfo;
            //    //if (Convert.ToBoolean(info["SmenaOpened"]) && Convert.ToBoolean(info["BitStatus5"]) && Convert.ToByte(info["ByteStatus"]) == 32)
            //    //{
                //Console.WriteLine(KlefMem["PacketFirst"]);
                //Console.WriteLine(KlefMem["PacketLast"]);
                //Console.WriteLine(KlefMem["FreeMem"]);
            //    //}
            //    //UInt16 tempt = ics.getBeginKLEF;
            //    //UInt16 EndKlef = ics.getEndKLEF;
            //    //bool i = ics.FPGetTaxRates;
            //    //Console.WriteLine("Состояние обрезчика:{0}", ics.GetFPCplCutter);
            //    //Console.WriteLine("Перевод обрезчика:{0}", ics.FPCplCutter());
            //    //Console.WriteLine("Состояние обрезчика:{0}", ics.GetFPCplCutter);            
            //    //Console.WriteLine("Перевод обрезчика:{0}", ics.FPCplCutter());            
            }
            ics.FPClose();

            Console.ReadKey();
        }


        static string PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.AppendFormat("{0:x2}", b );
                sb.Append(" ");
            }
            sb.Append("}");
            return sb.ToString();
        }




        class CRC16 
        {

            //private const ushort polynomial = 0x1021;
            private ushort[] table = new ushort[256];

            public CRC16(ushort polynomial)
               
            {
                ushort value;
                ushort temp;
                for (ushort i = 0; i < table.Length; ++i)
                {
                    value = 0;
                    temp = i;
                    for (byte j = 0; j < 8; ++j)
                    {
                        if (((value ^ temp) & 0x0001) != 0)
                        {
                            value = (ushort)((value >> 1) ^ polynomial);
                        }
                        else
                        {
                            value >>= 1;
                        }
                        temp >>= 1;
                    }
                    table[i] = value;
                }
            }

            public  byte[] ComputeChecksumBytes(byte[] arr)
            {
                uint crc = this.ComputeChecksum(arr);
                return new byte[] { (byte)(crc >> 8), (byte)(crc & 0x00ff) };
            }

            protected  uint ComputeChecksum(byte[] bytes)
            {
                ushort crc = 0;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
                return crc;
            }
        }




    }
}