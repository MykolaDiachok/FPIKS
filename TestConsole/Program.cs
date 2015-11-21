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
        static SerialPort s;
        static byte[] serialPortReceivedData;
        static List<byte> bBuffer = new List<byte>();
        static uint Curr_number = 0; // счетчик
        static byte status;
        static byte result;

        static void Main(string[] args)
        {


            //006 016 002 109 051 000 000 048 032 057 001 000 032 057 001 000 255 253 128 016 003 
            //006 016 002 176 051 000 000 048 071 173 000 000 072 173 000 000 150 253 113 016 003 
            
            //byte[] a = new byte[] { 006, 016, 002, 109, 051, 000, 000, 048, 032, 057, 001, 000, 032, 057, 001, 000, 255, 253, 128, 016, 003 };

            //byte[] a = new byte[] { 032, 057, 001, 000, 032, 057, 001, 000, 255, 253 };
            //byte[] b = new byte[] {  071, 173, 000, 000, 072, 173, 000, 000, 150, 253 };

            //byte[] a1 = new byte[] { 057, 001, 000 };
            //byte[] a2 = new byte[] { 032, 057, 001 };
            //byte[] a3 = new byte[] { 000, 255, 253 };

            //Console.ReadKey();
            //BitConverter.ToInt32(new byte[] { 057, 001, 000, 032, 057, 001, 000, 255, 253 },0)
            FPIKS.FPIKS ics = new FPIKS.FPIKS();
            bool connect = ics.FPInit((byte)50,9600,500,500);
            ////ics.FPNullCheck();
            if (connect)
            {
                Dictionary<string, object> getinfo = ics.getDicInfo;



                Console.WriteLine(getinfo["Error"]);

                Dictionary<string, object> KlefMem = ics.getKlefInfo;
            //    Dictionary<string, object> info = ics.getDicInfo;
            //    //if (Convert.ToBoolean(info["SmenaOpened"]) && Convert.ToBoolean(info["BitStatus5"]) && Convert.ToByte(info["ByteStatus"]) == 32)
            //    //{
                Console.WriteLine(KlefMem["PacketFirst"]);
                Console.WriteLine(KlefMem["PacketLast"]);
                Console.WriteLine(KlefMem["FreeMem"]);
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




    }
}