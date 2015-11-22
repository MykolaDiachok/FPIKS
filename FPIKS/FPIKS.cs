using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace FPIKS
{
    public class FPIKS
    {

        const byte DLE = 0x10;
        const byte STX = 0x02;
        const byte ETX = 0x03;
        const byte ACK = 0x06;
        const byte NAK = 0x15;
        const byte SYN = 0x16;
        const byte ENQ = 0x05;

        SerialPort s=null;
        byte LastCommand = 255;

        int getchecksum(byte[] buf, int len)
        {
            int i, sum, n, res;
            byte lobyte, cs;

            n = len - 3;
            sum = 0;
            cs = 0x00;
            lobyte = 0x00;

            for (i = 2; i < n; i++)
                //for (i = 0; i < buf.Length; ++i)
                sum += Convert.ToInt16(buf[i]);

            do
            {
                res = sum + cs;
                cs++;
                lobyte = (byte)(res & 0xFF);
            }
            while (lobyte != 0x00);
            return cs - 1;
        }

      

        byte getchecksum(List<byte> buf)
        {
            int i, n;
            byte lobyte, cs;
            uint sum, res;

            n = buf.Count - 3;
            sum = 0;
            cs = 0x00;
            lobyte = 0x00;

            for (i = 2; i < n; i++)
                sum += buf[i];

            do
            {
                res = sum + cs;
                cs++;
                lobyte = (byte)(res & 0xFF);
            }
            while (lobyte != 0x00);
            return (byte)(cs - 1);
        }


        uint Curr_number = 0; // счетчик
        //string _ComPort;
        byte ByteStatus=0; // Возврат ФР статус
        byte ByteResult=0; // Возврат ФР результат
        byte ByteReserv=0; // Возврат ФР результат
        public bool bLastCommand; // Инфо о успешности последней команды

        string _SerialNumber, _FiscalNumber, _version;
        DateTime _MadeDate, _RegistrationDate;


        #region Ошибки

        public byte GetByteStatus
        {
            get
            {
                return ByteStatus;
            }
        }

        public byte GetByteResult
        {
            get
            {
                return ByteResult;
            }
        }

        public byte GetByteReserv
        {
            get
            {
                return ByteReserv;
            }
        }

        public string GetTextErrorMessage
        {

            //num & (1 << i

            get
            {
                if ((ByteStatus == 0) && (ByteResult == 0))
                    return "Операция была завершена без ошибок.";
                else if (ByteResult == 1)
                    return "Ошибка принтера(печатающего устройства).";
                else if (ByteResult == 2)
                    return "Закончилась чековая или контрольная лента. Необходимо заправить новый рулон. ";
                else if (ByteResult == 4)
                    return "Произошел сбой фискальной памяти.";
                else if (ByteResult == 6)
                    return "Снижение напряжения питания.";
                else if (ByteResult == 8)
                    return "Фискальная память ЭККР переполнена.";
                else if (ByteResult == 10)
                    return "не было персонализации ";
                else if (ByteResult == 16)
                    return "Неправильная команда или команда запрещена в данном режиме.";
                else if (ByteResult == 19)
                    return "Ошибка программирования пользовательского логотипа. ";
                else if (ByteResult == 20)
                    return "Ошибка длины строки параметра. Длина строки превышает допустимое значение.";
                else if (ByteResult == 21)
                    return "Неправильный пароль. ";
                else if (ByteResult == 22)
                    return "Неверно задан номер оператора.";
                else if (ByteResult == 23)
                    return "Налоговая группа не существует либо не установлена. Возможно, налоговые группы не были запрограммированы. ";
                else if (ByteResult == 24)
                    return "Ошибка формы оплаты. Тип оплаты не существует. ";
                else if (ByteResult == 25)
                    return "Попытка передачи недопустимых кодов символов. ";
                else if (ByteResult == 26)
                    return "Заданное количество налоговых групп больше допустимого. ";
                else if (ByteResult == 27)
                    return "Общая сумма отмен позиций в чеке превышает общую сумму продаж чека.";
                else if (ByteResult == 28)
                    return "Ошибка описания в артикуле(название,  налоговая группа). ";
                else if (ByteResult == 30)
                    return "Ошибка формата даты/времени. ";
                else if (ByteResult == 31)
                    return "Превышен предел количества записей в чеке. ";
                else if (ByteResult == 32)
                    return "Превышен предел разрядности вычисленной стоимости товара(услуги).";
                else if (ByteResult == 33)
                    return "Ошибка переполнения регистра дневного оборота. ";
                else if (ByteResult == 34)
                    return "Ошибка переполнения регистра оплат.";
                else if (ByteResult == 35)
                    return "Попытка выдачи большей суммы наличных, чем находится в денежном ящике. ";
                else if (ByteResult == 36)
                    return "Устанавливаемая дата предшествует дате последнегоZ-отчета. ";
                else if (ByteResult == 37)
                    return "Операция продажи не может быть выполнена, поскольку открыт чек выплаты. ";
                else if (ByteResult == 38)
                    return "Операция выплаты не может быть выполнена, поскольку открыт чек продажи.";
                else if (ByteResult == 39)
                    return "Команда не может быть выполнена,  поскольку чек не открыт. ";
                else if (ByteResult == 41)
                    return "Команда не может быть выполнена, поскольку сначала нужно выполнитьZ-отчет. ";
                else if (ByteResult == 42)
                    return "Выполнение команды невозможно, поскольку не было чеков.";
                else if (ByteResult == 43)
                    return "Выдача сдачи запрещена с указанной формы оплаты. ";
                else if (ByteResult == 44)
                    return "Выполнение команды невозможно, поскольку текущий чек не закрыт. ";
                else if (ByteResult == 45)
                    return "Выполнение операций со скидками и наценками невозможно, поскольку перед этим не было операций продаж(выплат). ";
                else if (ByteResult == 46)
                    return "Операция не может быть выполнена после начала оплат. ";
                else if (ByteResult == 47)
                    return "переполнение контрольной ленты";
                else if (ByteResult == 48)
                    return "неправильный номер данных КЛЕФ";
                else if (ByteResult == 50)
                    return "команда запрещена, КЛЕФ не пустой";                
                else if (ByteResult == 101)
                    return "Ошибка. Нет ответа от ЭККР.";
                return "Ошибка не определена";
            }

        }

        #endregion
        #region CRC16

        static byte[] returnWithOutDublicateETX(byte[] source)
        {
            return returnWithOutDublicate(source, new byte[] { DLE, DLE });
        }

        static byte[] returnWithOutDublicate(byte[] source, byte[] pattern)
        {

            List<byte> tReturn = new List<byte>();
            int sLenght = source.Length;
            for (int i = 0; i < sLenght; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    tReturn.Add(source[i]);
                    i++;
                }
                else
                {
                    tReturn.Add(source[i]);
                }
            }
            return (byte[])tReturn.ToArray();
        }

        static int? PatternAt(byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    return i;
                }
            }
            return null;
        }

        static string PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.AppendFormat("{0:x2}", b);
                sb.Append(" ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        ///

        public enum Crc16Mode : ushort { Standard = 0xA001, CcittKermit = 0x8408 }

        public class Crc16
        {
            static ushort[] table = new ushort[256];

            public ushort ComputeChecksum(params byte[] bytes)
            {
                ushort crc = 0;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
                return crc;
            }

            public byte[] ComputeChecksumBytes(params byte[] bytes)
            {
                ushort crc = ComputeChecksum(bytes);
                return BitConverter.GetBytes(crc);
            }

            public Crc16(Crc16Mode mode)
            {
                ushort polynomial = (ushort)mode;
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
        }

        public byte[] returnArrayBytesWithCRC16(byte[] inBytes)
        {
            Crc16 crc = new Crc16(Crc16Mode.CcittKermit);
            byte[] bytebegin = { DLE, STX };
            byte[] byteend = { DLE, ETX };

            byte[] tempb = returnWithOutDublicateETX(inBytes);
            var searchBegin = PatternAt(tempb, bytebegin);
            if (searchBegin == null)
                return null;

            var searchEnd = PatternAt(tempb, byteend);
            if (searchEnd == null)
                return null;

            var newArr = tempb.Skip((int)searchBegin + 2).Take((int)searchEnd - 2).ToArray();

            byte[] a = new byte[newArr.Length + 1];
            newArr.CopyTo(a, 0);
            a[newArr.Length] = ETX;
            

            //var control = tempb.Skip((int)searchEnd + 2).Take(2).ToArray();
            

            byte[] crcBytes = crc.ComputeChecksumBytes(a);
            byte[] retBytes = new byte[inBytes.Length + 2];
            inBytes.CopyTo(retBytes, 0);
            retBytes[retBytes.Length - 2] = crcBytes[0];
            retBytes[retBytes.Length - 1] = crcBytes[1];
            return retBytes;
        }
        #endregion
        #region Основные обработки
        byte[] preparation(byte[] _byte)
        {
            bBuffer.Clear();
            List<byte> _out = new List<byte>();
            _out.Add(DLE);
            _out.Add(STX);
            _out.Add((byte)Curr_number);
            _out.AddRange(_byte);
            _out.Add(0x00);
            _out.Add(DLE);
            _out.Add(ETX);
            _out[_out.Count - 3] = getchecksum(_out); //считает контрольную сумму
            
            for (int pos = 2; pos <= _out.Count - 5; pos++)
            //for (int pos = 2; pos <= _out.Count - 3; pos++)
                {
                if (_out[pos] == DLE)
                {
                    _out.Insert(pos + 1, DLE);
                    pos++;
                }
            }
            return returnArrayBytesWithCRC16(_out.ToArray());
        }

        byte[] serialPortReceivedData;
        List<byte> bBuffer = new List<byte>();


        //Если true то еще не завершился.
        // 0 = не ответа
        // 1 = ACK
        // 2 = NAK
        // 3 = SYN
        // 4 = Good
        // 5 = Unknown
        int processing(byte[] income, byte[] answer, out byte[] ReturnData)
        {

            List<byte> _ReturnData = new List<byte>();
            bLastCommand = false;
            ByteStatus = 254;
            ByteResult = 0;
            ByteReserv = 0;
            List<byte> _income = new List<byte>();
            _income.AddRange(income);
            List<byte> _answer = new List<byte>();
            _answer.AddRange(answer);
            if (_answer.Count == 0)
            {
                ReturnData = _ReturnData.ToArray();
                
                return 0;
            }
            else if ((_answer.Count < _income.Count) && (_answer[0] == ACK) && (_answer[_answer.Count - 1] != ETX)) //ACK
            {
                ReturnData = _ReturnData.ToArray();
                
                return 1;
            }
            else if ((_answer.Count < _income.Count) && (_answer[0] == NAK)) //NAK
            {
                ByteStatus = 253;
                ReturnData = _ReturnData.ToArray();
                
                return 2;
            }
            else if ((_answer[_answer.Count - 1] == SYN)) //&& (_answer[0] == SYN)) //SYN
            {
                ReturnData = _ReturnData.ToArray();
                
                return 3;
            }
            else if ((_answer.Count >= 3) && (_answer[_answer.Count - 1] == ETX))
            {
                int beg = 0;
                for (int x = 0; x <= _answer.Count-1; x++)
                {
                    if ((_answer[x] == DLE) && (_answer[x + 1] == STX))
                    {
                        beg = x + 2;
                        break;
                    }
                }
                if (beg > 0)
                {
                    int curix = 2;
                    if (_answer[beg] != income[curix])
                    {
                        bLastCommand = false;
                        ReturnData = _ReturnData.ToArray();
                        _ReturnData = null;
                        return 6;
                    }
                    if (income[curix] == DLE)
                        curix++;
                    if (_answer[beg] == DLE) //Если номер
                        beg++;
                    beg++;
                    curix++;


                    if (_answer[beg] != income[curix])
                    {
                        bLastCommand = false;
                        ReturnData = _ReturnData.ToArray();
                
                        return 6;
                    }

                    //if ((income[3] == DLE) && _answer[beg] != income[4])  
                    //{
                    //    bLastCommand = !false;
                    //    ReturnData = _ReturnData.ToArray();
                    //    return 5;
                    //}
                    //else if ((_answer[beg] != income[3]) && (income[3] != DLE))
                    //{
                    //    bLastCommand = !false;
                    //    ReturnData = _ReturnData.ToArray();
                    //    return 5;
                    //}
                    if (_answer[beg] == DLE)// Если код
                        beg++;
                    ByteStatus = _answer[beg + 1];
                    ByteResult = _answer[beg + 2];
                    ByteReserv = _answer[beg + 3];
                    if ((ByteStatus == 0) && (ByteResult == 0))
                        bLastCommand = true;
                    else
                        bLastCommand = false;
                    for (int x = beg + 4; x <= _answer.Count-4; x++)
                    {
                        //if ((_answer[x + 1] == DLE) && (_answer[x + 2] == ETX))
                        //    break;
                        _ReturnData.Add(_answer[x]);
                    }
                }
                //Убиваем задвоения
                for (int x = 0; x < _ReturnData.Count - 1; x++)
                {
                    if ((_ReturnData[x] == DLE) && (_ReturnData[x + 1] == DLE))
                        _ReturnData.RemoveAt(x);
                }

                ReturnData = _ReturnData.ToArray();
                
                return 4;
            }
            ReturnData = _ReturnData.ToArray();
            
            return 5;

        }


//            List<byte> _ReturnData = new List<byte>();
//            bLastCommand = false;
//            ByteStatus = 254;
//            ByteResult = 0;
//            ByteReserv = 0;
//            List<byte> _income = new List<byte>();
//            _income.AddRange(income);
//            List<byte> _answer = new List<byte>();
//            _answer.AddRange(answer);
//            if (_answer.Count == 0)
//            {
//                ReturnData = _ReturnData.ToArray();
//                return 0;
//            }
//            else if ((_answer.Count < _income.Count) && (_answer[0] == ACK)&& (_answer[_answer.Count - 1] != ETX)) //ACK
//            {
//                ReturnData = _ReturnData.ToArray();
//                return 1;
//            }
//            else if ((_answer.Count < _income.Count) && (_answer[0] == NAK)) //NAK
//            {
//                ByteStatus = 253;
//                ReturnData = _ReturnData.ToArray();
//                return 2;
//            }
//            else if ((_answer[_answer.Count - 1] == SYN)) //&& (_answer[0] == SYN)) //SYN
//            {
//                ReturnData = _ReturnData.ToArray();
//                return 3;
//            }
//            else if ((_answer.Count >= 3) && (_answer[_answer.Count - 1] == ETX))
//            {
//                int beg = 0;
//                for (int x = 0; x <= _answer.Count-1; x++)
//                {
//                    if ((_answer[x] == DLE) && (_answer[x + 1] == STX))
//                    {
//                        beg = x + 2;
//                        break;
//                    }
//                }
//                if (beg > 0)
//                {
//                    int curix = 2;
//                    if (_answer[beg] != income[curix])
//                    {
//                        bLastCommand = false;
//                        ReturnData = _ReturnData.ToArray();
//                        return 5;
//                    }
//                    if (income[curix] == DLE)
//                        curix++;
//                    if (_answer[beg] == DLE) //Если номер
//                        beg++;
//                    beg++;
//                    curix++;


//                    if (_answer[beg] != income[curix])
//                    {
//                        bLastCommand = false;
//                        ReturnData = _ReturnData.ToArray();
//                        return 5;
//                    }

//                    //if ((income[3] == DLE) && _answer[beg] != income[4])  
//                    //{
//                    //    bLastCommand = !false;
//                    //    ReturnData = _ReturnData.ToArray();
//                    //    return 5;
//                    //}
//                    //else if ((_answer[beg] != income[3]) && (income[3] != DLE))
//                    //{
//                    //    bLastCommand = !false;
//                    //    ReturnData = _ReturnData.ToArray();
//                    //    return 5;
//                    //}
//                    ByteStatus = _answer[beg];
//                    if (_answer[beg] == DLE)// Если код
//                        beg++;
//                    beg++;
//                    ByteResult = _answer[beg];
//                    if (_answer[beg] == DLE)// Если код
//                        beg++;
//                    beg++;
//                    ByteReserv = _answer[beg];
//                    if (_answer[beg] == DLE)// Если код
//                        beg++;
//                    beg++;
                    
//                    //if (_answer[beg] == DLE)// Если код
//                      //  beg++;
//                    //beg++;
//                    if ((ByteStatus == 0) && (ByteResult == 0))
//                        bLastCommand = true;
//                    else
//                        bLastCommand = false;
//                    for (int x = beg; x <= _answer.Count-4; x++)
//                    {
                        
//                        //if ((_answer[x - 1] != DLE) && (_answer[x] == DLE) && (_answer[x+1] == ETX))
//                        //  break;
//                        //else if ((_answer[x - 2] == DLE) && (_answer[x - 1] == DLE) && (_answer[x] == DLE) && (_answer[x + 1] == ETX))
////                            break;
                        
//                        _ReturnData.Add(_answer[x]);
//                    }
//                }
//                //Убиваем задвоения
//                for (int x = 0; x < _ReturnData.Count-1; x++)
//                {
//                    if ((_ReturnData[x] == DLE) && (_ReturnData[x + 1] == DLE))
//                        _ReturnData.RemoveAt(x);
//                }

//                ReturnData = _ReturnData.ToArray();
//                return 4;
//            }
//            ReturnData = _ReturnData.ToArray();
//            return 5;
 
        bool working(byte[] Data)
        {
            //SerialPort s = new SerialPort(_ComPort, 9600, Parity.None, 8, StopBits.One);
            //s.Open();
            int ret=-1;

            if (!s.IsOpen)
            {
                ByteResult = 101;
                return ret == 4;
            }
            bool rep = true;
            int crep=0;
            while(rep)
            {
            //for (int ci = 1; ci <= 3; ci++)
            //{
                s.Write(Data, 0, Data.Length);
                Thread.Sleep(600);
                int byteRecieved = s.BytesToRead;
                byte[] messByte = new byte[byteRecieved];
                s.Read(messByte, 0, byteRecieved);
                int cicle = 0;
                byte[] temp = new byte[0];
                ret = processing(Data, messByte, out temp);
                while ((ret != 4))
                {
                    cicle++;
                    Thread.Sleep(600);
                    byteRecieved = s.BytesToRead;
                    messByte = new byte[byteRecieved];
                    s.Read(messByte, 0, byteRecieved);
                    ret = processing(Data, messByte, out temp);
                    //if ((ret == 3) || (ret == 1))
                    //    cicle = 0;
                    if ((ret != 6) && (ret != 2))
                        rep = false;
                    else
                    {
                        rep = true;
                        Thread.Sleep(600);
                        break;
                    }

                    if ((cicle > 10) || (ret == 2)) 
                        break;
                }
            //    if ((ret == 4)) //|| (ret != 5))                
            //        break;
                crep++;
                if ((crep > 5) || (ret == 4))
                    break;
            }
            Curr_number++;
            //s.Close();
            //s.Dispose();
            return ((ret == 4));
        }//Старые обработки

        bool working(byte[] Data, out byte[] returndata)
        {
            int ret = -1;
            List<byte> _ReturnData = new List<byte>();
            returndata = _ReturnData.ToArray();
            if ((Data == null) || Data.Length == 0)
                return ret == 4;
            //try
            //{               
                
                //SerialPort s = new SerialPort(_ComPort, 9600, Parity.None, 8, StopBits.One);
                //s.Open();
                if (!s.IsOpen)
                {
                    ByteStatus = 101;
                    returndata = _ReturnData.ToArray();
                    return ret == 4;
                }
                bool rep = true;
                int crep = 0;
                //for (int ci = 1; ci <= 3; ci++)
                //{
                while (rep)
                {
                    s.Write(Data, 0, Data.Length);
                    Thread.Sleep(600);
                    int byteRecieved = s.BytesToRead;
                    byte[] messByte = new byte[byteRecieved];
                    s.Read(messByte, 0, byteRecieved);
                    int cicle = 0;
                    ret = processing(Data, messByte, out returndata);
                    while ((ret != 4))
                    {
                        cicle++;
                        Thread.Sleep(600);
                        byteRecieved = s.BytesToRead;
                        messByte = new byte[byteRecieved];
                        s.Read(messByte, 0, byteRecieved);
                        ret = processing(Data, messByte, out returndata);
                        if ((ret != 6) && (ret != 2))
                            rep = false;
                        else
                        {
                            rep = true;
                            break;
                        }
                        //if ((ret == 3) || (ret == 1))
                        //    cicle = 0;

                        if ((cicle > 10) || (ret == 2)) break;


                    }
                    crep++;
                    if ((crep > 5) || (ret == 4))
                        break;
                    //  if ((ret == 4)) break;
                }
                Curr_number++;
                //s.Close();
                //s.Dispose();
                //
                
            //}
            //catch (Exception ex)
            //{
            //    ByteResult = 101;
            //    EventLog m_EventLog = new EventLog("");
            //    m_EventLog.Source = "FPIKSErrors";
            //    m_EventLog.WriteEntry("Ошибка в работе сервиса::working:" + ex.Message,
            //        EventLogEntryType.Warning);
            //}
            //finally
            //{
                
            //}
            return ((ret == 4));
        } // Старые обработки!!!!

        static IEnumerable<int> Search(byte[] source, byte[] pattern)
        {
            if (pattern.Length <= source.Length)
            {
                int[] mask = new int[pattern.Length + 1];

                int i = 0;
                int j = -1;
                mask[0] = -1;
                while (i < pattern.Length)
                {
                    while (j > -1 && pattern[i] != pattern[j])
                        j = mask[j];
                    i++;
                    j++;
                    mask[i] = j;
                }

                i = 0;
                j = 0;
                while (i < source.Length)
                {
                    while (j > -1 && pattern[j] != source[i])
                        j = mask[j];
                    i++;
                    j++;
                    if (j >= pattern.Length)
                    {
                        yield return (i - j);
                        j = mask[j];
                    }
                }
            }
        }

        bool GetFromBuffer(byte Command, int defaultSleep, out  List<byte> returnbytes, out byte _ByteStatus, out byte _ByteResult, out byte _ByteReserv)
        {
            //Thread.Sleep(defaultSleep);

            _ByteStatus=255;
            _ByteResult = 255;
            _ByteReserv = 255;

            IEnumerable<int> ibegin = null;
            IEnumerable<int> iend = null;

            for (int x = 1; x <= 4; x++)
            {
                ibegin = Search(bBuffer.ToArray(), new byte[] { DLE, STX });
                iend = Search(bBuffer.ToArray(), new byte[] { DLE, ETX });

                if ((bBuffer.Count == 0) || ((ibegin.Count() == 0) || (iend.Count() == 0)))
                    Thread.Sleep(x * defaultSleep);
                else
                    break;
            }
            returnbytes = new List<byte>();
            if ((ibegin.Count() == 0) || (iend.Count() == 0))
            {
                returnbytes.Clear();
                return false;
            }

            int IB = ibegin.First();
           
            int IE = iend.Last();
                       

            if ((IB == 0) || (IE == 0))
            {
                returnbytes.Clear();
                return false;
            }
            else if (IB > IE)
            {
                returnbytes.Clear();
                return false;
            }

            returnbytes = bBuffer.GetRange(IB + 2, IE - IB - 2);
            int sx=0;
            do
            {
                if ((returnbytes[sx] == DLE) && (returnbytes[sx + 1] == DLE))
                {
                    returnbytes.RemoveAt(sx);
                }
                sx++;
            } while (sx < returnbytes.Count);


            byte CheckSummGet = returnbytes[returnbytes.Count - 1];

            //returnbytes.RemoveAt(returnbytes.Count - 1); // Убираем checksum
            returnbytes.InsertRange(0, new byte[] { DLE, STX });
            returnbytes.AddRange(new byte[] { DLE, ETX });
            byte checksumm = getchecksum(returnbytes);
            if (CheckSummGet != checksumm)
            {
                returnbytes.Clear();
                return false;
            }
            returnbytes.RemoveAt(0); // Убираем DLE
            returnbytes.RemoveAt(0); //Убираем STX

            returnbytes.RemoveAt(returnbytes.Count - 1); // убираем последние DLE
            returnbytes.RemoveAt(returnbytes.Count - 1); // убираем последние ETX

            Curr_number = Convert.ToUInt32(returnbytes[0]) + 1;

            returnbytes.RemoveAt(0); // убираем порядковый номер команды
            returnbytes.RemoveAt(returnbytes.Count - 1);// убираем контральную сумму

            if (Command != returnbytes[0]) // Проверка команды
            {
                returnbytes.Clear();
                return false;
            }

            returnbytes.RemoveAt(0);
            _ByteStatus = returnbytes[0];
            this.ByteStatus = returnbytes[0];
            returnbytes.RemoveAt(0);

            _ByteResult = returnbytes[0]; 
            this.ByteResult = returnbytes[0];
            returnbytes.RemoveAt(0);

            _ByteReserv = returnbytes[0];
            this.ByteReserv = returnbytes[0];
            returnbytes.RemoveAt(0); //удаляем резерв
            //bBuffer.Clear();
            return true;

        }

        List<byte> ReturnResult(out bool ret, List<byte> CommandBytes = null, int wait = 100)
        {
            

            List<byte> rep = new List<byte>();
            byte getbytestatus, getbyteresult, getbytereserv;
            //int wait = 100;
            int count = 0;
            byte operation = CommandBytes[0];
            bool workOK;
            do
            {
                if (!s.IsOpen)
                {
                    workOK = false;
                    break;
                }
                byte[] Data_ = preparation(CommandBytes.ToArray());
                Thread.Sleep(40);
                bBuffer.Clear();
                s.Write(Data_, 0, Data_.Length);
                Thread.Sleep(40+wait);
                try
                {
                    workOK = GetFromBuffer(operation, wait, out rep, out getbytestatus, out getbyteresult, out getbytereserv);
                }
                catch (Exception ex)
                {
                    EventLog m_EventLog = new EventLog("");
                    m_EventLog.Source = "FPIKSErrors";
                    m_EventLog.WriteEntry("Ошибка в работе FPIKS::ReturnResult:" + ex.Message,
                    EventLogEntryType.Warning);
                    m_EventLog = null;
                    workOK = false;
                    count++;
                    Thread.Sleep(count * 1000);
                    continue;
                }
                if ((rep.Count == 0) && (workOK) && (getbytestatus == 255))
                    workOK = false;
                count++;
                try
                {
                    if (GetBit(getbytereserv, 2) == true)
                    {
                        workOK = false;
                        //break;
                    }
                    if (GetBit(getbytestatus, 0) == true)
                    {
                        workOK = false;
                        //break;
                    }
                    if (GetBit(getbytestatus, 7) == true)
                    {
                        workOK = false;
                        //break;
                    }
                }
                catch(Exception ex)
                {
                    EventLog m_EventLog = new EventLog("");
                    m_EventLog.Source = "FPIKSErrors";
                    m_EventLog.WriteEntry("Ошибка в работе FPIKS::ReturnResult:" + ex.Message,
                    EventLogEntryType.Warning);
                    m_EventLog = null;
                    workOK = false;
                }

                //wait = wait;
                if ((!workOK) && (count<3))
                    Thread.Sleep(200);
                else if ((!workOK) && (count>=3))
                    Thread.Sleep(count*1000); // спим при пустых ответах спим               
            } while ((!workOK) && (count < 10));

            this.LastCommand = operation;
            //_coding = null;
            this.bLastCommand = workOK;
            ret = workOK;
            return rep;
        }


        List<byte> ReturnResult(out bool ret, out byte _ByteStatus, out byte _ByteResult, out byte _ByteReserv, List<byte> CommandBytes = null, int wait = 100)
        {

            _ByteStatus = 255;
            _ByteResult = 255;
            _ByteReserv = 255;

            List<byte> rep = new List<byte>();
            byte getbytestatus, getbyteresult, getbytereserv;
            //int wait = 100;
            int count = 0;
            byte operation = CommandBytes[0];
            bool workOK;
            do
            {
                if (!s.IsOpen)
                {
                    workOK = false;
                    break;
                }
                byte[] Data_ = preparation(CommandBytes.ToArray());
                Thread.Sleep(40);
                bBuffer.Clear();
                s.Write(Data_, 0, Data_.Length);
                Thread.Sleep(40 + wait);
                try
                {
                    workOK = GetFromBuffer(operation, wait, out rep, out getbytestatus, out getbyteresult, out getbytereserv);
                    _ByteStatus = getbytestatus;
                    _ByteResult = getbyteresult;
                    _ByteReserv = getbytereserv;
                }
                catch (Exception ex)
                {
                    EventLog m_EventLog = new EventLog("");
                    m_EventLog.Source = "FPIKSErrors";
                    m_EventLog.WriteEntry("Ошибка в работе FPIKS::ReturnResult:" + ex.Message,
                    EventLogEntryType.Warning);
                    m_EventLog = null;
                    workOK = false;
                    count++;
                    Thread.Sleep(count * 1000);
                    continue;
                }
                if ((rep.Count == 0) && (workOK) && (getbytestatus == 255))
                    workOK = false;
                count++;

                if (GetBit(getbytereserv, 2) == true)
                {
                    workOK = false;
                    //break;
                }
                if (GetBit(getbytestatus, 0) == true)
                {
                    workOK = false;
                    //break;
                }
                if (GetBit(getbytestatus, 7) == true)
                {
                    workOK = false;
                    //break;
                }

                //wait = wait;
                if ((!workOK) && (count < 3))
                    Thread.Sleep(200);
                else if ((!workOK) && (count >= 3))
                    Thread.Sleep(count * 1000); // спим при пустых ответах спим               
            } while ((!workOK) && (count < 10));

            this.LastCommand = operation;
            //_coding = null;
            this.bLastCommand = workOK;
            ret = workOK;
            return rep;
        }



        byte BitArrayToByte(BitArray ba)
        {
            byte result = 0;
            for (byte index = 0, m = 1; index < 8; index++, m *= 2)
                result += ba.Get(index) ? m : (byte)0;
            return result;
        }

        public byte[] ToByteArray(BitArray bits)
        {
            int numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;

            byte[] bytes = new byte[numBytes];
            int byteIndex = 0, bitIndex = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            return bytes;
        }

        ///<summary>
        /// Возвращает бит в num байте val
        ///</summary>
        ///<param name="val">Входнойбайт</param>
        ///<param name="num">Номербита, начинаяс 0</param>
        ///<returns>true-битравен 1, false- битравен 0</returns>
        bool GetBit(byte val, int num)
        {
            if ((num > 7) || (num < 0))//Проверка входных данных
            {
                throw new ArgumentException();
            }
            return ((val >> num) & 1) > 0;//собственно все вычисления
        }

        ///<summary>
        /// Устанавливает значение определенного бита в байте
        ///</summary>
        ///<param name="val">Входнойбайт</param>
        ///<param name="num">Номербита</param>
        ///<param name="bit">Значениебита: true-битравен 1, false- битравен 0 </param>
        ///<returns>Байт, с измененным значением бита</returns>
        byte SetBit(byte val, int num, bool bit)
        {
            if ((num > 7) || (num < 0))//Проверка входных данных
            {
                throw new ArgumentException();
            }
            byte tmpval = 1;
            tmpval = (byte)(tmpval << num);//устанавливаем необходимый бит в единицу
            val = (byte)(val & (~tmpval));//сбрасываем в 0 необходимый бит

            if (bit)// если бит требуется установить в 1
            {
                val = (byte)(val | (tmpval));//то устанавливаем необходимый бит в 1
            }
            return val;
        }


        #endregion

        public bool FPInit(byte PortNumber, uint BaudRate, int ReadTimeOut, int WriteTimeOut)
        {
            bool IsOpen = false;
            try
            {
                
                s = null;
                s = new SerialPort("Com" + PortNumber.ToString(), (int)BaudRate, Parity.None, 8, StopBits.One);
                s.WriteTimeout = WriteTimeOut;
                s.ReadTimeout = ReadTimeOut;
                s.Open();
                IsOpen = s.IsOpen;
                s.DataReceived += new SerialDataReceivedEventHandler(seriport_DataAvailable);
                
                
                
            }
             catch (Exception ex)
            {
                s = null;
                ByteResult = 101;
                EventLog m_EventLog = new EventLog("");
                m_EventLog.Source = "FPIKSErrors";
                m_EventLog.WriteEntry("Ошибка в работе сервиса::FPInit:" + ex.Message+@"
                Port=" + PortNumber.ToString() + @"
                Open=" + IsOpen.ToString() + @"
                ",
                    EventLogEntryType.Warning);
                IsOpen = false;
            }
            finally
            {

            }
            return IsOpen;
        }

        public bool FPClose()
        {
            if ((s!=null)&&(s.IsOpen))
                s.Close();                            
            s = null;            
            return true;
        }

     
        void seriport_DataAvailable(object sender, EventArgs e)
        {
            while (((SerialPort)sender).BytesToRead > 0)
            {
                serialPortReceivedData = new byte[((SerialPort)sender).BytesToRead];
                ((SerialPort)sender).Read(serialPortReceivedData, 0, serialPortReceivedData.Length);
                bBuffer.AddRange(serialPortReceivedData);
                if (bBuffer[0] == bBuffer.Count)
                {
                    //send the response to the listeners
                    bBuffer.Clear();
                    break;
                }
            }
        }


        public bool FPOutOfCash(UInt32 Summa)
        {
            //byte[] b_avans = BitConverter.GetBytes(Summa);
            //byte[] strSend = new byte[5];
            //strSend[0] = 24; //Code AVANS                   
            //strSend[1] = b_avans[0]; //Code                    
            //strSend[2] = b_avans[1]; //Code
            //strSend[3] = b_avans[2]; //Code
            //strSend[4] = b_avans[3]; //Code

            //strSend = preparation(strSend);
            //bool ret = working(strSend);
            //b_avans = null;
            //strSend = null;
            //return ret && bLastCommand;

            List<byte> _coding = new List<byte>();
            _coding.Add(24);
            _coding.AddRange(BitConverter.GetBytes(Summa));
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding);
            return resultcommand;
        }

        public bool FPInToCash(UInt32 Summa)
        {
            //byte[] b_avans = BitConverter.GetBytes(Summa);
            //byte[] strSend = new byte[5];
            //strSend[0] = 16; //Code AVANS                   
            //strSend[1] = b_avans[0]; //Code                    
            //strSend[2] = b_avans[1]; //Code
            //strSend[3] = b_avans[2]; //Code
            //strSend[4] = b_avans[3]; //Code

            //strSend = preparation(strSend);

            //bool ret = working(strSend);
            //strSend = null;
            //b_avans = null;
            //return ret && bLastCommand;
            List<byte> _coding = new List<byte>();
            _coding.Add(16);
            _coding.AddRange(BitConverter.GetBytes(Summa));
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding);
            return resultcommand;
        }

        public DateTime CurrentDateTime
        {
            get
            {
                List<byte> _coding = new List<byte>();
                _coding.Add(1);
                bool resultcommand1;
                List<byte> rep = ReturnResult(out resultcommand1, _coding);

                string hexday = rep[0].ToString("X");
                int _day = Convert.ToInt16(hexday);

                string hexmonth = rep[1].ToString("X");
                int _month = Convert.ToInt16(hexmonth);

                string hexyear = rep[2].ToString("X");
                int _year = Convert.ToInt16(hexyear);
               // _coding = null; rep = null;

                _coding = new List<byte>();
                _coding.Add(3);
                bool resultcommand2;
                rep = ReturnResult(out resultcommand2, _coding);

                string hexhour = rep[0].ToString("X");
                int _hour = Convert.ToInt16(hexhour);

                string hexminute = rep[1].ToString("X");
                int _minute = Convert.ToInt16(hexminute);

                string hexsecond = rep[2].ToString("X");
                int _second = Convert.ToInt16(hexsecond);

                _coding = null; rep = null;

                return new DateTime(2000 + _year, _month, _day, _hour, _minute, _second);
            }
        }

        

        public string CurrentDate
        {

              get
            {               
               List<byte> _coding = new List<byte>();
               _coding.Add(1);
               bool resultcommand;
               List<byte> rep = ReturnResult(out resultcommand,_coding);
                if(!resultcommand)
                    return new DateTime(2000, 1, 1).ToString("dd.MM.yyyy");
                string hexday = rep[0].ToString("X");
                int _day = Convert.ToInt16(hexday);

                string hexmonth = rep[1].ToString("X");
                int _month = Convert.ToInt16(hexmonth);

                string hexyear = rep[2].ToString("X");
                int _year = Convert.ToInt16(hexyear);
                rep = null;
                return new DateTime(2000 + _year, _month, _day).ToString("dd.MM.yyyy");
            }
            

           

        }

        public string CurrentTime
        {

               get
            {

                List<byte> _coding = new List<byte>();
                _coding.Add(3);
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding);
                if(!resultcommand)
                    return new DateTime(2000, 1, 1, 0, 0, 0).ToString("hh:mm:ss");

                string hexhour = rep[0].ToString("X");
                int _hour = Convert.ToInt16(hexhour);

                string hexminute = rep[1].ToString("X");
                int _minute = Convert.ToInt16(hexminute);

                string hexsecond = rep[2].ToString("X");
                int _second = Convert.ToInt16(hexsecond);

                _coding = null; rep = null;
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, _hour, _minute, _second).ToString("hh:mm:ss");

  
            }

  
        }

        public bool SetDateTime(DateTime SetDayTimeInfo)
        {
            List<byte> _coding = new List<byte>();
            List<byte> rep = new List<byte>();
            bool resultcommand;
            byte operation = 2;
            _coding.Add(operation); //SetDate
            _coding.Add(Convert.ToByte(Convert.ToInt32(SetDayTimeInfo.ToString("dd"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(SetDayTimeInfo.ToString("MM"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(SetDayTimeInfo.ToString("yy"), 16)));

            rep = ReturnResult(out resultcommand, _coding, 100);

            bool InfoDate = resultcommand;
            
            

            _coding = new List<byte>();
            operation = 4;
            _coding.Add(operation); //SetTime
            _coding.Add(Convert.ToByte(Convert.ToInt32(SetDayTimeInfo.ToString("HH"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(SetDayTimeInfo.ToString("mm"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(SetDayTimeInfo.ToString("ss"), 16)));

            //wait = 100;
            //count = 0;
            //workOK = false;
            //while (!workOK)
            //{
            //    byte[] Data_ = preparation(_coding.ToArray());
            //    s.Write(Data_, 0, Data_.Length);


            //    workOK = GetFromBuffer(operation, wait, out rep);
            //    wait = wait * 2;
            //    count++;

            //}
            rep = ReturnResult(out resultcommand, _coding, 100);

            bool InfoTime = resultcommand;
            

            return InfoDate && InfoTime;
        }

        public int? GetNumZReport
        {
            get
            {
                Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0037));
                _coding.AddRange(new byte [] {0x37,0x00});
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                _coding.Add(16);
                _coding.Add(2);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                _coding = null;
                return BitConverter.ToUInt16(rep.ToArray(), 0) - 1;
                
            }
        }

        public UInt16 getBeginKLEF
        {
            get
            {
                Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0037));
                _coding.AddRange(new byte[] { 0x39, 0x00 });                
                _coding.Add(16);
                _coding.Add(2);
                

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                _coding = null;
                return BitConverter.ToUInt16(rep.ToArray(),0);//BitConverter.ToUInt16(rep.ToArray(), 0) - 1;

            }
        }

        public UInt16 getEndKLEF
        {
            get
            {
                Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0037));
                _coding.AddRange(new byte[] { 0x3C, 0x00 });
                _coding.Add(16);
                _coding.Add(2);


                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                _coding = null;
                return BitConverter.ToUInt16(rep.ToArray(), 0);//BitConverter.ToUInt16(rep.ToArray(), 0) - 1;

            }
        }

        

        //public string getKlefInfo
        //{
        //    get
        //    {
        //        Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 
        //        List<byte> _coding = new List<byte>();
        //        _coding.Add(51); //GetMemory
        //        //_coding.AddRange(BitConverter.GetBytes(0x0037));
        //        _coding.AddRange(new byte[] { 0x3C, 0x00 });
        //        _coding.Add(16);
        //        _coding.Add(2);


        //        bool resultcommand;
        //        List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

        //        _coding = null;
        //        return "";//;//BitConverter.ToUInt16(rep.ToArray(), 0) - 1;

        //    }
        //}


        public string GetLastDateZReport
        {
            get
            {
                //Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 
                //List<byte> _coding = new List<byte>();
                //_coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0169));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                //_coding.Add(16);
                //_coding.Add(3);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //if (ret.Length == 0)
                //    return "";
                //else
                //{
                //    string hexday = ret[0].ToString("X");
                //    int _day = Convert.ToInt16(hexday);

                //    string hexmonth = ret[1].ToString("X");
                //    int _month = Convert.ToInt16(hexmonth);

                //    string hexyear = ret[2].ToString("X");
                //    int _year = Convert.ToInt16(hexyear);
                //    if ((_day == 0) || (_month == 0) || (_year == 0))
                //        return "";
                //    else
                //        return new DateTime(_year, _month, _day).ToString("dd.MM.yyyy");
                //}

                Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory                
                _coding.AddRange(new byte[] { 0x69, 0x01 });                
                _coding.Add(16);
                _coding.Add(3);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                _coding = null;
                return new DateTime(Convert.ToInt16(rep[2].ToString("X")), Convert.ToInt16(rep[1].ToString("X")), Convert.ToInt16(rep[0].ToString("X"))).ToString("dd.MM.yyyy"); ;

            }
        }

        public Int32[] GetTurnCurrentCheckTax
        {
            get
            {
                Int32[] Ret = new Int32[6];
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int o = 0; o < 6; o++)
                //{
                //    for (int x = 0; x < 4; x++)
                //    {
                //        A[x] = ret[o + x];
                //    }
                //    Ret[o] = BitConverter.ToInt32(A, 0);
                //}
                //return Ret;
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                
                for (int o = 0; o < 6; o++)
                {
                    Ret[o] = BitConverter.ToInt32(rep.GetRange(o * 4, 4).ToArray(), 0);
                }

                _coding = null;
                return Ret;


            }
        }

        public Int32 GetTurnCurrentCheckTax_A
        {
            get
            {                
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int x = 0; x < 4; x++)
                //    A[x] = ret[x];
                //return BitConverter.ToInt16(A, 0);
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
                _coding = null;
                return BitConverter.ToInt32(rep.GetRange(0, 4).ToArray(), 0);

            }
        }

        public Int32 GetTurnCurrentCheckTax_B
        {
            get
            {
                //List<byte> _coding = new List<byte>();
                //_coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                //_coding.Add(16);
                //_coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int x = 0; x < 4; x++)
                //    A[x] = ret[4+x];
                //return BitConverter.ToInt16(A, 0);

                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                
                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);
                
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
                _coding = null;
                return BitConverter.ToInt32(rep.GetRange(4, 4).ToArray(), 0);

            }
        }

        public Int32 GetTurnCurrentCheckTax_C
        {
            get
            {
                //List<byte> _coding = new List<byte>();
                //_coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                //_coding.Add(16);
                //_coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int x = 0; x < 4; x++)
                //    A[x] = ret[8 + x];
                //return BitConverter.ToInt16(A, 0);

                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory

                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
                _coding = null;
                return BitConverter.ToInt32(rep.GetRange(8, 4).ToArray(), 0);

            }
        }

        public Int32 GetTurnCurrentCheckTax_D
        {
            get
            {
                //List<byte> _coding = new List<byte>();
                //_coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                //_coding.Add(16);
                //_coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int x = 0; x < 4; x++)
                //    A[x] = ret[12 + x];
                //return BitConverter.ToInt16(A, 0);

                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory

                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
                _coding = null;
                return BitConverter.ToInt32(rep.GetRange(12, 4).ToArray(), 0);

            }
        }

        public Int32 GetTurnCurrentCheckTax_E
        {
            get
            {
                //List<byte> _coding = new List<byte>();
                //_coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                //_coding.Add(16);
                //_coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int x = 0; x < 4; x++)
                //    A[x] = ret[16 + x];
                //return BitConverter.ToInt16(A, 0);

                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory

                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
                _coding = null;
                return BitConverter.ToInt32(rep.GetRange(16, 4).ToArray(), 0);

            }
        }

        public Int32 GetTurnCurrentCheckTax_F
        {
            get
            {
                //List<byte> _coding = new List<byte>();
                //_coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x0100));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                //_coding.Add(16);
                //_coding.Add(24);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //byte[] A = new byte[4];
                //for (int x = 0; x < 4; x++)
                //    A[x] = ret[20 + x];
                //return BitConverter.ToInt16(A, 0);
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory

                _coding.AddRange(new byte[] { 0x00, 0x01 });
                _coding.Add(16);
                _coding.Add(24);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
                _coding = null;
                return BitConverter.ToInt32(rep.GetRange(20, 4).ToArray(), 0);

            }
        }

        public int GetSaleCheckNumber
        {
            get
            {
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x301B));
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                _coding.AddRange(new byte[] { 0x1B, 0x30 });
                _coding.Add(16); //старница
                _coding.Add(2);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //strSend = null; _coding = null;
                //return BitConverter.ToUInt16(ret, 0);
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                _coding = null;
                return BitConverter.ToUInt16(rep.ToArray(), 0);
            }
        }

        public int GetPayCheckNumber
        {
            get
            {
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                //_coding.AddRange(BitConverter.GetBytes(0x30AB));
                _coding.AddRange(new byte[] {0x77,0x30});
                //_coding.RemoveAt(_coding.Count - 1); // убиваем 2 лишних
                //_coding.RemoveAt(_coding.Count - 1);
                _coding.Add(16); //старница
                _coding.Add(2);
                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                 _coding = null;
                return BitConverter.ToUInt16(rep.ToArray(), 0);
            }
        }

        public bool GetFPCplCutter
        {
            //Запрет обрезчика
            get
            {
                List<byte> _coding = new List<byte>();
                _coding.Add(28); //GetMemory
                
                _coding.AddRange(new byte[] { 0x1A, 0x30 });
                
                _coding.Add(16); //старница
                _coding.Add(1); //размер блока
                

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 100);

                _coding = null;
                return GetBit(rep.ToArray()[0], 3);
                //0 = Нет
                //1 = Запрещен
            }
        }

        //public string GetMoneyInBox
        //{
            
        //    get
        //    {

        //        List<byte> _coding = new List<byte>();
        //        _coding.Add(33);
        //        bool resultcommand;
        //        List<byte> rep = ReturnResult(out resultcommand, _coding);
        //        return BitConverter.ToUInt32(rep.ToArray(), 0).ToString();

        //        //UInt32 Return = 0;
        //        //List<byte> _coding = new List<byte>();
        //        //_coding.Add(33); //GetBox
                
        //        //byte[] strSend = preparation(_coding.ToArray());
        //        //byte[] ret;
                
        //        //bLastCommand = working(strSend, out ret);
                
        //        //if (ret.Length < 4) // если не равно 4 запросим еще раз
        //        //{
        //        //    Thread.Sleep(3000);
        //        //    bLastCommand = working(strSend, out ret);
        //        //    if (ret.Length >= 4)
        //        //        Return = BitConverter.ToUInt32(ret, 0);
        //        //    //else
        //        //      //  return "";
        //        //}
        //        //else
        //        //    Return = BitConverter.ToUInt32(ret, 0);
        //        //strSend = null; _coding = null; ret = null;
        //        //return Return.ToString();
        //    }
        //}

        public UInt32 GetMoneyInBox
        {

            get
            {

                List<byte> _coding = new List<byte>();
                _coding.Add(33);
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding);
                try
                {
                    return BitConverter.ToUInt32(rep.ToArray(), 0);
                }
                catch(Exception ex)
                {
                    EventLog m_EventLog = new EventLog("");
                    m_EventLog.Source = "FPIKSErrors";
                    m_EventLog.WriteEntry("Ошибка в работе FPIKS::GetMoneyInBox:" + ex.Message,
                    EventLogEntryType.Warning);
                    m_EventLog = null;
                    return 0;
                }

                //UInt32 Return = 0;
                //List<byte> _coding = new List<byte>();
                //_coding.Add(33); //GetBox

                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;

                //bLastCommand = working(strSend, out ret);

                //if (ret.Length < 4) // если не равно 4 запросим еще раз
                //{
                //    Thread.Sleep(3000);
                //    bLastCommand = working(strSend, out ret);
                //    if (ret.Length >= 4)
                //        Return = BitConverter.ToUInt32(ret, 0);
                //    //else
                //      //  return "";
                //}
                //else
                //    Return = BitConverter.ToUInt32(ret, 0);
                //strSend = null; _coding = null; ret = null;
                //return Return.ToString();
            }
        }

        public string GetInfoStatus
        {
            get
            {
                string sReturn="";
                if (GetBit(GetByteStatus, 0) == true)
                    sReturn = sReturn + "принтер не готов;";
                if (GetBit(GetByteStatus, 1) == true)
                    sReturn = sReturn + "превышение продолжительности хранения данных в КЛЕФ;";
                if (GetBit(GetByteStatus, 2) == true)
                    sReturn = sReturn + "ошибка или переполнение фискальной памяти;";
                if (GetBit(GetByteStatus, 3) == true)
                    sReturn = sReturn + "неправильная дата или ошибка часов;";
                if (GetBit(GetByteStatus, 4) == true)
                    sReturn = sReturn + "ошибка индикатора;";
                if (GetBit(GetByteStatus, 5) == true)
                    sReturn = sReturn + "превышение продолжительности смены;";
                if (GetBit(GetByteStatus, 6) == true)
                    sReturn = sReturn + "снижение рабочего напряжения питания;";
                if (GetBit(GetByteStatus, 7) == true)
                    sReturn = sReturn + "команда не существует или запрещена в данном режиме;";
                return sReturn;
            }
        }

        public string GetInfoReserv
        {
            get
            {
                string sReturn = "";
                if (GetBit(GetByteReserv, 0) == true)
                    sReturn = sReturn + "открыт чек служебного отчета;";
                if (GetBit(GetByteReserv, 1) == true)
                    sReturn = sReturn + "состояние аварии (команда завершится после устранения ошибки);";
                if (GetBit(GetByteReserv, 2) == true)
                    sReturn = sReturn + "отсутствие бумаги, если принтер не готов;";
                if (GetBit(GetByteReserv, 3) == true)
                    sReturn = sReturn + "чек: продажи/выплаты (0/1);";
                if (GetBit(GetByteReserv, 4) == true)
                    sReturn = sReturn + "принтер фискализирован;";
                if (GetBit(GetByteReserv, 5) == true)
                    sReturn = sReturn + "смена открыта;";
                if (GetBit(GetByteReserv, 6) == true)
                    sReturn = sReturn + "открыт чек;";
                if (GetBit(GetByteReserv, 7) == true)
                    sReturn = sReturn + "ЭККР не персонализирован;";
                return sReturn;
            }
        }

        public string GetPapStat
        {

            get
            {
                string Return = "";
                List<byte> _coding = new List<byte>();
                _coding.Add(48); //GetPapStat

                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding);

                if ((!resultcommand)&&(rep.Count==0))
                    return "Ошибка чтения статуса";
                //if (ret.Length != 1) 
                //{
                //    Thread.Sleep(3000);
                //    bLastCommand = working(strSend, out ret);
                //   // if (ret.Length >= 1)
                //        //Return = BitConverter.ToUInt32(ret, 0);
                //    //else
                //    //  return "";
                //}
                byte read = rep[0];
                if (GetBit(read, 0) == true)
                    Return += "ошибка связи с принтером,";
                if (GetBit(read, 2) == true)
                    Return += "контрольная лента почти заканчивается,";
                if (GetBit(read, 3) == true)
                    Return += "чековая лента почти заканчивается,";
                if (GetBit(read, 5) == true)
                    Return += "контрольная лента закончилась,";
                if (GetBit(read, 5) == true)
                    Return += "чековая лента закончилась,";

                if (Return.Count()!=0)
                    Return = Return + "@";

                Return.Replace(",@", "");
                _coding = null; //ret = null;
                return Return.ToString();
            }
        }



        public bool FPGetTaxRates
        {
            get
            {
                List<byte> _coding = new List<byte>();
                _coding.Add(44); //GetCheckSums

                
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 240);
                if (!resultcommand)
                    return false;


                byte TexCount = rep[0];

                string hexday = rep[1].ToString("X");
                int _day = Convert.ToInt16(hexday);

                string hexmonth = rep[2].ToString("X");
                int _month = Convert.ToInt16(hexmonth);

                string hexyear = rep[3].ToString("X");
                int _year = Convert.ToInt16(hexyear);
                Int32[] MasTax = new Int32[TexCount];
                for (byte x = 1; x <= TexCount; x++)
                {
                    MasTax[x-1] = BitConverter.ToInt32(rep.GetRange(4+(x*2-2), 2).ToArray(), 0);
                }
                //UInt32 Tax_B = BitConverter.ToUInt32(rep.GetRange(4, 4).ToArray(), 0);
                //UInt32 Tax_C = BitConverter.ToUInt32(rep.GetRange(8, 4).ToArray(), 0);
                //UInt32 Tax_D = BitConverter.ToUInt32(rep.GetRange(12, 4).ToArray(), 0);
                //UInt32 Tax_E = BitConverter.ToUInt32(rep.GetRange(16, 4).ToArray(), 0);
                //UInt32 Tax_F = BitConverter.ToUInt32(rep.GetRange(20, 4).ToArray(), 0);

                //UInt32 Sum_1 = BitConverter.ToUInt32(rep.GetRange(24, 4).ToArray(), 0);
                //UInt32 Sum_2 = BitConverter.ToUInt32(rep.GetRange(28, 4).ToArray(), 0);
                //UInt32 Sum_3 = BitConverter.ToUInt32(rep.GetRange(32, 4).ToArray(), 0);
                //UInt32 Sum_4 = BitConverter.ToUInt32(rep.GetRange(36, 4).ToArray(), 0);
                //UInt32 Sum_5 = BitConverter.ToUInt32(rep.GetRange(40, 4).ToArray(), 0);
                //UInt32 Sum_6 = BitConverter.ToUInt32(rep.GetRange(44, 4).ToArray(), 0);
                //UInt32 Sum_7 = BitConverter.ToUInt32(rep.GetRange(48, 4).ToArray(), 0);
                //UInt32 Sum_8 = BitConverter.ToUInt32(rep.GetRange(52, 4).ToArray(), 0);
                //UInt32 Sum_9 = BitConverter.ToUInt32(rep.GetRange(56, 4).ToArray(), 0);
                //UInt32 Sum_10 = BitConverter.ToUInt32(rep.GetRange(60, 4).ToArray(), 0);


                rep = null; _coding = null;
                //return Sum_1 + Sum_2 + Sum_3 + Sum_4 + Sum_5 + Sum_6 + Sum_7 + Sum_8 + Sum_9 + Sum_10;
                return true;
            }
        }


        public UInt32 GetCheckTotal
        {
            get
            {
                List<byte> _coding = new List<byte>();
                _coding.Add(43); //GetCheckSums

                //byte[] strSend = preparation(_coding.ToArray());
                //byte[] ret;
                //bLastCommand = working(strSend, out ret);
                //if (ret.Length <= 4)
                //    return 0;
                //Int64[] Checksum = new Int64[6];
                //byte[] sum;
                //for (int x = 0; x < 6; x++)
                //{
                //     sum= new byte[4];
                //    for (int y = 0; y <= 3; y++)
                //    {
                //       sum[y]  = ret[y + x*4];
                //    }
                //    Checksum[x] = BitConverter.ToUInt32(sum,0);
                //}
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 240);
                if (!resultcommand)
                    return 0;
                UInt32 Tax_A = BitConverter.ToUInt32(rep.GetRange(0, 4).ToArray(), 0);
                UInt32 Tax_B = BitConverter.ToUInt32(rep.GetRange(4, 4).ToArray(), 0);
                UInt32 Tax_C = BitConverter.ToUInt32(rep.GetRange(8, 4).ToArray(), 0);
                UInt32 Tax_D = BitConverter.ToUInt32(rep.GetRange(12, 4).ToArray(), 0);
                UInt32 Tax_E = BitConverter.ToUInt32(rep.GetRange(16, 4).ToArray(), 0);
                UInt32 Tax_F = BitConverter.ToUInt32(rep.GetRange(20, 4).ToArray(), 0);

                UInt32 Sum_1 = BitConverter.ToUInt32(rep.GetRange(24, 4).ToArray(), 0);
                UInt32 Sum_2 = BitConverter.ToUInt32(rep.GetRange(28, 4).ToArray(), 0);
                UInt32 Sum_3 = BitConverter.ToUInt32(rep.GetRange(32, 4).ToArray(), 0);
                UInt32 Sum_4 = BitConverter.ToUInt32(rep.GetRange(36, 4).ToArray(), 0);
                UInt32 Sum_5 = BitConverter.ToUInt32(rep.GetRange(40, 4).ToArray(), 0);
                UInt32 Sum_6 = BitConverter.ToUInt32(rep.GetRange(44, 4).ToArray(), 0);
                UInt32 Sum_7 = BitConverter.ToUInt32(rep.GetRange(48, 4).ToArray(), 0);
                UInt32 Sum_8 = BitConverter.ToUInt32(rep.GetRange(52, 4).ToArray(), 0);
                UInt32 Sum_9 = BitConverter.ToUInt32(rep.GetRange(56, 4).ToArray(), 0);
                UInt32 Sum_10 = BitConverter.ToUInt32(rep.GetRange(60, 4).ToArray(), 0);

                
                rep = null; _coding = null; 
                //return Sum_1 + Sum_2 + Sum_3 + Sum_4 + Sum_5 + Sum_6 + Sum_7 + Sum_8 + Sum_9 + Sum_10;
                return Tax_A + Tax_B + Tax_C + Tax_D + Tax_E + Tax_F;
            }
        }

        public bool FPLineFeed()
        {

            //byte[] strSend = new byte[1];
            //strSend[0] = 14; //Code LineFeed                   
            List<byte> _coding = new List<byte>();
            _coding.Add(14);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
            return resultcommand;
        }

        public bool FPCplCutter() //Функция отключает либо включает работу обрезчика чековой ленты в зависимости от его текущего состояния. 
        {                   
            List<byte> _coding = new List<byte>();
            _coding.Add(46);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
            return resultcommand;
        }


        public bool FPResetOrder()
        {

            //byte[] strSend = new byte[1];
            //strSend[0] = 15; //Code ResetOrder                   
            List<byte> _coding = new List<byte>();
            _coding.Add(15);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 100);
            rep = null;
            return resultcommand;
        }

        public bool FPNullCheck()
        {

            Encoding cp866 = Encoding.GetEncoding(866);
            List<byte> _coding = new List<byte>();
            List<byte> rep = new List<byte>();
            string sinfo = "Нульовий чек";
            byte[] strinfo = cp866.GetBytes(sinfo);
            BitArray len = new BitArray(new byte[] { (byte)sinfo.Length });          

            //len[7] = true; // Если бит 7 длины строки равен единице (1) при первой регистрации в чеке, то открывается чек 
            //выплат, иначе будет открыт чек продаж. Открыв чек комментарием (например строкой “НУЛЕВОЙ 
            //ЧЕК”) и закрыв его командой20, можно напечатать нулевой чек.
            _coding.Clear();
            byte operation = 11;
            _coding.Add(operation);
            _coding.Add(BitArrayToByte(len));
            
            _coding.AddRange(strinfo);

            bool command1;
            rep = ReturnResult(out command1, _coding, 100);
            

            //byte[] strSend = preparation(_coding.ToArray());
            //bool firstcomm = working(strSend);
            _coding.Clear();
            operation = 20;

            _coding.Add(operation); //регистрация оплаты и печать чека, если сума оплат не меньше суммы продаж                   
            _coding.Add(0x03); //тип оплаты                               
            _coding.AddRange(BitConverter.GetBytes(0 ^ (1 << 31)));


            bool command2;
            rep = ReturnResult(out command2, _coding, 100);
            _coding = null; cp866 = null;
            return command1 && command2;
        }

        public bool FPComment(string Comment, bool OpenPayCheck=false)
        {

            Encoding cp866 = Encoding.GetEncoding(866);
            List<byte> _coding = new List<byte>();
            string PComment = Comment.Substring(0, Math.Min(Comment.Length, 27));
            byte[] strinfo = cp866.GetBytes(PComment);
            

            //len[7] = true; // Если бит 7 длины строки равен единице (1) при первой регистрации в чеке, то открывается чек 
            //выплат, иначе будет открыт чек продаж. Открыв чек комментарием (например строкой “НУЛЕВОЙ 
            //ЧЕК”) и закрыв его командой20, можно напечатать нулевой чек.
            int len = PComment.Length;
            _coding.Add(11);
            if (OpenPayCheck)
                len = (byte)(len ^ (1 << 7));//_amount_status[7] = true;
            _coding.Add((byte)len);

            _coding.AddRange(strinfo);



            //byte[] strSend = preparation(_coding.ToArray());
            //bool firstcomm = working(strSend);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 240);

            rep = null; _coding = null; cp866 = null;
            return resultcommand;
        }

        public bool FPSetCashier(byte Num_Cashier, string Name_Cashier, ushort Pass_Cashier, bool TakeProgName)
        {
            Encoding cp866 = Encoding.GetEncoding(866);
            List<byte> _coding = new List<byte>();

            byte[] strinfo = cp866.GetBytes(Name_Cashier.Substring(0,Math.Min(15,Name_Cashier.Length)));
            //byte[] strinfo = System.GetEncoding(866).GetBytes(Name_Cashier);

            byte[] b_pass = BitConverter.GetBytes(Pass_Cashier);

            //byte[] strSend = new byte[5];
            _coding.Add(6); //Code SetCashier                   
            _coding.Add(b_pass[0]); //Code                    
            _coding.Add(b_pass[1]); //Code
            _coding.Add(Num_Cashier);
            BitArray len = new BitArray(new byte[] { (byte)strinfo.Length });
            if (TakeProgName)
                _coding.Add(255);
            else
                _coding.Add(BitArrayToByte(len));
            _coding.AddRange(strinfo);

            //byte[] strSend = preparation(_coding.ToArray());

            //bool Rets = working(strSend);

            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 240);

            rep = null; _coding = null; cp866 = null; len = null; strinfo = null; b_pass = null;
            return resultcommand;
        }

        public bool FPPayMoneyEx(uint Amount, byte Amount_Status, bool IsOneQuant, int Price, ushort NalogGroup, bool MemoryGoodName, string GoodName, string StrCode)
        {

            Encoding cp866 = Encoding.GetEncoding(866);
            List<byte> _coding = new List<byte>();

            StrCode = PrepareString(StrCode);

            string _GoodName = PrepareString(GoodName);//Encoding.Convert(Encoding.GetEncoding("cp1251"), Encoding.GetEncoding("cp866"),new byte [] GoodName.ToCharArray());
            //byte[] b_amount= BitConverter.GetBytes(Amount);

            

            //byte[] strSend = new byte[5];
            _coding.Add(8); //Code Sale                   

            //BitArray _amount = new BitArray(BitConverter.GetBytes(Amount));

            _coding.AddRange(BitConverter.GetBytes(Amount));
            _coding.RemoveAt(_coding.Count - 1); // так как передается только 3 значения, последнее киляем
            //
            //статус (
            //биты 0..3 -число десятичных разрядов в количестве, 
            //бит 7=1 –количество 1 не печатается в чеке)

            //BitArray _amount_status = new BitArray(new byte[] { Amount_Status });

            if (IsOneQuant)
                Amount_Status = (byte)((int)Amount_Status ^ (1 << 7));//_amount_status[7] = true;
            _coding.Add(Amount_Status);

            //цена в коп (бит 31 = 1 –отрицательная цена)
            int _price = Price;
            //BitArray b_price = new BitArray(BitConverter.GetBytes(_price));
            if (Price < 0)
            {
                _price = -_price;
                _price = _price ^ (1 << 31);
            }
            //b_price[31] = true;
            _coding.AddRange(BitConverter.GetBytes(_price));


            //налоговая группа             
            if (NalogGroup==0)
                _coding.Add(0x80);
            else if (NalogGroup == 1)
                _coding.Add(0x81);
            else if (NalogGroup == 2)
                _coding.Add(0x82);
            else if (NalogGroup == 3)
                _coding.Add(0x83);
            else if (NalogGroup == 4)
                _coding.Add(0x84);
            else if (NalogGroup == 5)
                _coding.Add(0x85);

            byte[] strinfo = cp866.GetBytes(_GoodName.Substring(0, Math.Min(70, _GoodName.Length)));
            //длина названия товара или услуги (= n )
            //(n=255 –название взять из памяти)
            //BitArray len = new BitArray(new byte[] { (byte)strinfo.Length });
            if (MemoryGoodName)
                _coding.Add(255);
            else
                _coding.Add((byte)strinfo.Length); // _coding.Add(BitArrayToByte(len));
            //название товара или услуги (для n # 255) 
            _coding.AddRange(strinfo);


            //код товара
            byte[] _strcode = cp866.GetBytes(StrCode.Substring(0,Math.Min(6,StrCode.Length)));
            _coding.AddRange(_strcode);
            for (int x = _strcode.Length; x <= 6; x++)
                _coding.Add(0);

            //byte[] strSend = preparation(_coding.ToArray());

            //bool Rets = working(strSend);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 240);            
            rep = null; _strcode = null; cp866 = null; _coding = null;
            return resultcommand;
        }

        string PrepareString(string forwork)
        {
            string _fo = forwork.Replace("№","N");
            //_fo = forwork.Replace("О", "О");
            //_fo = forwork.Replace("95111-Вода минеральная Оболонская 2л (Оболонь)", "95111-555");
            return _fo;
        }

        public bool FPSaleEx(UInt32 Amount, byte Amount_Status, bool IsOneQuant, Int32 Price, ushort NalogGroup, bool MemoryGoodName, string GoodName, string StrCode)
        {

            Encoding cp866 = Encoding.GetEncoding(866);
            List<byte> _coding = new List<byte>();

            string _GoodName = PrepareString(GoodName);//Encoding.Convert(Encoding.GetEncoding("cp1251"), Encoding.GetEncoding("cp866"),new byte [] GoodName.ToCharArray());
            //if (_GoodName != "95111-Вода минеральная Оболонская 2л (Оболонь)")
                //return true;
            //_GoodName = _GoodName.Substring(6, 5);
            //StrCode = "1";
            //byte[] b_amount= BitConverter.GetBytes(Amount);



            //byte[] strSend = new byte[5];
            _coding.Add(18); //Code Sale                   

            //BitArray _amount = new BitArray(BitConverter.GetBytes(Amount));

            _coding.AddRange(BitConverter.GetBytes(Amount));
            _coding.RemoveAt(_coding.Count - 1); // так как передается только 3 значения, последнее киляем
            //
            //статус (
            //биты 0..3 -число десятичных разрядов в количестве, 
            //бит 7=1 –количество 1 не печатается в чеке)

            //BitArray _amount_status = new BitArray(new byte[] { Amount_Status });

            if (IsOneQuant)
                Amount_Status = (byte)((int)Amount_Status ^ (1 << 7));//_amount_status[7] = true;
            _coding.Add(Amount_Status);

            //цена в коп (бит 31 = 1 –отрицательная цена)
            int _price = Price;
            //BitArray b_price = new BitArray(BitConverter.GetBytes(_price));
            if (Price < 0)
            {
                _price = -_price;
                _price = _price ^ (1 << 31);
            }
            //b_price[31] = true;
            _coding.AddRange(BitConverter.GetBytes(_price));

            //налоговая группа             
            if (NalogGroup == 0)
                _coding.Add(0x80);
            else if (NalogGroup == 1)
                _coding.Add(0x81);
            else if (NalogGroup == 2)
                _coding.Add(0x82);
            else if (NalogGroup == 3)
                _coding.Add(0x83);
            else if (NalogGroup == 4)
                _coding.Add(0x84);
            else if (NalogGroup == 5)
                _coding.Add(0x85);

            byte[] strinfo = cp866.GetBytes(_GoodName.Substring(0, Math.Min(70, _GoodName.Length)));
            //длина названия товара или услуги (= n )
            //(n=255 –название взять из памяти)
            //BitArray len = new BitArray(new byte[] { (byte)strinfo.Length });
            if (MemoryGoodName)
                _coding.Add(255);
            else
                _coding.Add((byte)strinfo.Length); // _coding.Add(BitArrayToByte(len));
            //название товара или услуги (для n # 255) 
            _coding.AddRange(strinfo);


            //код товара
            //byte[] _strcode = cp866.GetBytes(StrCode.Substring(0, Math.Min(6, StrCode.Length)));
            UInt64 __code = Convert.ToUInt64(StrCode);
            //_coding.AddRange(_strcode);
            //for (int x = _strcode.Length+1; x <= 6; x++)
              //  _coding.Add(0);
            _coding.AddRange(BitConverter.GetBytes(__code));
            _coding.RemoveAt(_coding.Count - 1);
            _coding.RemoveAt(_coding.Count - 1);

            //byte[] strSend = preparation(_coding.ToArray());

            //bool ret =working(strSend);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 240);
            rep = null; _coding = null; cp866 = null;

            return resultcommand;
        }

        public bool FPPayment(byte Payment_Status, UInt32 Payment, bool CheckClose, bool FiscStatus, string Comment)
        {

            Encoding cp866 = Encoding.GetEncoding(866);
            List<byte> _coding = new List<byte>();

            Comment = PrepareString(Comment);
            
            _coding.Add(20); //Code Payment                   
            //_coding.Add(Payment_Status); //тип оплаты                   
            //BitArray _Payment_Status = new BitArray(new byte[] { Payment_Status });
            //int _Payment_Status = Convert.ToInt16(Payment_Status);
            if (!FiscStatus)
                //_Payment_Status[6] = true;
               // _Payment_Status = (int)Payment_Status ^ (1 << 6);
                _coding.Add((byte)((int)Payment_Status ^ (1 << 6)));
            else
                _coding.Add((byte)((int)Payment_Status));

            //BitArray b_Payment = new BitArray(BitConverter.GetBytes(Payment));
            int _Payment = (int)Payment;
            if (CheckClose)
                //b_Payment[31] = true;
                _Payment = _Payment ^ (1 << 31);
            _coding.AddRange(BitConverter.GetBytes(_Payment));


            //byte[] strSend = preparation(_coding.ToArray());


            //bool ret = working(strSend);

            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 240);
            rep = null; _coding = null; cp866 = null;
            return resultcommand;
        }


        //
        //
        //ПеречислениеEDiscountType содержит значения, определяющие типы скидок. 
        //Название константы  Значение  Описание
        //edtAbsItemDisc 0  Абсолютная скидка на последнюю товарную
        //позицию(выражается в копейках). 
        //edtAbsItemAdd 1  Абсолютная наценка на последнюю товарную
        //позицию(выражается в копейках). 
        //edtRelItemDisc 2  Относительная скидка на последнюю товарную
        //позицию(выражается в0,00%). 
        //edtRelItemAdd 3  Относительная наценка на последнюю товарную
        //позицию(выражается в0,00%). 
        //edtAbsRecDisc 4  Абсолютная скидка на промежуточную сумму
        //чека(выражается в копейках). 
        //edtAbsRecAdd 5  Абсолютная наценка на промежуточную сумму
        //чека(выражается в копейках). 
        //edtRelRecDisc 6  Относительная скидка на промежуточную сумму
        //чека(выражается в0,00%). 
        //edtRelRecAdd 7  Относительная наценка на промежуточную сумму
        //чека(выражается в0,00%).
        public bool FPDiscount(byte Discount_Type, uint Discount_Value, string DiscComment)
        {
            
            Encoding cp866 = Encoding.GetEncoding(866);
            //cp866.
            List<byte> _coding = new List<byte>();

            DiscComment = PrepareString(DiscComment);

            _coding.Add(35); //Code Discount                   

            if ((Discount_Type == 0) || (Discount_Type == 1))
               _coding.Add(1); //тип операции                   
            else if ((Discount_Type == 2) || (Discount_Type == 3))
                _coding.Add(0); //тип операции                   
            else if ((Discount_Type == 4) || (Discount_Type == 5))
                _coding.Add(3); //тип операции                   
            else if ((Discount_Type == 6) || (Discount_Type == 7))
                _coding.Add(2); //тип операции                   

            if ((Discount_Type == 2) || (Discount_Type == 3) ||(Discount_Type == 6) ||(Discount_Type == 7))
            {
                List<byte> T1 = new List<byte>();
                T1.AddRange(BitConverter.GetBytes(Discount_Value));
                T1.RemoveAt(3);
                T1.Add(2+2);
                int _value = BitConverter.ToInt32(T1.ToArray(),0);
                if ((Discount_Type == 2) || (Discount_Type == 6))
                    _value = _value ^ (1 << 31);
                _coding.AddRange(BitConverter.GetBytes(_value));
            }
            else
            {
                int _value = (int)Discount_Value;
                if ((Discount_Type == 0)||(Discount_Type == 4))
                    _value = _value ^ (1 << 31);
                _coding.AddRange(BitConverter.GetBytes(_value));
            }

              
            byte[] strinfo = cp866.GetBytes(DiscComment.Substring(0,Math.Min(25,DiscComment.Length)));
            _coding.Add((byte)strinfo.Length);
            _coding.AddRange(strinfo);

            //byte[] strSend = preparation(_coding.ToArray());

            //bool ret = working(strSend);
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 240);
            UInt32 QuantityDiscounts = BitConverter.ToUInt32(rep.GetRange(0, 4).ToArray(), 0);
            UInt32 ChecksSum = BitConverter.ToUInt32(rep.GetRange(4, 4).ToArray(), 0);
            rep = null; _coding = null; cp866 = null;


            return resultcommand;
        }


        #region Информация

        #region данные принтера

        public string GetSerialNumber
        {
            get
            {
                if (_SerialNumber == null)
                {
                    GetInfo();
                    return _SerialNumber;
                }
                else
                    return _SerialNumber;
            }
        }

        public string GetFiscalNumber
        {
            get
            {
                if (_FiscalNumber == null)
                {
                    GetInfo();
                    return _FiscalNumber;
                }
                else
                    return _FiscalNumber;
            }
        }

        public DateTime GetMadeDate
        {
            get
            {
                if (_MadeDate == null)
                {
                    GetInfo();
                    return _MadeDate;
                }
                else
                    return _MadeDate;
            }
        }

        public string GetRegistrationDate
        {
            get
            {
                if (_RegistrationDate == null)
                {
                    GetInfo();
                    return _RegistrationDate.ToString("dd.MM.yyyy");
                }
                else
                {
                    return _RegistrationDate.ToString("dd.MM.yyyy");
                }
            }
        }

        public string GetVersion
        {
            get
            {
                if ((_version == null)||(_version == ""))
                {
                    GetInfo();
                    if (_version != null)
                        return _version.ToString();
                    else
                        return "";
                }
                else
                {
                    return _version.ToString();
                }
            }
        }

        #endregion

        #region Конфигурация принтера (биты)

        bool _PaymentMode;

        public bool GetPaymentMode
        {
            get
            {
                if (LastCommand != 0)
                    GetInfo();
                    return _PaymentMode;
               
            }
        }

        bool _BoxStatus;

        public bool GetBoxStatus
        {
            get{
                if (LastCommand != 0)
                    GetInfo();
                    return _BoxStatus;
               
            }
        }


        bool _CheckType;

        public bool GetCheckType
        {
            get
            {
                if (LastCommand != 0)
                    GetInfo();
                    return _CheckType;
               
            }
        }


        bool _TaxType;

        public bool GetTaxType
        {
            get
            {
                if (LastCommand != 0)
                    GetInfo();
                    return _TaxType;
                
            }
        }

        //GetSmenaOpened
        bool _SmenaOpened;

        public bool GetSmenaOpened
        {
            get
            {
                if (LastCommand != 0)
                    GetInfo();
                    return _SmenaOpened;
               
            }
        }
        //GetCheckOpened
        bool _CheckOpened;

        public bool GetCheckOpened
        {
            get
            {
                    if (LastCommand != 0)
                        GetInfo();
                    return _CheckOpened;
              
            }
        }

        //GetControlDisplay
        bool _ControlDisplay;

        public bool GetControlDisplay
        {
            get
            {
                    if (LastCommand != 0)
                        GetInfo();
                    return _ControlDisplay;
  
            }
        }


        //GetTaxChanging
        bool _TaxChanging;

        public bool GetTaxChanging
        {
            get
            {
                    if (LastCommand != 0)
                        GetInfo();
                    return _TaxChanging;

            }
        }

        /// <summary>
        /// GetWorkMode
        /// </summary>
        bool _WorkMode;

        public bool GetWorkMode
        {
            get
            {
                    if (LastCommand != 0)
                        GetInfo();
                    return _WorkMode;
 
            }
        }

        /////
        //GetLastCommandStatus
        //
        bool _LastCommandStatus;

        public bool GetLastCommandStatus
        {
            get
            {
                     //GetInfo();
                    //return bLastCommand;
                return !bLastCommand;

            }
        }
        /// <summary>
        /// GetPrintMode
        /// </summary>
        /// 
        bool _PrintMode;

        public bool GetPrintMode
        {
            get
            {
                    if(LastCommand!=0)
                        GetInfo();
                    return _PrintMode;
 
            }
        }



        #endregion



        void GetInfo()
        {
            //byte[] strSend = new byte[1];
            //strSend[0] = 0; //Code SendStatus                   
            //strSend = preparation(strSend);
            //byte[] _data = new byte[44]; // берем минимум
            //bLastCommand = working(strSend, out _data);
            
            //if (_data.Length == 0)
            //{
            //    Thread.Sleep(1000);
            //    bLastCommand = working(strSend, out _data);
            //}
            //strSend = null;
            //if (_data.Length == 0)
            //    return;


            List<byte> _coding = new List<byte>();
            _coding.Add(0); //SendStatus                
            bool resultcommand;
            List<byte> rep;
            try
            {
                rep = ReturnResult(out resultcommand, _coding, 400);
                if (!resultcommand)
                    return;
            }
            catch
            {
                return;
            }
            Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку. 

            //byte[] conf = new byte[] { _data[0], _data[1] };
            //int cur = 0;

            BitArray _bit = new BitArray(rep.GetRange(0, 2).ToArray());            
            _PaymentMode = _bit[1];
            _BoxStatus = _bit[2];
            _CheckType = _bit[3];
            _TaxType = _bit[4];
            _SmenaOpened = _bit[5];
            _CheckOpened = _bit[6];
            _ControlDisplay = _bit[7];
            _TaxChanging = _bit[9];
            _WorkMode = _bit[12];
            _LastCommandStatus = _bit[13];
            _PrintMode = _bit[14];
            //int beginbyte;
            //if (_SmenaOpened)
            //    beginbyte = 2;
            //else
            //    beginbyte = 3;

            //byte[] Ser = new byte[19];
            //for (int x = 0; x <= 18; x++)
            //    Ser[x] = _data[beginbyte + x];
            ////for (int x = 2; x < 21; x++)
            ////{
            ////    Ser[x-2] = _data[x];
            ////}

            string SerADate = cp866.GetString(rep.GetRange(2, 19).ToArray());
            
            try
            {
                _SerialNumber = SerADate.Substring(0, 19 - 8 - 2).Trim();
                _MadeDate = new DateTime(2000 + Convert.ToInt16(SerADate.Substring(17, 2)), Convert.ToInt16(SerADate.Substring(14, 2)), Convert.ToInt16(SerADate.Substring(11, 2)));
            }
            catch
            { return; }

            //int byteBeginDate;
            //if (_SmenaOpened)
            //    byteBeginDate=21;
            //else
            //    byteBeginDate = 22;

            string hexday = rep[21].ToString("X");
            int _day = Convert.ToInt16(hexday);

            string hexmonth = rep[22].ToString("X");
            int _month = Convert.ToInt16(hexmonth);

            string hexyear = rep[23].ToString("X");
            int _year = Convert.ToInt16(hexyear);

            string hexhour = rep[24].ToString("X");
            int _hour = Convert.ToInt16(hexhour);

            string hexmin = rep[25].ToString("X");
            int _min = Convert.ToInt16(hexmin);

            _RegistrationDate = new DateTime(2000 + _year, _month, _day, _hour, _min, 0);

            //byte[] Fisc = new byte[10];
            //for (int x = 0; x <= 9; x++)
            //{
            //    Fisc[x] = _data[27 + x];
            //}
            _FiscalNumber = cp866.GetString(rep.GetRange(26, 10).ToArray());

            ////Остальную расшифровку не делал, но читаем 1 байт узнаем длину, потом читаем длину байтов и раскодируем строку.

            //Byte[] version = new byte[] { _data[_data.Count() - 5], _data[_data.Count() - 4], _data[_data.Count() - 3], _data[_data.Count() - 2], _data[_data.Count() - 1] }; 

            _version = cp866.GetString(rep.GetRange(rep.Count - 5, 5).ToArray()); 

        }

        uint SaleCheckNumber;
        UInt32 TurnSaleTax_A;
        UInt32 TurnSaleTax_B;
        UInt32 TurnSaleTax_C;
        UInt32 TurnSaleTax_D;
        UInt32 TurnSaleTax_E;
        UInt32 TurnSaleTax_F;
        UInt32 TurnSaleCard;//1
        UInt32 TurnSaleCredit;//2
        UInt32 TurnSaleCheck;//3
        UInt32 TurnSaleCash;//4
        UInt32 TurnSale5;
        UInt32 TurnSale6;
        UInt32 TurnSale7;
        UInt32 TurnSale8;
        UInt32 TurnSale9;
        UInt32 TurnSale10;
        UInt32 ExtraChargeSale;
        UInt32 DiscountSale;
        UInt32 AvansSum;
        uint PayCheckNumber;
        UInt32 TurnPayTax_A;
        UInt32 TurnPayTax_B;
        UInt32 TurnPayTax_C;
        UInt32 TurnPayTax_D;
        UInt32 TurnPayTax_E;
        UInt32 TurnPayTax_F;
        UInt32 TurnPayCard; //1
        UInt32 TurnPayCredit; //2
        UInt32 TurnPayCheck;//3
        UInt32 TurnPayCash;//4
        UInt32 TurnPay5;
        UInt32 TurnPay6;
        UInt32 TurnPay7;
        UInt32 TurnPay8;
        UInt32 TurnPay9;
        UInt32 TurnPay10;
        UInt32 DiscountPay;
        UInt32 ExtraChargePay;
        UInt32 PaymentSum;

        public string GetSaleCheckNumberExt
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return SaleCheckNumber.ToString();
            }
        }

        public string GetPayCheckNumberExt
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return PayCheckNumber.ToString();
            }
        }

        public string GetDaySaleSum
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                UInt32 DaySaleSum = TurnSaleCard + TurnSaleCredit + TurnSaleCheck + TurnSaleCash + TurnSale5 + TurnSale6 + TurnSale7 + TurnSale8 + TurnSale9 + TurnSale10;
                return DaySaleSum.ToString();
            }
        }

        public string GetDayPaySum
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                UInt32 DayPaySum = TurnPayCard + TurnPayCredit + TurnPayCheck + TurnPayCash + TurnPay5 + TurnPay6 + TurnPay7 + TurnPay8 + TurnPay9 + TurnPay10;
                return DayPaySum.ToString();
            }
        }

        public string GetTurnSaleTax_A
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleTax_A.ToString();
            }
        }

        public string GetTurnSaleTax_B
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleTax_B.ToString();
            }
        }

        public string GetTurnSaleTax_C
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleTax_C.ToString();
            }
        }

        public string GetTurnSaleTax_D
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleTax_D.ToString();
            }
        }

        public string GetTurnSaleTax_E
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleTax_E.ToString();
            }
        }

        public string GetTurnSaleTax_F
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleTax_F.ToString();
            }
        }

        public string GetTurnSaleCard
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleCard.ToString();
            }
        }

        public string GetTurnSaleCredit
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleCredit.ToString();
            }
        }

        public string GetTurnSaleCheck
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleCheck.ToString();
            }
        }

        public string GetTurnSaleCash
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnSaleCash.ToString();
            }
        }

        public string GetTurnPayTax_A
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayTax_A.ToString();
            }
        }

        public string GetTurnPayTax_B
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayTax_B.ToString();
            }
        }

        public string GetTurnPayTax_C
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayTax_C.ToString();
            }
        }

        public string GetTurnPayTax_D
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayTax_D.ToString();
            }
        }

        public string GetTurnPayTax_E
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayTax_E.ToString();
            }
        }

        public string GetTurnPayTax_F
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayTax_F.ToString();
            }
        }

        public string GetTurnPayCard
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayCard.ToString();
            }
        }

        public string GetTurnPayCredit
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayCredit.ToString();
            }
        }

        public string GetTurnPayCheck
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayCheck.ToString();
            }
        }

        public string GetTurnPayCash
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return TurnPayCash.ToString();
            }
        }

        public string GetDiscountSale
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return DiscountSale.ToString();
            }
        }

        public string GetExtraChargeSale
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return ExtraChargeSale.ToString();
            }
        }

        public string GetDiscountPay
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return DiscountPay.ToString();
            }
        }

        public string GetExtraChargePay
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return ExtraChargePay.ToString();
            }
        }

        public string GetAvansSum
        {
            get
            {
                if (LastCommand != (byte)42)
                    GetDayReport();
                return AvansSum.ToString();
            }
        }

        public string GetPaymentSum
        {
            get
            {
                if(LastCommand!=(byte)42)
                    GetDayReport();
                return PaymentSum.ToString();
            }
        }

        void GetDayReport()
        {
            //List<byte> _coding = new List<byte>();
            //Encoding cp866 = Encoding.GetEncoding(866);

            //_coding.Add(42); //GetMemory
           
            //byte[] strSend = preparation(_coding.ToArray());
            //byte[] ret;
            //bLastCommand = working(strSend, out ret);

            List<byte> _coding = new List<byte>();
            _coding.Add(42); //DayReport                
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding, 400);


            //byte[] inc = new byte[2];
            //for (int x = 0; x < 2; x++)
            //    inc[x] = ret[x];
            int cur = 0;
            SaleCheckNumber = BitConverter.ToUInt16(rep.GetRange(cur, 2).ToArray(), 0);

            //byte[] A = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    A[x] = ret[2 + x];            
            cur = cur + 2;
            TurnSaleTax_A = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] B = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    B[x] = ret[7 + x];
            cur = cur + 5;
            TurnSaleTax_B = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] C = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    C[x] = ret[12 + x];
            cur = cur + 5;
            TurnSaleTax_C = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] D = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    D[x] = ret[17 + x];
            cur = cur + 5;
            TurnSaleTax_D = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] E = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    E[x] = ret[22 + x];
            cur = cur + 5;
            TurnSaleTax_E = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] F = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    F[x] = ret[27 + x];
            cur = cur + 5;
            TurnSaleTax_F = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] Card = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    Card[x] = ret[32 + x];
            cur = cur + 5;
            TurnSaleCard = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] Credit = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    Credit[x] = ret[37 + x];
            cur = cur + 5;
            TurnSaleCredit = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] Check = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    Check[x] = ret[42 + x];
            cur = cur + 5;
            TurnSaleCheck = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] Cash = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    Cash[x] = ret[47 + x];
            cur = cur + 5;
            TurnSaleCash = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            cur = cur + 5;
            TurnSale5 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnSale6 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnSale7 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnSale8 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnSale9 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnSale10 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            //byte[] _ExtraChargeSale = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _ExtraChargeSale[x] = ret[52 + x];
            cur = cur + 5;
            ExtraChargeSale = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _DiscountSale = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _DiscountSale[x] = ret[57 + x];
            cur = cur + 5;
            DiscountSale = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _AvansSum = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _AvansSum[x] = ret[62 + x];
            cur = cur + 5;
            AvansSum = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _PayCheckNumber = new byte[2];
            //for (int x = 0; x < 2; x++)
            //    _PayCheckNumber[x] = ret[67+x];
            cur = cur + 5;
            PayCheckNumber = BitConverter.ToUInt16(rep.GetRange(cur, 2).ToArray(), 0);


            //byte[] _TurnPayTax_A = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayTax_A[x] = ret[69 + x];
            cur = cur + 2;
            TurnPayTax_A = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayTax_B = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayTax_B[x] = ret[74 + x];
            cur = cur + 5;
            TurnPayTax_B = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayTax_C = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayTax_C[x] = ret[79 + x];
            cur = cur + 5;
            TurnPayTax_C = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayTax_D = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayTax_D[x] = ret[84 + x];
            cur = cur + 5;
            TurnPayTax_D = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayTax_E = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayTax_E[x] = ret[89 + x];
            cur = cur + 5;
            TurnPayTax_E = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayTax_F = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayTax_F[x] = ret[94 + x];
            cur = cur + 5;
            TurnPayTax_F = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayCard = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayCard[x] = ret[99 + x];
            cur = cur + 5;
            TurnPayCard = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayCredit = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayCredit[x] = ret[104 + x];
            cur = cur + 5;
            TurnPayCredit = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayCheck = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayCheck[x] = ret[109 + x];
            cur = cur + 5;
            TurnPayCheck = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _TurnPayCash = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _TurnPayCash[x] = ret[114 + x];
            cur = cur + 5;
            TurnPayCash = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            cur = cur + 5;
            TurnPay5 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnPay6 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnPay7 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnPay8 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnPay9 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            cur = cur + 5;
            TurnPay10 = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);
            //byte[] _ExtraChargePay = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _ExtraChargePay[x] = ret[119 + x];
            cur = cur + 5;
            ExtraChargePay = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _DiscountPay = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _DiscountPay[x] = ret[124 + x];
            cur = cur + 5;
            DiscountPay = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);

            //byte[] _PaymentSum = new byte[5];
            //for (int x = 0; x < 5; x++)
            //    _PaymentSum[x] = ret[129 + x];
            cur = cur + 5;
            PaymentSum = BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0);



        }

        public void AddRange(Dictionary<string, object> source, Dictionary<string, object> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    // handle duplicate key issue here
                }
            }
        }

        public Dictionary<string, UInt32> DicGetDayReport
        {
            get
            {
                //List<byte> _coding = new List<byte>();
                //Encoding cp866 = Encoding.GetEncoding(866);

                //_coding.Add(42); //GetMemory

                Dictionary<string, UInt32> Return = new Dictionary<string, uint>();

                 List<byte> _coding = new List<byte>();
                _coding.Add(42); //DayReport                
                bool resultcommand;
                List<byte> rep = ReturnResult(out resultcommand, _coding, 400);

                int cur = 0;
                Return.Add("SaleCheckNumber", BitConverter.ToUInt16(rep.GetRange(cur, 2).ToArray(), 0)); //счетчик чеков продаж = 2 


                cur = cur + 2;
                Return.Add("TurnSaleTax_A", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //1

                cur = cur + 5;
                Return.Add("TurnSaleTax_B", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //2

                cur = cur + 5;
                Return.Add("TurnSaleTax_C", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //3

                cur = cur + 5;
                Return.Add("TurnSaleTax_D", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //4

                cur = cur + 5;
                Return.Add("TurnSaleTax_E", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //5

                cur = cur + 5;
                Return.Add("TurnSaleTax_F", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //6

                cur = cur + 5;
                Return.Add("TurnSaleCard", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //7

                cur = cur + 5;
                Return.Add("TurnSaleCredit", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //8

                cur = cur + 5;
                Return.Add("TurnSaleCheck", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //9

                cur = cur + 5;
                Return.Add("TurnSaleCash", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //10

                cur = cur + 5;
                Return.Add("TurnSale5", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //11
                cur = cur + 5;
                Return.Add("TurnSale6", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //12
                cur = cur + 5;
                Return.Add("TurnSale7", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0));//13
                cur = cur + 5;
                Return.Add("TurnSale8", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //14
                cur = cur + 5;
                Return.Add("TurnSale9", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //15
                cur = cur + 5;
                Return.Add("TurnSale10", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //16

                cur = cur + 5;
                Return.Add("ExtraChargeSale", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //дневная наценка по продажам 

                cur = cur + 5;
                Return.Add("DiscountSale", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //дневная скидка по продажам 

                cur = cur + 5;
                Return.Add("AvansSum", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //дневная сумма служебного вноса  

                cur = cur + 5;
                Return.Add("PayCheckNumber", BitConverter.ToUInt16(rep.GetRange(cur, 2).ToArray(), 0)); //счетчик чеков выплат 


                cur = cur + 2;
                Return.Add("TurnPayTax_A", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //1

                cur = cur + 5;
                Return.Add("TurnPayTax_B", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //2

                cur = cur + 5;
                Return.Add("TurnPayTax_C", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //3

                cur = cur + 5;
                Return.Add("TurnPayTax_D", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //4 

                cur = cur + 5;
                Return.Add("TurnPayTax_E", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //5

                cur = cur + 5;
                Return.Add("TurnPayTax_F", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //6

                cur = cur + 5;
                Return.Add("TurnPayCard", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //7

                cur = cur + 5;
                Return.Add("TurnPayCredit", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //8

                cur = cur + 5;
                Return.Add("TurnPayCheck", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //9

                cur = cur + 5;
                Return.Add("TurnPayCash", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //10

                cur = cur + 5;
                Return.Add("TurnPay5", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0));  //11
                cur = cur + 5;
                Return.Add("TurnPay6", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //12
                cur = cur + 5;
                Return.Add("TurnPay7", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //13
                cur = cur + 5;
                Return.Add("TurnPay8", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //14
                cur = cur + 5;
                Return.Add("TurnPay9", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //15
                cur = cur + 5;
                Return.Add("TurnPay10", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //16

                cur = cur + 5;
                Return.Add("ExtraChargePay", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //дневная наценка по выплатам 

                cur = cur + 5;
                Return.Add("DiscountPay", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //дневная скидка по выплатам 

                cur = cur + 5;
                Return.Add("PaymentSum", BitConverter.ToUInt32(rep.GetRange(cur, 5).ToArray(), 0)); //дневная сумма служебной выдачи


                return Return;
            }
        }

        public Dictionary<string, object> infoReturn(byte status, byte result, byte reserv)
        {
            Dictionary<string, object> Return = new Dictionary<string, object>();
            Return.Add("ByteStatus", status);
            Return.Add("ByteResult", result);
            Return.Add("ByteReserv", reserv);
            var bitsStatus = new BitArray(new byte[] { status });//new BitArray(bStatus);
            string StatusInfo = "";
            Return.Add("BitStatus0", bitsStatus[0]); // принтер не готов    проверить принтер**
            if (bitsStatus[0]) StatusInfo = StatusInfo  + "принтер не готов    проверить принтер**;";
            Return.Add("BitStatus1", bitsStatus[1]); //превышение продолжительности хранения данных в КЛЕФ  проверить модем
            if (bitsStatus[1]) StatusInfo = StatusInfo + "ошибка модема, выключить/включить ЭККР,обратиться в сервис - центр; ";
            Return.Add("BitStatus2", bitsStatus[2]); //ошибка или переполнение фискальной памяти  обратиться в сервис-центр
            if (bitsStatus[2]) StatusInfo = StatusInfo + "ошибка или переполнение фискальной памяти  обратиться в сервис-центр;";
            Return.Add("BitStatus3", bitsStatus[3]); //неправильная дата или ошибка часов   обратиться в сервис-центр
            if (bitsStatus[3]) StatusInfo = StatusInfo + "неправильная дата или ошибка часов   обратиться в сервис-центр;";
            Return.Add("BitStatus4", bitsStatus[4]); //ошибка индикатора  подключить индикатор
            if (bitsStatus[4]) StatusInfo = StatusInfo + "ошибка индикатора  подключить индикатор;";
            Return.Add("BitStatus5", bitsStatus[5]); //превышение продолжительности смены  сделать z-отче
            if (bitsStatus[5]) StatusInfo = StatusInfo + "превышение продолжительности смены  сделать z-отчет;";
            Return.Add("BitStatus6", bitsStatus[6]); //снижение рабочего напряжения питания  проверить блок питания
            if (bitsStatus[6]) StatusInfo = StatusInfo + "снижение рабочего напряжения питания  проверить блок питания;";
            Return.Add("BitStatus7", bitsStatus[7]); //командане существует или запрещена в данном режиме  проверить последовательность выполнения команд
            if (bitsStatus[7]) StatusInfo = StatusInfo + "командане существует или запрещена в данном режиме  проверить последовательность выполнения команд;";
            Return.Add("StatusInfo", StatusInfo);

            string ReservInfo = "";

            var bitsReserv = new BitArray(new byte[] { reserv });//new BitArray(bReserv);
            Return.Add("BitReserv0", bitsReserv[0]); // открыт чек служебного отчета
            if (bitsReserv[0]) ReservInfo = ReservInfo + "открыт чек служебного отчета;";
            Return.Add("BitReserv1", bitsReserv[1]); // состояние аварии (команда завершится после устранения ошибки)
            if (bitsReserv[1]) ReservInfo = ReservInfo + "состояние аварии (команда завершится после устранения ошибки);";
            Return.Add("BitReserv2", bitsReserv[2]); // отсутствие бумаги, если принтер не готов
            if (bitsReserv[2]) ReservInfo = ReservInfo + "отсутствие бумаги, если принтер не готов;";
            Return.Add("BitReserv3", bitsReserv[3]); // чек: продажи/выплаты (0/1)
            if (bitsReserv[3]) ReservInfo = ReservInfo + "чек: продажи/выплаты (0/1);";
            Return.Add("BitReserv4", bitsReserv[4]); // принтер фискализирован
            if (bitsReserv[4]) ReservInfo = ReservInfo + "принтер фискализирован;";
            Return.Add("BitReserv5", bitsReserv[5]); // смена открыта
            if (bitsReserv[5]) ReservInfo = ReservInfo + "смена открыта;";
            Return.Add("BitReserv6", bitsReserv[6]); // открыт чек
            if (bitsReserv[6]) ReservInfo = ReservInfo + "открыт чек;";
            Return.Add("BitReserv7", bitsReserv[7]); // ЭККР не персонализирован
            if (bitsReserv[7]) ReservInfo = ReservInfo + "ЭККР не персонализирован;";
            Return.Add("ReservInfo", ReservInfo);

            string ResultInfo = "";
            if (result == 0)
                ResultInfo = "нормальное завершение";
            else if (result == 1) ResultInfo = "ошибка принтера";
            else if (result == 2) ResultInfo = "закончилась бумага";
            else if (result == 4) ResultInfo = "сбой фискальной памяти";
            else if (result == 6) ResultInfo = "снижение напряжения питания";
            else if (result == 8) ResultInfo = "фискальная память переполнена";
            else if (result == 10) ResultInfo = "не было персонализации";
            else if (result == 16) ResultInfo = "команда запрещена в данном режиме";
            else if (result == 19) ResultInfo = "ошибка программирования логотипа";
            else if (result == 20) ResultInfo = "неправильная длина строки";
            else if (result == 21) ResultInfo = "неправильный пароль";
            else if (result == 22) ResultInfo = "несуществующий номер (пароля, строки)";
            else if (result == 23) ResultInfo = "налоговая группа не существует или не установлена, налоги не вводились";
            else if (result == 24) ResultInfo = "тип оплат не существует";
            else if (result == 25) ResultInfo = "недопустимые коды символов";
            else if (result == 26) ResultInfo = "превышение количества налогов ";
            else if (result == 27) ResultInfo = "отрицательная продажа больше суммы предыдущих продаж чека";
            else if (result == 28) ResultInfo = "ошибка в описании артикула ";
            else if (result == 30) ResultInfo = "ошибка формата даты/времени";
            else if (result == 31) ResultInfo = "превышение регистраций в чеке";
            else if (result == 32) ResultInfo = "превышение разрядности вычисленной стоимости";
            else if (result == 33) ResultInfo = "переполнение регистра дневного оборота";
            else if (result == 34) ResultInfo = "переполнение регистра оплат";
            else if (result == 35) ResultInfo = "сумма “выдано” больше, чем в денежном ящике";
            else if (result == 36) ResultInfo = "дата младше даты последнего z-отчета";
            else if (result == 37) ResultInfo = "открыт чек выплат, продажи запрещены";
            else if (result == 38) ResultInfo = "открыт чек продаж, выплаты запрещены";
            else if (result == 39) ResultInfo = "команда запрещена, чек не открыт";
            else if (result == 40) ResultInfo = "переполнение памяти артикулов";
            else if (result == 41) ResultInfo = "команда запрещена до Z-отчета";
            else if (result == 42) ResultInfo = "команда запрещена до фискализации";
            else if (result == 43) ResultInfo = "сдача с этой оплаты запрещена ";
            else if (result == 44) ResultInfo = "команда запрещена, чек открыт";
            else if (result == 45) ResultInfo = "скидки/наценки запрещены, не было продаж";
            else if (result == 46) ResultInfo = "команда запрещена после начала оплат";
            else if (result == 47) ResultInfo = "превышение продолжительности отправки данных больше 72 часов";
            else if (result == 48) ResultInfo = "нет ответа от модема";
            else if (result == 50) ResultInfo = "команда запрещена, КЛЕФ не пустой";

            if ((bitsStatus[0]
               || bitsStatus[1]
               || bitsStatus[2]
               || bitsStatus[3]
               || bitsStatus[4]
               || bitsStatus[5]
               || bitsStatus[6]
               || bitsStatus[7])
               || (bitsReserv[1] || bitsReserv[2] || bitsReserv[7])
               || (result > 0)
               )
            {
                Return.Add("Error", true);
            }
            else
                Return.Add("Error", false);

            Return.Add("ResultInfo", ResultInfo);
            return Return;
        }

        public Dictionary<string, object> getDicInfo
        {
            get
            {
                Dictionary<string, object> Return = new Dictionary<string, object>();

                List<byte> _coding = new List<byte>();
                _coding.Add(0); //DayReport                
                bool resultcommand;
                byte bStatus, bResult, bReserv;
                List<byte> rep = ReturnResult(out resultcommand, out bStatus, out bResult, out bReserv, _coding, 400);

                AddRange(Return, infoReturn(bStatus, bResult, bReserv));


                Encoding cp866 = Encoding.GetEncoding(866); // кодировка, которая используется для конвертации байтов в строку.
                int cur = 0;

                BitArray _bit = new BitArray(rep.GetRange(cur, 2).ToArray());

                Return.Add("UsedTaxes", Convert.ToBoolean(_bit[0]));
                Return.Add("PayStatus", Convert.ToBoolean(_bit[1]));
                Return.Add("BoxClosed", Convert.ToBoolean(_bit[2]));
                Return.Add("CheckType", Convert.ToBoolean(_bit[3]));
                Return.Add("TaxType", Convert.ToBoolean(_bit[4]));
                Return.Add("SmenaOpened", Convert.ToBoolean(_bit[5]));
                Return.Add("CheckOpened", Convert.ToBoolean(_bit[6]));
                Return.Add("ControlDisplayOff", Convert.ToBoolean(_bit[7]));
                Return.Add("ControlPrinted", Convert.ToBoolean(_bit[8]));
                Return.Add("PrinteredLogo", Convert.ToBoolean(_bit[9]));
                Return.Add("CutterBlocked", Convert.ToBoolean(_bit[9]));
                Return.Add("ServReportPrintReceiptOnly", Convert.ToBoolean(_bit[11]));
                Return.Add("WorkModeStatus", Convert.ToBoolean(_bit[12])); 
                Return.Add("LastCommandStatus", Convert.ToBoolean(_bit[13]));
                Return.Add("PrintMode", Convert.ToBoolean(_bit[14]));

                


                string SerADate = cp866.GetString(rep.GetRange(2, 19).ToArray());
            
            try
            {
                _SerialNumber = SerADate.Substring(0, 19 - 8 - 2).Trim();
                _MadeDate = new DateTime(2000 + Convert.ToInt16(SerADate.Substring(17, 2)), Convert.ToInt16(SerADate.Substring(14, 2)), Convert.ToInt16(SerADate.Substring(11, 2)));
            }
            catch
            {
                _SerialNumber = "";
                _MadeDate = new DateTime(2000,1,1);
            }

            

            string hexday = rep[21].ToString("X");
            int _day = Convert.ToInt16(hexday);
            if (_day == 0) _day = 1;
            string hexmonth = rep[22].ToString("X");
            int _month = Convert.ToInt16(hexmonth);
                _month = Math.Min(_month, 1);
                _month = Math.Max(_month, 12);
            string hexyear = rep[23].ToString("X");
            int _year = Convert.ToInt16(hexyear);

            string hexhour = rep[24].ToString("X");
            int _hour = Convert.ToInt16(hexhour);

            string hexmin = rep[25].ToString("X");
            int _min = Convert.ToInt16(hexmin);

                _RegistrationDate = new DateTime(2000 + _year, _month, _day, _hour, _min, 0);
                


                _FiscalNumber = cp866.GetString(rep.GetRange(26, 10).ToArray());

            

            _version = cp866.GetString(rep.GetRange(rep.Count - 5, 5).ToArray()); 




                Return.Add("SerialNumber", _SerialNumber);
                Return.Add("FiscalNumber", _FiscalNumber);
                Return.Add("Reg_Date", _MadeDate);
                //Return.Add("Reg_Date", Convert.ToBoolean(_bit[14]));
                //Return.Add("Reg_Time", Convert.ToBoolean(_bit[14]));
                Return.Add("Reg_DateTime", _RegistrationDate);
                Return.Add("HardwareVersion", _version);
                return Return;
            }
        }

        public Dictionary<string, object> getKlefInfo
        {
            get
            {
                Dictionary<string, object> Return = new Dictionary<string, object>();

                List<byte> _coding = new List<byte>();
                _coding.Add(51); //DayReport                
                bool resultcommand;
                byte bStatus, bResult, bReserv;
                List<byte> rep = ReturnResult(out resultcommand, out bStatus, out bResult, out bReserv, _coding, 100);

                AddRange(Return, infoReturn(bStatus, bResult, bReserv));
                UInt32 PacketFirst = BitConverter.ToUInt32(rep.GetRange(0, 4).ToArray(),0);
                Return.Add("PacketFirst", PacketFirst);
                UInt32 PacketLast = BitConverter.ToUInt32(rep.GetRange(4, 4).ToArray(), 0);
                Return.Add("PacketLast", PacketLast);
                UInt16 FreeMem = BitConverter.ToUInt16(rep.GetRange(8, 2).ToArray(), 0);
                if (PacketFirst == 0 && PacketLast==0 && FreeMem==0)
                    FreeMem = 65535;
                Return.Add("FreeMem", FreeMem);

                return Return;
            }
        }

        #endregion

        #region отчеты

        public bool FPDayReport(ushort pass)
        {
            //byte[] b_pass = BitConverter.GetBytes(pass);
            //byte[] strSend = new byte[5];
            //strSend[0] = 9; //Code X                   
            //strSend[1] = b_pass[0]; //Code                    
            //strSend[2] = b_pass[1]; //Code               

            //strSend = preparation(strSend);



            //return working(strSend) && bLastCommand;

            List<byte> _coding = new List<byte>();
            _coding.Add(9); //DayReport
            _coding.AddRange(BitConverter.GetBytes(pass));
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding);

            return resultcommand;

        }

        public bool FPDayClrReport(ushort pass)
        {
            //byte[] b_pass = BitConverter.GetBytes(pass);
            //byte[] strSend = new byte[5];
            //strSend[0] = 13; //Code Z                   
            //strSend[1] = b_pass[0]; //Code                    
            //strSend[2] = b_pass[1]; //Code               

            //strSend = preparation(strSend);



            //return working(strSend) && bLastCommand;

            List<byte> _coding = new List<byte>();
            _coding.Add(13); //DayClrReport
            _coding.AddRange(BitConverter.GetBytes(pass));
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding);

            return resultcommand;
        }

        public bool FPPeriodicReport(ushort pass, DateTime FirstDay, DateTime LastDay)
        {

            Encoding cp866 = Encoding.GetEncoding(866);
            byte[] te = cp866.GetBytes(FirstDay.ToString("dd"));
            List<byte> _coding = new List<byte>();
            _coding.Add(17); //PeriodicReport
            _coding.AddRange(BitConverter.GetBytes(pass));
            

            _coding.Add(Convert.ToByte(Convert.ToInt32(FirstDay.ToString("dd"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(FirstDay.ToString("MM"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(FirstDay.ToString("yy"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(LastDay.ToString("dd"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(LastDay.ToString("MM"), 16)));
            _coding.Add(Convert.ToByte(Convert.ToInt32(LastDay.ToString("yy"), 16)));

            
            bool resultcommand;
            List<byte> rep = ReturnResult(out resultcommand, _coding);

            return resultcommand;
            //byte[] strSend = preparation(_coding.ToArray());
            //byte[] ret;
            //bool Rets = working(strSend, out ret);
            //strSend = null;
            //return Rets && bLastCommand;
        }

        #endregion

    }
}
