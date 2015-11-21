using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FPIKS;
using System.Xml;
using System.Diagnostics;

namespace FPIKSWork
{
    //Тут будет вся работа выполняться
    public class FPIKSWork
    {
        private bool disposed = false;

        private static int sqltimeout = 800;

        ReaderWriterLock rw = new ReaderWriterLock();

        string ConnectionString;
        public Int32 Port;
        public Int32 ErrorCode;
        public Int32 ByteStatus;
        public Int32 ByteResult;
        public Int32 ByteReserv;
        public Int32 FPNumber;
        public string FPVersion;
        public bool SmenaOpened;
        public string SerialNumber;
        public string FiscalNumber;
        public string CurrentDate;
        public string CurrentTime;
        public string ErrorInfo;
        public bool Error;
        public DateTime lastTime;
        //SqlConnection con;
        //SqlTransaction trans;        
        bool connect;

        FPIKS.FPIKS ics=null;// = new FPIKS.FPIKS();

        public  FPIKSWork(int ComPort)
        {
            
            this.Port = ComPort;
            string CurrDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FPIKSWork)).CodeBase);
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(CurrDir + "\\ConnectionString.xml");
            XmlNode list = xdoc.SelectSingleNode("/root/ConnectionString");

            ConnectionString = list.InnerText;
            list = null;
            xdoc = null;
            lastTime = DateTime.Now;
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    //ics.FPClose();
                    //ics = null;
                    //con.Close();
                    //con.Dispose();
                    //con = null;
                }

            }
            disposed = true;
        }

        ~FPIKSWork()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        public void TestConnection(object mu)
        {
            //Mutex mu
            try
            {
                ((Mutex)mu).WaitOne();
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    //con.ConnectionString = ConnectionString;
                    con.Open();
                    ErrorInfo = "";
                    ErrorCode = 0;
                    Error = false;
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    else if (con.State == ConnectionState.Broken)
                    {
                        con.Close();
                        con.Open();
                    }
                    else if (con.State != ConnectionState.Open)
                    {
                        Thread.Sleep(1000);
                    }

                    if (con.State != ConnectionState.Open)
                    {
                        Error = true;
                        ErrorInfo = "Подключение к SQL не выполнено";
                        ErrorCode = 9997;
                        SetError(Error, ErrorCode, ErrorInfo);
                        if (ics != null)
                            ics.FPClose();
                        ics = null;
                        if (mu!=null)
                            ((Mutex)mu).ReleaseMutex();
                        return;
                    }


                    ics = new FPIKS.FPIKS();

                    
                    Thread.Sleep(1000);
                    //string CurrDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FPIKSWork)).CodeBase);
                    //XmlDocument xdoc = new XmlDocument();
                    //xdoc.Load(CurrDir + "\\ConnectionString.xml");
                    //XmlNode list = xdoc.SelectSingleNode("/root/ConnectionString");

                    //ConnectionString = list.InnerText;


                 
                    try
                    {
                        connect = ics.FPInit((byte)Port, 9600, 500, 500);
                    }
                    catch
                    {
                        connect = false;
                    }

                    if (!connect)
                    {
                        Thread.Sleep(1000);
                        Error = true;
                        ErrorInfo = "Подключение не выполнено, не возможно подключиться через COM порт";
                        ErrorCode = 9999;
                        SetError(Error, ErrorCode, ErrorInfo);
                        if (ics != null)
                            ics.FPClose();
                        ics = null;
                        if (mu != null)
                            ((Mutex)mu).ReleaseMutex();
                        return;
                    }

                    Dictionary<string, object> getinfo = ics.getDicInfo;


                    this.FPNumber = Convert.ToInt32(getinfo["SerialNumber"]);//Convert.ToInt32(ics.GetSerialNumber);
                    this.FPVersion = getinfo["HardwareVersion"].ToString();//ics.GetVersion;
                    this.SmenaOpened = Convert.ToBoolean(getinfo["SmenaOpened"]); //ics.GetSmenaOpened;
                    //this.Port = ComPort;

                    if (this.SmenaOpened && Convert.ToBoolean(getinfo["BitStatus5"]) && Convert.ToByte(getinfo["ByteStatus"]) == 032)
                    {
                        ics.FPDayClrReport(0);
                        Error = true;
                    }

                    if (Convert.ToBoolean(getinfo["BitReserv6"]))
                    {
                        ics.FPResetOrder();
                    }
                    getinfo = ics.getDicInfo;
                    setErrorFromGetInfo(getinfo);
                    Dictionary<string, object> KlefInfo = ics.getKlefInfo;
                    setKlef(KlefInfo);

                    //SetError(getinfo, Error, ErrorCode, ErrorInfo);
                    //if (connect)
                    //{
                      //  ics.FPResetOrder();
                        //Error = !ics.FPLineFeed();
                        //if ((Error) && (ics.GetByteStatus==0))
                        //    ics.FPDayClrReport(0);
                    //}
                    con.Close();
                    con.Dispose();
                }
            }
            catch (Exception ex)
            {
                EventLog m_EventLog = new EventLog("");
                m_EventLog.Source = "FPIKSErrors";
                m_EventLog.WriteEntry("Ошибка в работе сервиса::TestConnection:" + ex.Message +"Port:"+Port.ToString(),
                    EventLogEntryType.Warning);
                m_EventLog = null;
                Error = true;
                ErrorCode = 9999;
                ErrorInfo = "Ошибка в работе сервиса::TestConnection:" + ex.Message;
            }
            finally
            {

                SetError(Error, ErrorCode, ErrorInfo);
                if (ics!=null)
                    ics.FPClose();
                ics = null;
                
                //if((con!=null)&&(con.State==ConnectionState.Open))
                //    con.Close();                
                //con.Dispose();
                //con = null;
                //Thread.Sleep(1000);
                try
                {
                    if (mu != null)
                        ((Mutex)mu).ReleaseMutex();
                }
                catch (Exception ex)
                {
                    mu = null;
                    EventLog m_EventLog = new EventLog("");
                    m_EventLog.Source = "FPIKSErrors";
                    m_EventLog.WriteEntry("Ошибка в работе сервиса::TestConnection:((Mutex)mu).ReleaseMutex()" + ex.Message + "Port:" + Port.ToString(),
                        EventLogEntryType.Warning);
                    m_EventLog = null;
                    Error = true;
                    ErrorCode = 9999;
                    ErrorInfo = "Ошибка в работе сервиса::TestConnection:" + ex.Message;
                }
            }
           // return !Error;
        }

        public Boolean setErrorFromGetInfo(Dictionary<string, object> getinfo)
        {
            Boolean _error = false;
            if ((bool)getinfo["Error"])
            {
                SetError(getinfo, true);
                _error = true;
            }
            return _error;
        }

        public void Startjob(object mu)
        {
            
            ((Mutex)mu).WaitOne();
            TimeSpan diff = DateTime.Now - lastTime;
            if (diff.Seconds<=2)
            {
                Random xrnd = new Random();
                Thread.Sleep(xrnd.Next(5, 20) * 1000);
            }
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();



                if (con.State == ConnectionState.Closed)
                    con.Open();
                else if (con.State == ConnectionState.Broken)
                {
                    con.Close();
                    Thread.Sleep(1000);
                    con.Open();
                }
                else if (con.State != ConnectionState.Open)
                {
                    Thread.Sleep(1000);
                }
                if (con.State != ConnectionState.Open)
                {
                    //Если что-то с SQL просто подождем следующий раз.
                    //_SetErrorVersion();
                    if (ics != null)
                        ics.FPClose();
                    ics = null;
                    ((Mutex)mu).ReleaseMutex();
                    return;
                }

                
                ics = new FPIKS.FPIKS();
                try
                {
                    connect = ics.FPInit((byte)Port, 9600, 50, 50);
                }
                catch
                {
                    connect = false;
                }
                
                if (!connect)
                {
                    //Thread.Sleep(1000);
                    Error = true;
                    ErrorInfo = "Подключение не выполнено, не возможно подключиться через COM порт";
                    ErrorCode = 9999;
                    SetError(Error, ErrorCode, ErrorInfo);
                    if (ics != null)
                        ics.FPClose();
                    ics = null;

                    ((Mutex)mu).ReleaseMutex();
                    return;
                }
                Dictionary<string, object> getinfo = null;
                try
                {
                    getinfo  = ics.getDicInfo;
                }
                catch(Exception ex)
                {
                    Error = true;
                    ErrorCode = 9999;
                    ErrorInfo = "Ошибка в работе сервиса::Startjob::getDicInfo:" + ex.Message;
                    SetError(Error, ErrorCode, ErrorInfo);
                    if (ics != null)
                        ics.FPClose();
                    ics = null;
                    ((Mutex)mu).ReleaseMutex();
                    return;
                }

                this.FPNumber = Convert.ToInt32(getinfo["SerialNumber"]);//Convert.ToInt32(ics.GetSerialNumber);                
                this.SmenaOpened = Convert.ToBoolean(getinfo["SmenaOpened"]); //ics.GetSmenaOpened;

                if (this.FPNumber == 0) //заглушка что бы не попадались нулевые
                {
                    Error = true;
                    ErrorCode = 9995;
                    ErrorInfo = "Запрещено 0 FPNumber::Startjob::getDicInfo:";
                    SetError(Error, ErrorCode, ErrorInfo);
                    ics.FPClose();
                    ics = null;                    
                    ((Mutex)mu).ReleaseMutex();
                    return;
                    //         }
                }


                this.FPVersion = getinfo["HardwareVersion"].ToString();
               
                if ((this.FPVersion != "ЕП-06") || (this.FPVersion == "")) //заглушка что бы не попадались нулевые
                {

                    _SetErrorVersion();
                    ics.FPClose();
                    ics = null;
                    ((Mutex)mu).ReleaseMutex();
                    return;
                    //         }
                }

                if (setErrorFromGetInfo(getinfo))
                {
                    ics.FPClose();
                    ics = null;
                    ((Mutex)mu).ReleaseMutex();
                    return;
                }

                try
                {
                    if (ics.GetNumZReport==null)
                    {
                        _SetErrorIcsConnection();
                        ics.FPClose();
                        ics = null;
                        ((Mutex)mu).ReleaseMutex();
                        return;
                    }
                }
                catch
                {
                    _SetErrorIcsConnection();
                    ics.FPClose();
                    ics = null;
                    ((Mutex)mu).ReleaseMutex();
                    return;
                }

                Dictionary<string, object> KlefInfo = ics.getKlefInfo;
                setKlef(KlefInfo);
               
                //}
                
                //this.Port = ComPort;
                //con = new SqlConnection(ConnectionString);

                //this.con.ConnectionString = ConnectionString;
                //con.Open();


                bool info = false;
                //DateTime now = DateTime.Now.AddDays(-1).AddSeconds(-30).AddHours(3); // по старому, по новому уже все в скуле
                DateTime now = DateTime.Now;

                _SetInfoAndPapStat(ics.GetPapStat, now, ics.CurrentDate, ics.CurrentTime,ics.GetByteStatus,ics.GetByteResult,ics.GetByteReserv);

                Int64 Time = Convert.ToInt64(now.ToString("yyyyMMddHHmmss"));

                using (SqlCommand command = new SqlCommand(@"
                                                            SELECT TOP 1
                                                            Operations.[id]
                                                            ,Operations.[NumSlave]
                                                            ,Operations.[DateTime]
                                                            ,Operations.[FPNumber]
                                                            ,Operations.[Operation]
                                                            ,Operations.[Closed]
                                                            ,ComInit.[DeltaTime]
                                                        FROM [FPWork].[dbo].[tbl_Operations] Operations with(rowlock)
                                                        inner join [FPWork].dbo.tbl_ComInit ComInit ON ComInit.[FPNumber]=Operations.FPNumber and Operations.[DateTime] between ComInit.DateTimeBegin and ComInit.DateTimeStop
                                                        where       [Closed]=0 
                                                                and ComInit.FPNumber=@FPNumber 
                                                                and isnull(Operations.Error,0)=0 
                                                                and isnull(ComInit.Error,0)=0
                                                                and [DateTime]<=replace(convert(varchar, DATEADD(ss, ComInit.[DeltaTime], @TimeNow),111),'/','')+replace(convert(varchar, DATEADD(ss, ComInit.[DeltaTime], @TimeNow),108),':','')
                                                                and isnull([Disable],0)<>1
                                                        order by DateTime", con))
                {
                    command.Parameters.Add("@FPNumber", SqlDbType.Int);command.Parameters["@FPNumber"].Value = FPNumber;
                    //command.Parameters.Add("@WorkDate", SqlDbType.BigInt);
                    //command.Parameters["@WorkDate"].Value = Time;
                    command.Parameters.Add("@TimeNow", SqlDbType.DateTime);command.Parameters["@TimeNow"].Value = now;
                    command.CommandTimeout = sqltimeout;
                    //command.Transaction = trans;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        int ByteStatus = 0;
                        int ByteResult = 0;
                        //int ErrorByteStatus = 0;
                        //int ErrorByteResult = 0;
                        bool CloseOperation = true;
                        
                        while (reader.Read())
                        {
                            SetSQLInWork(FPNumber, (Int64)reader["DateTime"], (int)reader["Operation"]);

                            if (Convert.ToInt16(reader["Operation"]) == 39) //Печатаем Z-отчет
                            {
                                if (ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                if (!ics.GetSmenaOpened)
                                {
                                    
                                    ics.SetDateTime(now.AddSeconds((Int64)reader["DeltaTime"]));
                                    ics.FPNullCheck();
                                    InCashCurrent(FPNumber);

                                }
                                //Обновляем статусы перед Z отчетом
                                try
                                {
                                    SetInfo((int)reader["FPNumber"], (Int64)reader["DateTime"], (int)reader["Operation"]);
                                }
                                catch (Exception ex)
                                {
                                    EventLog m_EventLog = new EventLog("");
                                    m_EventLog.Source = "FPIKSErrors";
                                    m_EventLog.WriteEntry("Ошибка в работе сервиса::SetInfo:" + ex.Message,
                                    EventLogEntryType.Warning);
                                    m_EventLog = null;
                                }
                                
                                info = PrintZ(out ByteStatus, out ByteResult, out ByteReserv);
                                _SetErrorVersion();
                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 35) //Печатаем X-отчет
                            {
                                if (ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                if (!ics.GetSmenaOpened)
                                {
                                    ics.SetDateTime(now.AddSeconds((Int64)reader["DeltaTime"]));
                                    ics.FPNullCheck();
                                }
                                info = PrintX(out ByteStatus, out ByteResult, out ByteReserv);
                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 40) //Печатаем периодического отчета
                            {
                                if (ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                DateTime LastMonthLastDate = DateTime.Today.AddDays(0 - DateTime.Today.Day);
                                DateTime LastMonthFirstDate = LastMonthLastDate.AddDays(1 - LastMonthLastDate.Day);

                                if (DateTime.Today.Day >= 21)
                                {
                                    LastMonthFirstDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                                    LastMonthLastDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 20);
                                }
                                else if (DateTime.Today.Day > 10 && DateTime.Today.Day < 20)
                                {
                                    LastMonthFirstDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                                    LastMonthLastDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 10);
                                }

                                info = PeriodicReport(out ByteStatus, out ByteResult, out ByteReserv, LastMonthFirstDate, LastMonthLastDate);
                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 12) //Печатаем чек
                            {
                                if (!ics.GetSmenaOpened)
                                {
                                    if (ics.GetFPCplCutter)
                                        ics.FPCplCutter();
                                    ics.SetDateTime(now.AddSeconds((Int64)reader["DeltaTime"]));
                                    ics.FPNullCheck();
                                    InCashCurrent(FPNumber);
                                }
                                if (!ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                    SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 
                                                                [Payment].id
                                                                ,[Payment].NumOperation
                                                                ,[Payment].[DATETIME]
                                                                ,[Payment].[FPNumber]
                                                                ,[Payment].[Type]
                                                                ,[Payment].[FRECNUM]
                                                                ,[Payment].[Payment_Status]
                                                                ,[Payment].[Payment]
                                                                ,[Payment].[Payment0]
                                                                ,[Payment].[Payment1]
                                                                ,[Payment].[Payment2]
                                                                ,[Payment].[Payment3]
                                                                ,[Payment].[CheckClose]
                                                                ,[Payment].[FiscStatus]
                                                                ,[Payment].[Comment]
                                                                ,isnull([Payment].[CheckSum],0) [CheckSum]
                                                                ,isnull([Payment].[PayBonus],0) [PayBonus]
                                                                ,isnull([Payment].[BousInAcc],0) [BousInAcc]
                                                                ,isnull([Payment].[BonusCalc],0) [BonusCalc]
                                                                ,isnull([Payment].[Card],0) [Card]
                                                                ,isnull([Payment].[RowCount],0) [RowCount]
                                                            FROM [FPWork].[dbo].[tbl_Payment] [Payment] with(rowlock)
                                                            inner join [FPWork].dbo.tbl_ComInit ComInit ON ComInit.[FPNumber]=[Payment].FPNumber
                                                            where       DATETIME=@DATETIME 
                                                                    AND Payment.id=@NumPayment 
                                                                    AND Payment.NumOperation=@NumOperation 
                                                                    AND ComInit.FPNumber=@FPNumber 
                                                                    and Type=0 and isnull([Payment].[RowCount],0) > 0
                                                            ", con);
                                    cmd.Parameters.Add("@FPNumber", SqlDbType.Int); cmd.Parameters["@FPNumber"].Value = FPNumber;
                                    cmd.Parameters.Add("@NumOperation", SqlDbType.BigInt); cmd.Parameters["@NumOperation"].Value = reader["id"];
                                    cmd.Parameters.Add("@NumPayment", SqlDbType.BigInt); cmd.Parameters["@NumPayment"].Value = reader["NumSlave"];
                                    cmd.Parameters.Add("@DATETIME", SqlDbType.BigInt); cmd.Parameters["@DATETIME"].Value = reader["DateTime"];
                                    //cmd.Transaction = tr;
                                    cmd.CommandTimeout = sqltimeout;
                                    SqlDataReader rdr = cmd.ExecuteReader();

                                    if (rdr.Read())
                                    {
                                        List<object> Dop = new List<object>();
                                        Dop.Add(rdr["CheckSum"]);
                                        Dop.Add(rdr["PayBonus"]);
                                        Dop.Add(rdr["BousInAcc"]);
                                        Dop.Add(rdr["BonusCalc"]);
                                        Dop.Add(rdr["Card"]);
                                        Int64 SumCheck = 0;
                                        info = PrintCheck(Convert.ToUInt64(rdr["id"])
                                                                    , Convert.ToUInt32(rdr["Payment"])
                                                                    , Convert.ToUInt32(rdr["Payment0"])
                                                                    , Convert.ToUInt32(rdr["Payment1"])
                                                                    , Convert.ToUInt32(rdr["Payment2"])
                                                                    , Convert.ToUInt32(rdr["Payment3"])
                                                                    , Convert.ToBoolean(rdr["CheckClose"])
                                                                    , Convert.ToBoolean(rdr["FiscStatus"])
                                                                    , rdr["Comment"].ToString()
                                                                    , Convert.ToInt32(rdr["FPNumber"])
                                                                    , Convert.ToUInt64(rdr["DateTime"])
                                                                    , Convert.ToInt32(rdr["FRECNUM"])
                                                                    , out ByteStatus
                                                                    , out ByteResult
                                                                    , out ByteReserv
                                                                    , out CloseOperation
                                                                    , Dop
                                                                    , out SumCheck
                                                                    , Convert.ToUInt32(rdr["RowCount"]));

                                        using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_Payment]
                                                                        SET 
                                                                            [ByteStatus] = @ByteStatus
                                                                            ,[ByteResult] = @ByteResult
                                                                            ,[ByteReserv] = @ByteReserv                                                                            
                                                                            ,[Error]=@Error
                                                                            ,[FPSumm]=@FPSumm
                                                                        WHERE 
                                                                            [id]=@id
                                                                            and [DateTime]=@DateTime 
		                                                                    and [FPNumber]=@FPNumber
		                                                                    ", con))
                                        {
                                            commandUpdate.Parameters.Add("@id", SqlDbType.BigInt); commandUpdate.Parameters["@id"].Value = rdr["id"];
                                            commandUpdate.Parameters.Add("@DateTime", SqlDbType.BigInt); commandUpdate.Parameters["@DateTime"].Value = rdr["DateTime"];
                                            commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = rdr["FPNumber"];
                                            commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = ByteStatus;
                                            commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = ByteResult;
                                            commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = ByteReserv;
                                            commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = !info;
                                            commandUpdate.Parameters.Add("@FPSumm", SqlDbType.BigInt); commandUpdate.Parameters["@FPSumm"].Value = SumCheck;
                                            commandUpdate.CommandTimeout = sqltimeout;
                                            //commandUpdate.Transaction = tr;
                                            commandUpdate.ExecuteNonQuery();
                                            commandUpdate.Dispose();
                                        }


                                    }
                                    
                                    rdr.Close();
                                

                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 5) //Печатаем чек возврата
                            {
                                if (!ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 
                                                                [Payment].id
                                                                ,[Payment].NumOperation
                                                                ,[Payment].[DATETIME]
                                                                ,[Payment].[FPNumber]
                                                                ,[Payment].[Type]
                                                                ,[Payment].[FRECNUM]
                                                                ,[Payment].[Payment_Status]
                                                                ,[Payment].[Payment]
                                                                ,[Payment].[Payment0]
                                                                ,[Payment].[Payment1]
                                                                ,[Payment].[Payment2]
                                                                ,[Payment].[Payment3]
                                                                ,[Payment].[CheckClose]
                                                                ,[Payment].[FiscStatus]
                                                                ,[Payment].[Comment]
                                                                ,isnull([Payment].[CheckSum],0) [CheckSum]
                                                                ,isnull([Payment].[PayBonus],0) [PayBonus]
                                                                ,isnull([Payment].[BousInAcc],0) [BousInAcc]
                                                                ,isnull([Payment].[BonusCalc],0) [BonusCalc]
                                                                ,isnull([Payment].[Card],0) [Card]
                                                            FROM [FPWork].[dbo].[tbl_Payment] [Payment] with(rowlock)
                                                            inner join [FPWork].dbo.tbl_ComInit ComInit ON ComInit.[FPNumber]=[Payment].FPNumber
                                                            where       Payment.DATETIME=@DATETIME 
                                                                    AND Payment.id=@NumPayment 
                                                                    AND Payment.NumOperation=@NumOperation 
                                                                    AND ComInit.FPNumber=@FPNumber 
                                                                    and [Payment].Type=1
                                                            ", con);
                                cmd.Parameters.Add("@FPNumber", SqlDbType.Int); cmd.Parameters["@FPNumber"].Value = FPNumber;
                                cmd.Parameters.Add("@NumOperation", SqlDbType.BigInt); cmd.Parameters["@NumOperation"].Value = reader["id"];
                                cmd.Parameters.Add("@NumPayment", SqlDbType.BigInt); cmd.Parameters["@NumPayment"].Value = reader["NumSlave"];
                                cmd.Parameters.Add("@DATETIME", SqlDbType.BigInt); cmd.Parameters["@DATETIME"].Value = reader["DateTime"];
                                //cmd.Transaction = trans;
                                cmd.CommandTimeout = sqltimeout;
                                SqlDataReader rdr = cmd.ExecuteReader();

                                if (rdr.Read())
                                {
                                    List<object> Dop = new List<object>();
                                    Dop.Add(rdr["CheckSum"]);
                                    Dop.Add(rdr["PayBonus"]);
                                    Dop.Add(rdr["BousInAcc"]);
                                    Dop.Add(rdr["BonusCalc"]);
                                    Dop.Add(rdr["Card"]);
                                    Int64 SumCheck = 0;
                                    info = PrintCheckPay(Convert.ToUInt64(rdr["id"])
                                                                , Convert.ToUInt32(rdr["Payment"])
                                                                , Convert.ToUInt32(rdr["Payment0"])
                                                                , Convert.ToUInt32(rdr["Payment1"])
                                                                , Convert.ToUInt32(rdr["Payment2"])
                                                                , Convert.ToUInt32(rdr["Payment3"])
                                                                , Convert.ToBoolean(rdr["CheckClose"])
                                                                , Convert.ToBoolean(rdr["FiscStatus"])
                                                                , rdr["Comment"].ToString()
                                                                , Convert.ToInt32(rdr["FPNumber"])
                                                                , Convert.ToUInt64(rdr["DateTime"])
                                                                , Convert.ToInt32(rdr["FRECNUM"])
                                                                , out ByteStatus
                                                                , out ByteResult
                                                                , out ByteReserv
                                                                , out CloseOperation
                                                                , Dop
                                                                , out SumCheck);

                                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_Payment]
                                                                        SET 
                                                                            [ByteStatus] = @ByteStatus
                                                                            ,[ByteResult] = @ByteResult
                                                                            ,[ByteReserv] = @ByteReserv                                                                            
                                                                            ,[Error]=@Error
                                                                            ,[FPSumm]=@FPSumm
                                                                        WHERE 
                                                                            [id]=@id
                                                                            and [DateTime]=@DateTime 
		                                                                    and [FPNumber]=@FPNumber
		                                                                    ", con))
                                    {
                                        commandUpdate.Parameters.Add("@id", SqlDbType.BigInt); commandUpdate.Parameters["@id"].Value = rdr["id"];
                                        commandUpdate.Parameters.Add("@DateTime", SqlDbType.BigInt); commandUpdate.Parameters["@DateTime"].Value = rdr["DateTime"];
                                        commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = rdr["FPNumber"];
                                        commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = ByteStatus;
                                        commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = ByteResult;
                                        commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = ByteReserv;
                                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = !info;
                                        commandUpdate.Parameters.Add("@FPSumm", SqlDbType.BigInt); commandUpdate.Parameters["@FPSumm"].Value = SumCheck;
                                        commandUpdate.CommandTimeout = sqltimeout;
                                        //commandUpdate.Transaction = tr;
                                        commandUpdate.ExecuteNonQuery();
                                        commandUpdate.Dispose();
                                    }


                                }

                                rdr.Close();
                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 3) //Смена кассира
                            {
                                SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 
                                                                [Cashiers].[id]
                                                                ,[Cashiers].[DATETIME]
                                                                ,[Cashiers].[FPNumber]
                                                                ,[Cashiers].[Num_Cashier]
                                                                ,[Cashiers].[Name_Cashier]
                                                                ,[Cashiers].[Pass_Cashier]
                                                                ,[Cashiers].[TakeProgName]
                                                            FROM [FPWork].[dbo].[tbl_Cashiers] [Cashiers] with(rowlock)
                                                            inner join [FPWork].dbo.tbl_ComInit ComInit ON ComInit.[FPNumber]=[Cashiers].FPNumber
                                                            where [Cashiers].DATETIME=@DATETIME AND ComInit.FPNumber=@FPNumber
                                                            ", con);
                                cmd.Parameters.Add("@FPNumber", SqlDbType.Int); cmd.Parameters["@FPNumber"].Value = FPNumber;
                                cmd.Parameters.Add("@DATETIME", SqlDbType.BigInt); cmd.Parameters["@DATETIME"].Value = reader["DateTime"];
                                //cmd.Transaction = trans;
                                cmd.CommandTimeout = sqltimeout;
                                SqlDataReader rdr = cmd.ExecuteReader();

                                if (rdr.Read())
                                {
                                    info = ChangeCashiers(rdr["Name_Cashier"].ToString(), out ByteStatus, out ByteResult, out ByteReserv);
                                }

                                rdr.Close();

                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 10) //внесение денег
                            {
                                if (ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                if (!ics.GetSmenaOpened)
                                {
                                    
                                    ics.SetDateTime(now.AddSeconds((Int64)reader["DeltaTime"]));
                                    ics.FPNullCheck();
                                    InCashCurrent(FPNumber);
                                }
                                SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 
                                                                [CashIO].[id]
                                                                ,[CashIO].[DATETIME]
                                                                ,[CashIO].[FPNumber]
                                                                ,[CashIO].[Type]
                                                                ,[CashIO].[Money]
                                                            FROM [FPWork].[dbo].[tbl_CashIO] [CashIO] with(rowlock)
                                                            inner join [FPWork].dbo.tbl_ComInit ComInit ON ComInit.[FPNumber]=[CashIO].FPNumber
                                                            where [CashIO].DATETIME=@DATETIME AND ComInit.FPNumber=@FPNumber and [CashIO].Type=0
                                                            ", con);
                                cmd.Parameters.Add("@FPNumber", SqlDbType.Int); cmd.Parameters["@FPNumber"].Value = FPNumber;
                                cmd.Parameters.Add("@DATETIME", SqlDbType.BigInt); cmd.Parameters["@DATETIME"].Value = reader["DateTime"];
                                //cmd.Transaction = trans;
                                cmd.CommandTimeout = sqltimeout;
                                SqlDataReader rdr = cmd.ExecuteReader();

                                if (rdr.Read())
                                {
                                    info = InToCash(Convert.ToUInt32(rdr["Money"]), out ByteStatus, out ByteResult, out ByteReserv);
                                }

                                rdr.Close();
                            }
                            else if (Convert.ToInt16(reader["Operation"]) == 15) //вынос денег
                            {
                                if (ics.GetFPCplCutter)
                                    ics.FPCplCutter();
                                SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 
                                                                 [CashIO].[id]
                                                                ,[CashIO].[DATETIME]
                                                                ,[CashIO].[FPNumber]
                                                                ,[CashIO].[Type]
                                                                ,[CashIO].[Money]
                                                            FROM [FPWork].[dbo].[tbl_CashIO] [CashIO] with(rowlock)
                                                            inner join [FPWork].dbo.tbl_ComInit ComInit ON ComInit.[FPNumber]=[CashIO].FPNumber
                                                            where [CashIO].DATETIME=@DATETIME AND ComInit.FPNumber=@FPNumber and [CashIO].Type=1
                                                            ", con);
                                cmd.Parameters.Add("@FPNumber", SqlDbType.Int); cmd.Parameters["@FPNumber"].Value = FPNumber;
                                cmd.Parameters.Add("@DATETIME", SqlDbType.BigInt); cmd.Parameters["@DATETIME"].Value = reader["DateTime"];
                                //cmd.Transaction = trans;
                                cmd.CommandTimeout = sqltimeout;
                                SqlDataReader rdr = cmd.ExecuteReader();

                                if (rdr.Read())
                                {
                                    UInt32 MaxMoney = (UInt32)Math.Min(Convert.ToDecimal(rdr["Money"]), (ics.GetMoneyInBox));
                                    if (MaxMoney > 40000)
                                    {
                                        string s_MaxMOney = MaxMoney.ToString();
                                        UInt32 Ost = Convert.ToUInt32("3" + s_MaxMOney.Substring(s_MaxMOney.Length - 4, 4));
                                        MaxMoney = MaxMoney - Ost;
                                    }
                                    info = OutOfCash(MaxMoney, out ByteStatus, out ByteResult, out ByteReserv);
                                }

                                rdr.Close();
                            }

                            using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_Operations]
                                                                            SET [Closed] = @info
                                                                                ,Error= case when @info=0 and InWork=1 and @Error=0 then 1 else @Error end
                                                                                ,InWork=case when @info=1 and InWork=1 and Error=0 then 0 else InWork end
                                                                                ,ByteStatus=@ByteStatus
                                                                                ,ByteResult=@ByteResult                                                                                
                                                                                ,ByteReserv=@ByteReserv                                                                                
                                                                                ,CurentDateTime=@CurentDateTime
                                                                        WHERE [DateTime]=@DateTime 
                                                                                and [id]=@NumOperation
                                                                                and FPNumber=@FPNumber 
                                                                                and Operation=@Operation                                                                                
                                                                        ", con))
                            {

                                commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = reader["FPNumber"];
                                commandUpdate.Parameters.Add("@NumOperation", SqlDbType.BigInt); commandUpdate.Parameters["@NumOperation"].Value = reader["id"];
                                commandUpdate.Parameters.Add("@DateTime", SqlDbType.BigInt); commandUpdate.Parameters["@DateTime"].Value = reader["DateTime"];
                                commandUpdate.Parameters.Add("@Operation", SqlDbType.Int); commandUpdate.Parameters["@Operation"].Value = reader["Operation"];
                                commandUpdate.Parameters.Add("@info", SqlDbType.Bit); commandUpdate.Parameters["@info"].Value = CloseOperation;
                                commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = !info;
                                commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = ByteStatus;
                                commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = ByteResult;
                                commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = ByteReserv;
                                commandUpdate.Parameters.Add("@CurentDateTime", SqlDbType.DateTime); commandUpdate.Parameters["@CurentDateTime"].Value = DateTime.Now;
                                //ics.FPClose();
                                commandUpdate.CommandTimeout = sqltimeout;
                                commandUpdate.ExecuteNonQuery();
                                commandUpdate.Dispose();
                            }
                            try
                            {
                                SetInfo((int)reader["FPNumber"], (Int64)reader["DateTime"], (int)reader["Operation"]);
                            }
                            catch (Exception ex)
                            {
                                EventLog m_EventLog = new EventLog("");
                                m_EventLog.Source = "FPIKSErrors";
                                m_EventLog.WriteEntry("Ошибка в работе сервиса::SetInfo:" + ex.Message,
                                EventLogEntryType.Warning);
                                m_EventLog = null;
                            }
                        }

                        reader.Close();
                    }
                    command.Dispose();
                }
                    con.Close();
                con.Dispose();
            }
            //}
            //catch (Exception ex)
            //{
            //    EventLog m_EventLog = new EventLog("");
            //    m_EventLog.Source = "FPIKSErrors";
            //    m_EventLog.WriteEntry("Ошибка в работе сервиса::Startjob:" + ex.Message,
            //        EventLogEntryType.Warning);
            //}
            //finally
            //{
                lastTime = DateTime.Now;
                ics.FPClose();
                ics = null;
                //if((con!=null)&&(con.State==ConnectionState.Open))
                //    con.Close();
                //con.Dispose();
                //con = null;
                ((Mutex)mu).ReleaseMutex();
   //         }
            //return info;
        }

        private void setKlef(Dictionary<string, object> KlefInfo)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FpWork].[dbo].[tbl_ComInit] SET [KlefMem]=@KlefMem   
                                                                                                                            ,[CurrentSystemDateTime]=GetDate()                                                                                                                         
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int);
                        commandUpdate.Parameters["@Port"].Value = Port;
                        commandUpdate.Parameters.Add("@KlefMem", SqlDbType.Int); commandUpdate.Parameters["@KlefMem"].Value = KlefInfo["FreeMem"];                        
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            } 
        }


        bool InCashCurrent(int FPNumber)
        {
            if (!ics.GetFPCplCutter)
                ics.FPCplCutter();
            //            --10011160 - 200.00
            //--10011018 - 200.00
            //--10011171 - 300.00
            //--10011281 - 300.00
            //--10010509
            //--10011190
            //--10011185
            //--10010161 - 
            //--10010360 - 
            //--10010164
            //--10011352
            //--10011272 - 300.00
            //--10010004 - 300.00
            //--10010014 - 300.00
            //--10010007 - 300.00
            //--10009616 - 300.00
            //--10009623 - 300.00
            //--10009627 - 300.00
            //--10011191
            //--10011455
            //--10011460
            //--10011454
            //--10011463
            //--10011820
            //--10011236
            //--10011459
            //--10011740
            //--10011738
            //--10011734
            //--10011748
            //--10011751
            //--10011735
            //--10011745
            //--10011427
            //--10011172
            //--10010162

            UInt32 incash = 30000;
            switch (FPNumber){
                case 10013984: incash = 40000; break;
                case 10013958: incash = 40000; break;
                case 10013640: incash = 40000; break;
                case 10013972: incash = 40000; break;
                case 10013951: incash = 40000; break;
                case 10013967: incash = 40000; break;
                
                //case 


                //case 10011160:
                //    incash = 20000;
                //    break;
                //case 10011018:
                //    incash = 20000;
                //    break;
                //case 10011352:
                //case 10011172:                    
                //case 10011281:                    
                //case 10011272:                    
                //case 10010004:                    
                //case 10010014:                    
                //case 10010007:                    
                //case 10009616:                    
                //case 10009623:                    
                //case 10009627:
                //case 10011455:
                //case 10011454:
                //case 10011740:
                //case 10011236:
                //case 10011463:
                //case 10011459:                
                //case 10011190:
                //case 10011748:
                //case 10011751:
                //    incash = 30000;
                //    break;
                default:
                    incash = 30000;
                    break;
            }
            










            if (incash!=0)
                return InToCash(incash, out ByteStatus, out ByteResult, out ByteReserv);

            return true;

        }


        void SetInfo(int FPNumber, Int64 DateTime, int Operation)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();
               
                if (connect)
                {


                    if ((con.State == ConnectionState.Open) && (ics.GetByteStatus==0) &&(ics.GetByteResult==0))
                    {
                        using (SqlCommand command = new SqlCommand(@"INSERT INTO [FPWork].[dbo].[tbl_Info]
                                                                                                    ([DateTime]
                                                                                                     ,[FPNumber]
                                                                                                     ,[Operation]
                                                                                                     ,[MoneyInBox]
                                                                                                     ,[SaleCheckNumber]
                                                                                                     ,[PayCheckNumber]
                                                                                                     ,[NumZReport]
                                                                                                     ,[LastDateZReport]
                                                                                                     ,[TurnSaleTax_A]
                                                                                                     ,[TurnSaleTax_B]
                                                                                                     ,[TurnSaleTax_C]
                                                                                                     ,[TurnSaleTax_D]
                                                                                                     ,[TurnSaleTax_E]
                                                                                                     ,[TurnSaleTax_F]
                                                                                                     ,[TurnSaleCard]
                                                                                                     ,[TurnSaleCredit]
                                                                                                     ,[TurnSaleCheck]
                                                                                                     ,[TurnSaleCash]
                                                                                                     ,[TurnPayTax_A]
                                                                                                     ,[TurnPayTax_B]
                                                                                                     ,[TurnPayTax_C]
                                                                                                     ,[TurnPayTax_D]
                                                                                                     ,[TurnPayTax_E]
                                                                                                     ,[TurnPayTax_F]
                                                                                                     ,[TurnPayCard]
                                                                                                     ,[TurnPayCredit]
                                                                                                     ,[TurnPayCheck]
                                                                                                     ,[TurnPayCash]
                                                                                                     ,[DiscountSale]
                                                                                                     ,[ExtraChargeSale]
                                                                                                     ,[DiscountPay]
                                                                                                     ,[ExtraChargePay]
                                                                                                     ,[AvansSum]
                                                                                                     ,[PaymentSum])
                                                                                               VALUES
                                                                                                     (@DateTime
                                                                                                     ,@FPNumber
                                                                                                     ,@Operation
                                                                                                     ,@MoneyInBox
                                                                                                     ,@SaleCheckNumber
                                                                                                     ,@PayCheckNumber
                                                                                                     ,@NumZReport
                                                                                                     ,@LastDateZReport
                                                                                                     ,@TurnSaleTax_A
                                                                                                     ,@TurnSaleTax_B
                                                                                                     ,@TurnSaleTax_C
                                                                                                     ,@TurnSaleTax_D
                                                                                                     ,@TurnSaleTax_E
                                                                                                     ,@TurnSaleTax_F
                                                                                                     ,@TurnSaleCard
                                                                                                     ,@TurnSaleCredit
                                                                                                     ,@TurnSaleCheck
                                                                                                     ,@TurnSaleCash
                                                                                                     ,@TurnPayTax_A
                                                                                                     ,@TurnPayTax_B
                                                                                                     ,@TurnPayTax_C
                                                                                                     ,@TurnPayTax_D
                                                                                                     ,@TurnPayTax_E
                                                                                                     ,@TurnPayTax_F
                                                                                                     ,@TurnPayCard
                                                                                                     ,@TurnPayCredit
                                                                                                     ,@TurnPayCheck
                                                                                                     ,@TurnPayCash
                                                                                                     ,@DiscountSale
                                                                                                     ,@ExtraChargeSale
                                                                                                     ,@DiscountPay
                                                                                                     ,@ExtraChargePay
                                                                                                     ,@AvansSum
                                                                                                     ,@PaymentSum)                                                                                          
                                                                                          ", con))
                        {

                            Dictionary<string, UInt32> GetDayReport = ics.DicGetDayReport;
                            if (GetDayReport == null)
                                return;
                            command.Parameters.Add("@DateTime", SqlDbType.BigInt); command.Parameters["@DateTime"].Value = DateTime;
                            command.Parameters.Add("@FPNumber", SqlDbType.Int); command.Parameters["@FPNumber"].Value = FPNumber;
                            command.Parameters.Add("@Operation", SqlDbType.Int); command.Parameters["@Operation"].Value = Operation;
                            command.Parameters.Add("@MoneyInBox", SqlDbType.BigInt); command.Parameters["@MoneyInBox"].Value = ics.GetMoneyInBox;
                            command.Parameters.Add("@NumZReport", SqlDbType.BigInt); command.Parameters["@NumZReport"].Value = Convert.ToInt32(ics.GetNumZReport);
                            string Zrep = ics.GetLastDateZReport;
                            if (Zrep.Length == 10)
                            {
                                command.Parameters.Add("@LastDateZReport", SqlDbType.BigInt); command.Parameters["@LastDateZReport"].Value = Convert.ToInt32(Zrep.Substring(6, 4) + Zrep.Substring(3, 2) + Zrep.Substring(0, 2));
                            }
                            else
                            {
                                command.Parameters.Add("@LastDateZReport", SqlDbType.BigInt); command.Parameters["@LastDateZReport"].Value = 0;
                            }
                            //Int32[] TurnSaleTax = ics.GetTurnCurrentCheckTax;
                            //if (GetDayReport != null)
                            //{
                                command.Parameters.Add("@SaleCheckNumber", SqlDbType.BigInt); command.Parameters["@SaleCheckNumber"].Value = GetDayReport["SaleCheckNumber"];
                                command.Parameters.Add("@PayCheckNumber", SqlDbType.BigInt); command.Parameters["@PayCheckNumber"].Value = GetDayReport["PayCheckNumber"];
                            

                           
                                command.Parameters.Add("@TurnSaleTax_A", SqlDbType.BigInt); command.Parameters["@TurnSaleTax_A"].Value = GetDayReport["TurnSaleTax_A"];
                                command.Parameters.Add("@TurnSaleTax_B", SqlDbType.BigInt); command.Parameters["@TurnSaleTax_B"].Value = GetDayReport["TurnSaleTax_B"];
                                command.Parameters.Add("@TurnSaleTax_C", SqlDbType.BigInt); command.Parameters["@TurnSaleTax_C"].Value = GetDayReport["TurnSaleTax_C"];
                                command.Parameters.Add("@TurnSaleTax_D", SqlDbType.BigInt); command.Parameters["@TurnSaleTax_D"].Value = GetDayReport["TurnSaleTax_D"];
                                command.Parameters.Add("@TurnSaleTax_E", SqlDbType.BigInt); command.Parameters["@TurnSaleTax_E"].Value = GetDayReport["TurnSaleTax_E"];
                                command.Parameters.Add("@TurnSaleTax_F", SqlDbType.BigInt); command.Parameters["@TurnSaleTax_F"].Value = GetDayReport["TurnSaleTax_F"];




                                command.Parameters.Add("@TurnSaleCard", SqlDbType.BigInt); command.Parameters["@TurnSaleCard"].Value = GetDayReport["TurnSaleCard"];
                                command.Parameters.Add("@TurnSaleCredit", SqlDbType.BigInt); command.Parameters["@TurnSaleCredit"].Value = GetDayReport["TurnSaleCredit"];
                                command.Parameters.Add("@TurnSaleCheck", SqlDbType.BigInt); command.Parameters["@TurnSaleCheck"].Value = GetDayReport["TurnSaleCheck"];
                                command.Parameters.Add("@TurnSaleCash", SqlDbType.BigInt); command.Parameters["@TurnSaleCash"].Value = GetDayReport["TurnSaleCash"];


                                command.Parameters.Add("@TurnPayTax_A", SqlDbType.BigInt); command.Parameters["@TurnPayTax_A"].Value = GetDayReport["TurnPayTax_A"];
                                command.Parameters.Add("@TurnPayTax_B", SqlDbType.BigInt); command.Parameters["@TurnPayTax_B"].Value = GetDayReport["TurnPayTax_B"];
                                command.Parameters.Add("@TurnPayTax_C", SqlDbType.BigInt); command.Parameters["@TurnPayTax_C"].Value = GetDayReport["TurnPayTax_C"];
                                command.Parameters.Add("@TurnPayTax_D", SqlDbType.BigInt); command.Parameters["@TurnPayTax_D"].Value = GetDayReport["TurnPayTax_D"];
                                command.Parameters.Add("@TurnPayTax_E", SqlDbType.BigInt); command.Parameters["@TurnPayTax_E"].Value = GetDayReport["TurnPayTax_E"];
                                command.Parameters.Add("@TurnPayTax_F", SqlDbType.BigInt); command.Parameters["@TurnPayTax_F"].Value = GetDayReport["TurnPayTax_F"];



                                command.Parameters.Add("@TurnPayCard", SqlDbType.BigInt); command.Parameters["@TurnPayCard"].Value = GetDayReport["TurnPayCard"];
                                command.Parameters.Add("@TurnPayCredit", SqlDbType.BigInt); command.Parameters["@TurnPayCredit"].Value = GetDayReport["TurnPayCredit"];
                                command.Parameters.Add("@TurnPayCheck", SqlDbType.BigInt); command.Parameters["@TurnPayCheck"].Value = GetDayReport["TurnPayCheck"];
                                command.Parameters.Add("@TurnPayCash", SqlDbType.BigInt); command.Parameters["@TurnPayCash"].Value = GetDayReport["TurnPayCash"];


                                command.Parameters.Add("@DiscountSale", SqlDbType.BigInt); command.Parameters["@DiscountSale"].Value = GetDayReport["DiscountSale"];
                                command.Parameters.Add("@ExtraChargeSale", SqlDbType.BigInt); command.Parameters["@ExtraChargeSale"].Value = GetDayReport["ExtraChargeSale"];
                                command.Parameters.Add("@DiscountPay", SqlDbType.BigInt); command.Parameters["@DiscountPay"].Value = GetDayReport["DiscountPay"];
                                command.Parameters.Add("@ExtraChargePay", SqlDbType.BigInt); command.Parameters["@ExtraChargePay"].Value = GetDayReport["ExtraChargePay"];
                                command.Parameters.Add("@AvansSum", SqlDbType.BigInt); command.Parameters["@AvansSum"].Value = GetDayReport["AvansSum"];
                                command.Parameters.Add("@PaymentSum", SqlDbType.BigInt); command.Parameters["@PaymentSum"].Value = GetDayReport["PaymentSum"];
                            //}
                                command.CommandTimeout = sqltimeout;
                            command.ExecuteNonQuery();
                            command.Dispose();


                        }
                    }
                }
                con.Close();
                con.Dispose();
            }

        }

        void SetSQLInWork(int FPNumber, Int64 DateTime, int Operation)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_Operations]
                                                                        SET 
                                                                            [InWork] = 1      
                                                                            ,[CurentDateTime] = GETDATE()
      
                                                                        WHERE [DateTime]=@DateTime 
		                                                                    and [FPNumber]=@FPNumber
		                                                                    and [Operation]=@Operation", con))
                    {

                        commandUpdate.Parameters.Add("@DateTime", SqlDbType.BigInt); commandUpdate.Parameters["@DateTime"].Value = DateTime;
                        commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = FPNumber;
                        commandUpdate.Parameters.Add("@Operation", SqlDbType.Int); commandUpdate.Parameters["@Operation"].Value = Operation;
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
        }

        void UpdatePayment(int FPNumber, UInt64 DateTime, UInt64 NumPayment, byte lByteStatus, byte lByteResult, byte lByteReserv, bool lError, Int64 SumCheck)
        {
            //_FPNumber, _DateTime, _NumPayment, ics.GetBoxStatus, ics.GetByteResult, ics.GetByteReserv
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_Payment]
                                                                        SET 
                                                                            [ByteStatus] = @ByteStatus
                                                                            ,[ByteResult] = @ByteResult
                                                                            ,[ByteReserv] = @ByteReserv                                                                            
                                                                            ,[Error]=@Error
                                                                            ,[FPSumm]=@FPSumm
                                                                        WHERE [DateTime]=@DateTime 
		                                                                    and [FPNumber]=@FPNumber
		                                                                    and [id]=@NumPayment", con))
                    {

                        commandUpdate.Parameters.Add("@DateTime", SqlDbType.BigInt); commandUpdate.Parameters["@DateTime"].Value = DateTime;
                        commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = FPNumber;
                        commandUpdate.Parameters.Add("@NumPayment", SqlDbType.BigInt); commandUpdate.Parameters["@NumPayment"].Value = NumPayment;
                        commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = lByteStatus;
                        commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = lByteResult;
                        commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = lByteReserv;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = lError;
                        commandUpdate.Parameters.Add("@FPSumm", SqlDbType.BigInt); commandUpdate.Parameters["@FPSumm"].Value = SumCheck;
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
        }


        void SetError(Dictionary<string, object> getDicInfo, Boolean _error)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FpWork].[dbo].[tbl_ComInit] SET [Error]=@Error
                                                                                                                                         ,[ErrorCode]=@ErrorCode
                                                                                                                                         ,[ErrorInfo]=@ErrorInfo
                                                                                                                                         ,[FPNumber]=case when @FPNumber=0 then [FPNumber] else @FPNumber end
                                                                                                                                        ,[SerialNumber]=@SerialNumber 
                                                                                                                                        ,[FiscalNumber]=@FiscalNumber                                                                                                                                         
                                                                                                                                        ,[CurrentSystemDateTime]=GetDate() 
                                                                                                                                        ,[ByteStatus]=@ByteStatus 
                                                                                                                                        ,[ByteResult]=@ByteResult 
                                                                                                                                        ,[Version]=@FPVersion
                                                                                                                                        ,[SmenaOpened]=@SmenaOpened
                                                                                                                                        ,[ByteReservInfo]=@ByteReservInfo
                                                                                                                                        ,[ByteStatusInfo]=@ByteStatusInfo                                                                                                                                        
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        //commandUpdate.Transaction = trans;
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int);
                        commandUpdate.Parameters["@Port"].Value = Port;
                        //result = Connect.;
                        commandUpdate.Parameters.Add("@ErrorCode", SqlDbType.Int); commandUpdate.Parameters["@ErrorCode"].Value = getDicInfo["ByteResult"];//ics.GetByteResult;
                        commandUpdate.Parameters.Add("@ErrorInfo", SqlDbType.NVarChar, 1024); commandUpdate.Parameters["@ErrorInfo"].Value = getDicInfo["ResultInfo"];
                        commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = FPNumber;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = _error;
                        commandUpdate.Parameters.Add("@SerialNumber", SqlDbType.NVarChar, 20); commandUpdate.Parameters["@SerialNumber"].Value = getDicInfo["SerialNumber"].ToString();
                        commandUpdate.Parameters.Add("@FiscalNumber", SqlDbType.NVarChar, 20); commandUpdate.Parameters["@FiscalNumber"].Value = getDicInfo["FiscalNumber"].ToString();

                        commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = getDicInfo["ByteStatus"];
                        commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = getDicInfo["ByteResult"];
                        commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = getDicInfo["ByteReserv"];
                        commandUpdate.Parameters.Add("@ByteReservInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteReservInfo"].Value = getDicInfo["ReservInfo"];
                        commandUpdate.Parameters.Add("@ByteStatusInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteStatusInfo"].Value = getDicInfo["StatusInfo"];
                        commandUpdate.Parameters.Add("@FPVersion", SqlDbType.NVarChar, 5); commandUpdate.Parameters["@FPVersion"].Value = getDicInfo["HardwareVersion"].ToString();
                        commandUpdate.Parameters.Add("@SmenaOpened", SqlDbType.Bit); commandUpdate.Parameters["@SmenaOpened"].Value = getDicInfo["SmenaOpened"];
                        //ics.FPClose();
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();


                    }
                }
                con.Close();
                con.Dispose();
            }
        }

        void SetError(Dictionary<string, object> getDicInfo,Boolean _error,int _errorCode, string _errorInfo)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FpWork].[dbo].[tbl_ComInit] SET [Error]=@Error
                                                                                                                                         ,[ErrorCode]=@ErrorCode
                                                                                                                                         ,[ErrorInfo]=@ErrorInfo
                                                                                                                                         ,[FPNumber]=case when @FPNumber=0 then [FPNumber] else @FPNumber end
                                                                                                                                        ,[SerialNumber]=@SerialNumber 
                                                                                                                                        ,[FiscalNumber]=@FiscalNumber                                                                                                                                         
                                                                                                                                        ,[CurrentSystemDateTime]=GetDate() 
                                                                                                                                        ,[ByteStatus]=@ByteStatus 
                                                                                                                                        ,[ByteResult]=@ByteResult 
                                                                                                                                        ,[Version]=@FPVersion
                                                                                                                                        ,[SmenaOpened]=@SmenaOpened
                                                                                                                                        ,[ByteReservInfo]=@ByteReservInfo
                                                                                                                                        ,[ByteStatusInfo]=@ByteStatusInfo                                                                                                                                        
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        //commandUpdate.Transaction = trans;
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int);
                        commandUpdate.Parameters["@Port"].Value = Port;
                        //result = Connect.;
                        commandUpdate.Parameters.Add("@ErrorCode", SqlDbType.Int); commandUpdate.Parameters["@ErrorCode"].Value = _errorCode;//ics.GetByteResult;
                        commandUpdate.Parameters.Add("@ErrorInfo", SqlDbType.NVarChar, 1024); commandUpdate.Parameters["@ErrorInfo"].Value = _errorInfo;
                        commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = FPNumber;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = _error;
                        commandUpdate.Parameters.Add("@SerialNumber", SqlDbType.NVarChar, 20); commandUpdate.Parameters["@SerialNumber"].Value = getDicInfo["SerialNumber"].ToString();
                        commandUpdate.Parameters.Add("@FiscalNumber", SqlDbType.NVarChar, 20); commandUpdate.Parameters["@FiscalNumber"].Value = getDicInfo["FiscalNumber"].ToString();

                        commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = getDicInfo["ByteStatus"];
                        commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = getDicInfo["ByteStatus"];
                        commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = getDicInfo["ByteReserv"];
                        commandUpdate.Parameters.Add("@ByteReservInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteReservInfo"].Value = getDicInfo["ReservInfo"];
                        commandUpdate.Parameters.Add("@ByteStatusInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteStatusInfo"].Value = getDicInfo["StatusInfo"];
                        commandUpdate.Parameters.Add("@FPVersion", SqlDbType.NVarChar, 5); commandUpdate.Parameters["@FPVersion"].Value = getDicInfo["HardwareVersion"].ToString();
                        commandUpdate.Parameters.Add("@SmenaOpened", SqlDbType.Bit); commandUpdate.Parameters["@SmenaOpened"].Value = getDicInfo["SmenaOpened"];                        
                        //ics.FPClose();
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();


                    }
                }
                con.Close();
                con.Dispose();
            }
        }

        void SetError(Boolean _error, int _errorCode, string _errorInfo)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FpWork].[dbo].[tbl_ComInit] SET [Error]=@Error
                                                                                                                            ,[ErrorCode]=@ErrorCode
                                                                                                                            ,[ErrorInfo]=@ErrorInfo  
                                                                                                                            ,[CurrentSystemDateTime]=GetDate()
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int);
                        commandUpdate.Parameters["@Port"].Value = Port;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = _error;
                        commandUpdate.Parameters.Add("@ErrorCode", SqlDbType.Int); commandUpdate.Parameters["@ErrorCode"].Value = _errorCode;//ics.GetByteResult;
                        commandUpdate.Parameters.Add("@ErrorInfo", SqlDbType.NVarChar, 1024); commandUpdate.Parameters["@ErrorInfo"].Value = _errorInfo;
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
        }

        void SetError()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();
                //if ((ics == null)||(!connect))
                //{
                //    ics = new FPIKS.FPIKS();
                //    connect = ics.FPInit((byte)Port, 9600, 1, 1);
                //}
                if (connect)
                {
                    
                    Error = ics.GetLastCommandStatus;
                    ErrorInfo = ics.GetTextErrorMessage == null ? "" : ics.GetTextErrorMessage;
                    if (ErrorInfo != "ЭККР не отвечает!")
                    {
                        ErrorCode = Convert.ToInt32(ics.GetByteResult);
                        try
                        {
                            FPNumber = Convert.ToInt32(ics.GetSerialNumber);
                        }
                        catch {
                            connect = false;
                            ErrorInfo = "ЭККР не отвечает!";
                            Error = true;
                            ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                            ErrorCode = 9999;
                            //FPNumber = 0;
                            ByteResult = 9999;
                            ByteStatus = 9999;

                            FiscalNumber = "";

                            SerialNumber = "";


                            CurrentDate = "";
                            CurrentTime = "";
                        }

                        ByteResult = Convert.ToInt32(ics.GetByteResult);
                        ByteStatus = Convert.ToInt32(ics.GetByteStatus);
                        ByteReserv = Convert.ToInt32(ics.GetByteReserv);
                        try
                        {
                            FiscalNumber = ics.GetFiscalNumber;
                        }
                        catch
                        {
                            FiscalNumber = "";
                            connect = false;
                            ErrorInfo = "ЭККР не отвечает!";
                            Error = true;
                            ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                            ErrorCode = 9999;
                            //FPNumber = 0;
                            ByteResult = 9999;
                            ByteStatus = 9999;

                            FiscalNumber = "";

                            SerialNumber = "";


                            CurrentDate = "";
                            CurrentTime = "";
 
                        }
                        if (FiscalNumber == null) FiscalNumber = "";
                        try
                        {
                            SerialNumber = ics.GetSerialNumber;
                        }
                        catch
                        {
                            SerialNumber = "";
                            connect = false;
                            ErrorInfo = "ЭККР не отвечает!";
                            Error = true;
                            ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                            ErrorCode = 9999;
                            //FPNumber = 0;
                            ByteResult = 9999;
                            ByteStatus = 9999;

                            FiscalNumber = "";

                            SerialNumber = "";


                            CurrentDate = "";
                            CurrentTime = "";
                        }
                        try
                        {
                            FPVersion = ics.GetVersion;
                        }
                        catch {
                            FPVersion = "";
                            connect = false;
                            ErrorInfo = "ЭККР не отвечает!";
                            Error = true;
                            ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                            ErrorCode = 9999;
                            //FPNumber = 0;
                            ByteResult = 9999;
                            ByteStatus = 9999;

                            FiscalNumber = "";

                            SerialNumber = "";


                            CurrentDate = "";
                            CurrentTime = "";
                        }
                        try
                        {
                            SmenaOpened = ics.GetSmenaOpened;
                        }
                        catch {
                            SmenaOpened = false;
                            connect = false;
                            ErrorInfo = "ЭККР не отвечает!";
                            Error = true;
                            ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                            ErrorCode = 9999;
                            //FPNumber = 0;
                            ByteResult = 9999;
                            ByteStatus = 9999;

                            FiscalNumber = "";

                            SerialNumber = "";


                            CurrentDate = "";
                            CurrentTime = "";
                        };

                        if (SerialNumber == null) SerialNumber = "";
                        if ((ErrorInfo == "<КОМАНДА НЕ ПРИНЯТА К ИСПОЛНЕНИЮ>\r\nПревышение продолжительности смены\r\n") || (ByteResult == 41) || (ByteStatus == 32) || (ByteStatus == 39))
                        {
                            ics.FPResetOrder();
                            bool result = true;
                            //UInt32 Money = Convert.ToUInt32(ics.GetMoneyInBox); // Если вдруг чего осталось изымаем
                            UInt32 Money = ics.GetMoneyInBox; // Если вдруг чего осталось изымаем
                            if (Money > 0)
                            {
                                result = ics.FPOutOfCash(Money);
                            }
                            result = ics.FPDayClrReport(0);

                        }
                        //  ics.
                        try
                        {
                            //if (!ics.GetLastCommandStatus)
                            CurrentDate = Convert.ToString(ics.CurrentDate);
                            if (CurrentDate == null) CurrentDate = "";
                            //if (!ics.GetLastCommandStatus)
                            CurrentTime = Convert.ToString(ics.CurrentTime);
                            if (CurrentTime == null) CurrentTime = "";
                            if ((ByteResult != 0) || (ByteStatus != 0))
                                Error = true;
                        }
                        catch
                        {
                            Error = true;
                            ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                            ErrorCode = 9999;
                            //FPNumber = 0;
                            ByteResult = 9999;
                            ByteStatus = 9999;

                            FiscalNumber = "";

                            SerialNumber = "";


                            CurrentDate = "";
                            CurrentTime = "";
                        }
                    }
                }
                if ((!connect) || (ErrorInfo == "ЭККР не отвечает!")) 
                {
                    Error = true;
                    ErrorInfo = ErrorInfo == null ? "Подключение не выполнено, не возможно подключиться через COM порт" : ErrorInfo;
                    ErrorCode = 9999;
                    //FPNumber = 0;
                    ByteResult = 9999;
                    ByteStatus = 9999;

                    FiscalNumber = "";

                    SerialNumber = "";


                    CurrentDate = "";
                    CurrentTime = "";

                }

                if (con.State == ConnectionState.Open) 
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FpWork].[dbo].[tbl_ComInit] SET [Error]=@Error
                                                                                                                                         ,[ErrorCode]=@ErrorCode
                                                                                                                                         ,[ErrorInfo]=@ErrorInfo
                                                                                                                                         ,[FPNumber]=case when @FPNumber=0 then [FPNumber] else @FPNumber end
                                                                                                                                        ,[SerialNumber]=@SerialNumber 
                                                                                                                                        ,[FiscalNumber]=@FiscalNumber 
                                                                                                                                        ,[CurrentDate]=@CurrentDate 
                                                                                                                                        ,[CurrentTime]=@CurrentTime 
                                                                                                                                        ,[CurrentSystemDateTime]=GetDate() 
                                                                                                                                        ,[ByteStatus]=@ByteStatus 
                                                                                                                                        ,[ByteResult]=@ByteResult 
                                                                                                                                        ,[Version]=@FPVersion
                                                                                                                                        ,[SmenaOpened]=@SmenaOpened
                                                                                                                                        ,[ByteReservInfo]=@ByteReservInfo
                                                                                                                                        ,[ByteStatusInfo]=@ByteStatusInfo
                                                                                                                                        ,[PapStat]=@PapStat
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        //commandUpdate.Transaction = trans;
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int);
                        commandUpdate.Parameters["@Port"].Value = Port;
                        //result = Connect.;
                        commandUpdate.Parameters.Add("@ErrorCode", SqlDbType.Int); commandUpdate.Parameters["@ErrorCode"].Value = ErrorCode;//ics.GetByteResult;
                        commandUpdate.Parameters.Add("@ErrorInfo", SqlDbType.NVarChar, 1024); commandUpdate.Parameters["@ErrorInfo"].Value = ErrorInfo;
                        commandUpdate.Parameters.Add("@FPNumber", SqlDbType.Int); commandUpdate.Parameters["@FPNumber"].Value = FPNumber;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = Error;
                        commandUpdate.Parameters.Add("@SerialNumber", SqlDbType.NVarChar, 20); commandUpdate.Parameters["@SerialNumber"].Value = SerialNumber;
                        commandUpdate.Parameters.Add("@FiscalNumber", SqlDbType.NVarChar, 20); commandUpdate.Parameters["@FiscalNumber"].Value = FiscalNumber;
                        commandUpdate.Parameters.Add("@CurrentDate", SqlDbType.NVarChar, 10); commandUpdate.Parameters["@CurrentDate"].Value = CurrentDate;
                        commandUpdate.Parameters.Add("@CurrentTime", SqlDbType.NVarChar, 10); commandUpdate.Parameters["@CurrentTime"].Value = CurrentTime;
                        commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = ByteStatus;
                        commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = ByteResult;
                        commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = ByteReserv;
                        commandUpdate.Parameters.Add("@ByteReservInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteReservInfo"].Value = ics.GetInfoReserv;
                        commandUpdate.Parameters.Add("@ByteStatusInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteStatusInfo"].Value = ics.GetInfoStatus;
                        commandUpdate.Parameters.Add("@FPVersion", SqlDbType.NVarChar, 5); commandUpdate.Parameters["@FPVersion"].Value = FPVersion;
                        commandUpdate.Parameters.Add("@SmenaOpened", SqlDbType.Bit); commandUpdate.Parameters["@SmenaOpened"].Value = ics.GetSmenaOpened;
                        commandUpdate.Parameters.Add("@PapStat", SqlDbType.NVarChar, 100); commandUpdate.Parameters["@PapStat"].Value = ics.GetPapStat;
                        //ics.FPClose();
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();


                    }
                }
                con.Close();
                con.Dispose();
            }
            //ics.FPClose();
            //ics = null;
        }

        void _SetError()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();
               

                if (con.State == ConnectionState.Open) 
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FpWork].[dbo].[tbl_ComInit] SET [Error]=@Error  
                                                                                                                    ,[CurrentSystemDateTime]=GetDate()                                                                                                                                       
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int);
                        commandUpdate.Parameters["@Port"].Value = Port;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = 1;
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
            //ics.FPClose();
            //ics = null;
        }


        void _SetErrorIcsConnection()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_ComInit] SET [Error]=@Error,[ErrorCode]=@ErrorCode, [Version]=@FPVersion, [ErrorInfo]=@ErrorInfo, [SmenaOpened]=@SmenaOpened ,[CurrentSystemDateTime]=GetDate()                                                                                                                                        
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int); commandUpdate.Parameters["@Port"].Value = Port;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = 1;
                        commandUpdate.Parameters.Add("@ErrorCode", SqlDbType.Int); commandUpdate.Parameters["@ErrorCode"].Value = 999;
                        commandUpdate.Parameters.Add("@SmenaOpened", SqlDbType.Bit); commandUpdate.Parameters["@SmenaOpened"].Value = SmenaOpened;
                        commandUpdate.Parameters.Add("@FPVersion", SqlDbType.NVarChar, 5); commandUpdate.Parameters["@FPVersion"].Value = FPVersion;
                        commandUpdate.Parameters.Add("@ErrorInfo", SqlDbType.NVarChar, 1024); commandUpdate.Parameters["@ErrorInfo"].Value = "Не удалось подключиться к ФП, проверьте подключение";
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
            //ics.FPClose();
            //ics = null;
        }

        void _SetErrorVersion()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_ComInit] SET [Error]=@Error,[Version]=@FPVersion, [ErrorInfo]=@ErrorInfo, [SmenaOpened]=@SmenaOpened ,[CurrentSystemDateTime]=GetDate()                                                                                                                                        
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int); commandUpdate.Parameters["@Port"].Value = Port;
                        commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = 1;
                        commandUpdate.Parameters.Add("@SmenaOpened", SqlDbType.Bit); commandUpdate.Parameters["@SmenaOpened"].Value = SmenaOpened;
                        commandUpdate.Parameters.Add("@FPVersion", SqlDbType.NVarChar,5); commandUpdate.Parameters["@FPVersion"].Value = FPVersion;
                        commandUpdate.Parameters.Add("@ErrorInfo", SqlDbType.NVarChar, 1024); commandUpdate.Parameters["@ErrorInfo"].Value = "Не правильная версия на ФР. Данная система работает только с версией ПО ЭККР (“ЕП-06”)";
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
            //ics.FPClose();
            //ics = null;
        }

        void _SetInfoAndPapStat(string PapStat, DateTime CurrentSystemDateTime, string CurrentDate, string CurrentTime,byte lByteStatus,byte lByteResult, byte lByteReserv)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.ConnectionString = ConnectionString;
                con.Open();


                if (con.State == ConnectionState.Open)
                {
                    using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_ComInit] SET [PapStat]=@PapStat                                                                                        
                                                                                        ,[CurrentDate]=@CurrentDate
                                                                                        ,[CurrentTime]=@CurrentTime
                                                                                        ,[SmenaOpened]=@SmenaOpened
                                                                                        ,[ByteStatus]=@ByteStatus 
                                                                                        ,[ByteResult]=@ByteResult 
                                                                                        ,[ByteReserv]=@ByteReserv
                                                                                        ,[ByteReservInfo]=@ByteReservInfo
                                                                                        ,[ByteStatusInfo]=@ByteStatusInfo
                                                                                            ,[CurrentSystemDateTime]=GetDate()
                                                                                        
                                                                                                                           where CompName=@CompName and [Port]=@Port", con))
                    {
                        commandUpdate.Parameters.Add("@CompName", SqlDbType.NVarChar, 256);
                        commandUpdate.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        commandUpdate.Parameters.Add("@Port", SqlDbType.Int); commandUpdate.Parameters["@Port"].Value = Port;
                        //commandUpdate.Parameters.Add("@PapStat", SqlDbType.NVarChar, 100); commandUpdate.Parameters["@PapStat"].Value = PapStat;
                        //commandUpdate.Parameters.Add("@CurrentSystemDateTime", SqlDbType.DateTime); commandUpdate.Parameters["@CurrentSystemDateTime"].Value = CurrentSystemDateTime;
                        commandUpdate.Parameters.Add("@CurrentDate", SqlDbType.NVarChar, 10); commandUpdate.Parameters["@CurrentDate"].Value = CurrentDate;
                        commandUpdate.Parameters.Add("@CurrentTime", SqlDbType.NVarChar, 10); commandUpdate.Parameters["@CurrentTime"].Value = CurrentTime;
                        commandUpdate.Parameters.Add("@SmenaOpened", SqlDbType.Bit); commandUpdate.Parameters["@SmenaOpened"].Value = ics.GetSmenaOpened;
                        commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = lByteStatus;
                        commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = lByteResult;
                        commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = lByteReserv;
                        commandUpdate.Parameters.Add("@ByteReservInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteReservInfo"].Value = ics.GetInfoReserv;
                        commandUpdate.Parameters.Add("@ByteStatusInfo", SqlDbType.NVarChar, 250); commandUpdate.Parameters["@ByteStatusInfo"].Value = ics.GetInfoStatus;                                                
                        commandUpdate.Parameters.Add("@PapStat", SqlDbType.NVarChar, 100); commandUpdate.Parameters["@PapStat"].Value = ics.GetPapStat;
                        commandUpdate.CommandTimeout = sqltimeout;
                        commandUpdate.ExecuteNonQuery();
                        commandUpdate.Dispose();
                    }
                }
                con.Close();
                con.Dispose();
            }
            //ics.FPClose();
            //ics = null;
        }


        bool PrintZ(out int ByteStatus,out int ByteResult, out int ByteReserv)
        {
            if (ics.GetFPCplCutter)
                ics.FPCplCutter();
            bool result = true;
            ics.FPResetOrder();
            ics.FPDayReport(0); 
            Random xrnd = new Random();
            Thread.Sleep(xrnd.Next(10, 60) * 1000);
            UInt32 Money = ics.GetMoneyInBox; // Если вдруг чего осталось изымаем
            if (Money > 0)
            {
                result = ics.FPOutOfCash(Money);
                if (!result)
                {
                    ByteStatus = ics.GetByteStatus;
                    ByteResult = ics.GetByteResult;
                    ByteReserv = ics.GetByteReserv;
                    SetError();
                    return result;
                }

            }
            ics.FPDayReport(0); xrnd = new Random();
            Thread.Sleep(xrnd.Next(10, 60) * 1000);
            //if (!ics.GetSmenaOpened)
            //    ics.FPNullCheck();
            result = ics.FPDayClrReport(0);
            if (!result) 
            {
                ByteStatus = ics.GetByteStatus;
                ByteResult = ics.GetByteResult;
                ByteReserv = ics.GetByteReserv;
                SetError();
            }
            ByteStatus = 0;
            ByteResult = 0;
            ByteReserv = ics.GetByteReserv;
            return result;
        }

        bool PrintX(out int ByteStatus, out int ByteResult, out int ByteReserv)
        {
            if (ics.GetFPCplCutter)
                ics.FPCplCutter();
            ics.FPResetOrder();
            bool result = ics.FPDayReport(0);
            if (!result) SetError();
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            return result;
        }

        bool PeriodicReport(out int ByteStatus, out int ByteResult, out int ByteReserv, DateTime DateBegin, DateTime DateEnd)
        {
            if (ics.GetFPCplCutter)
                ics.FPCplCutter();
            ics.FPResetOrder();
            bool result = ics.FPPeriodicReport(0, DateBegin, DateEnd);
            if (!result) SetError();
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            return result;
        }

        bool ChangeCashiers(string name, out int ByteStatus, out int ByteResult, out int ByteReserv)
        {
            ics.FPResetOrder();
            //ics.FPNullCheck();
            bool result = ics.FPSetCashier(0, name, 0, false);
            if (!result) SetError();
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            //ics.FPNullCheck();
            return result;
        }

        bool OutOfCash(UInt32 Summa, out int ByteStatus, out int ByteResult, out int ByteReserv)
        {
            if (ics.GetFPCplCutter)
                ics.FPCplCutter();
            ics.FPResetOrder();
            ics.FPDayReport(0);
            bool result = ics.FPOutOfCash(Summa);
            if (!result) SetError();
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            return result;
        }

        bool InToCash(UInt32 Summa, out int ByteStatus, out int ByteResult, out int ByteReserv)
        {
            if (ics.GetFPCplCutter)
                ics.FPCplCutter();
            ics.FPResetOrder();
            ics.FPDayReport(0);
            bool result = ics.FPInToCash(Summa);
            if (!result) SetError();
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            return result;
        }

        UInt32 selectrandom(UInt32 sum)
        {
            List<UInt32> Ba = new List<UInt32>();
            //Ba.Add(100);
            //Ba.Add(200);
            Ba.Add(500);
            Ba.Add(1000);
            Ba.Add(2000);
            Ba.Add(5000);
            //Ba.Add(10000);
            //Ba.Add(50000);

            List<UInt32> Ko = new List<UInt32>();
            Ko.Add(5);
            Ko.Add(10);
            Ko.Add(25);
            Ko.Add(50);

            Random rdn = new Random();
            string s_sum = sum.ToString();
            UInt32 Kop = 0;
            UInt32 TSum = sum;
            UInt32 RSum = 0;
            UInt32 SumKop = 0;
            //////if ((s_sum.Length > 1)&& Convert.ToBoolean(rdn.Next(10)))
            //////{
            //////    UInt32 _Kop = Convert.ToUInt32(s_sum.Substring(s_sum.Length - 2, 2));
            //////    UInt32 _Kop10 = Convert.ToUInt32(s_sum.Substring(s_sum.Length - 2, 1) + "0");
            //////    if (Convert.ToBoolean(rdn.Next(10)))
            //////        Kop = _Kop;
            //////    else
            //////        Kop = _Kop10 + (UInt32)Ko[rdn.Next(1, 3)];
            //////}

            //////if (s_sum.Length > 3)
            //////{
            //////    Convert.ToUInt32(s_sum.Substring(0, s_sum.Length - 4) + "0000");
            //////}
            if (s_sum.Length > 3)
            {
                Kop = Convert.ToUInt32(s_sum.Substring(s_sum.Length - 2, 2));
                TSum = sum - Kop;
                RSum = 0 + Convert.ToUInt32(s_sum.Substring(0, s_sum.Length - 4) + "0000");
                SumKop = 0 + Convert.ToUInt32(s_sum.Substring(s_sum.Length - 2, 1) + "0"); ;
            }
            while (RSum <= TSum)
            {
                RSum = RSum + (UInt32)Ba[rdn.Next(0, 4)];
            }
            if (RSum < sum)
            {
                while (SumKop <= Kop)
                {
                    SumKop = SumKop + (UInt32)Ko[rdn.Next(0, 3)];
                }
            }
            else if (Kop > 0 && Convert.ToBoolean(rdn.Next(10)))
            {

                while (SumKop <= Kop)
                {
                    SumKop = SumKop + (UInt32)Ko[rdn.Next(0, 3)];
                }
            }
            return RSum + SumKop;
        }

        void updateFromExchangeItems(int _FPNumber, UInt64 _DateTime, UInt64 _NumPayment)
        {
            //tbl_exchangeItemsRest - остатки в основных еденицах
            //tbl_exchangeItemsFrom - Выбираемый товар
            //tbl_exchangeItemsTo - заменяемый товар
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    //Для обхода выбираем необходимый товар                    
                    using (SqlCommand command = new SqlCommand(@"SELECT 
                                                                tbl_SALES.[id]
                                                                ,tbl_SALES.[DATETIME]
                                                                ,tbl_SALES.[FPNumber]
                                                                ,tbl_SALES.[SORT]
                                                                ,tbl_SALES.[Type]
                                                                ,tbl_SALES.[FRECNUM]
                                                                ,tbl_SALES.[Amount]
                                                                ,tbl_SALES.[Amount_Status]
                                                                ,tbl_SALES.[IsOneQuant]
                                                                ,tbl_SALES.[Price]
                                                                ,tbl_SALES.[NalogGroup]
                                                                ,tbl_SALES.[MemoryGoodName]
                                                                ,tbl_SALES.[GoodName]
                                                                ,tbl_SALES.[packname]
                                                                ,piRest.rest/coefficient As tRest
                                                                ,tbl_SALES.[Amount]*coefficient as tAmount
                                                                ,piRest.Code as piCode
                                                         FROM [FPWork].[dbo].[tbl_SALES] as tbl_SALES with(rowlock)
                                                            inner join [FPWork].[dbo].[tbl_exchangeItemsFrom] as piFrom
															 on tbl_SALES.packname= piFrom.packcode  and piFrom.enable=1
														inner join [FPWork].[dbo].[tbl_exchangeItemsRest] as piRest with(rowlock)
															on tbl_SALES.Amount<piRest.rest/coefficient
                                                                and tbl_SALES.StrCode=piRest.Code
															    and piRest.enable=1
                                        where [FPNumber]=@FPNumber and [DATETIME]=@DATETIME and [NumPayment]=@NumPayment
                                        order by [SORT]", conn)) //заменить StrCode на packname
                    {
                        command.Parameters.Add("@FPNumber", SqlDbType.Int); command.Parameters["@FPNumber"].Value = _FPNumber;
                        command.Parameters.Add("@DATETIME", SqlDbType.BigInt); command.Parameters["@DATETIME"].Value = _DateTime;
                        command.Parameters.Add("@NumPayment", SqlDbType.BigInt); command.Parameters["@NumPayment"].Value = _NumPayment;
                        command.CommandTimeout = sqltimeout;
                        command.Transaction = tr;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_exchangeItemsRest]
                                                                                        SET [code] = @Code
                                                                                        ,[rest] = [rest]-@tAmount;
                                                                                
                                                                                UPDATE [tbl_SALES]
                                                                               SET 
                                                                                    [tbl_SALES].[Amount] = @Amount
                                                                                    ,[tbl_SALES].[Amount_Status] = @Amount_Status
                                                                                    ,[tbl_SALES].[IsOneQuant] = @IsOneQuant
                                                                                    ,[tbl_SALES].[NalogGroup] = @NalogGroup
                                                                                    ,[tbl_SALES].[GoodName] = @GoodName
                                                                                    ,[tbl_SALES].[packname] = @packname
                                                                                    ,[tbl_SALES].[StrCode] = @StrCode
                                                                                    ,[tbl_SALES].[exchange]=1
                                                                                from [FPWork].[dbo].[tbl_SALES]
                                                                                inner join [FPWork].[dbo].[tbl_exchangeItemsTo] as to
                                                                                    on
                                                                                
                                                                        WHERE [id]=@id                                                                                 
                                                                        ", conn))
                                {

                                    commandUpdate.Parameters.Add("@id", SqlDbType.BigInt); commandUpdate.Parameters["@id"].Value = reader["id"];
                                    commandUpdate.Parameters.Add("@tAmount", SqlDbType.Int); commandUpdate.Parameters["@tAmount"].Value = reader["tAmount"];
                                    commandUpdate.Parameters.Add("@piCode", SqlDbType.NVarChar); commandUpdate.Parameters["@piCode"].Value = reader["piCode"];
                                    commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = ics.GetByteReserv;
                                    commandUpdate.Transaction = tr;
                                    commandUpdate.CommandTimeout = sqltimeout;
                                    commandUpdate.ExecuteNonQuery();
                                    commandUpdate.Dispose();
                                }
                            }
                        }
                    }
                       tr.Commit();
                }
                conn.Close();
                conn.Dispose();
            }

        }

        bool PrintCheck(UInt64 _NumPayment, UInt32 _Payment, UInt32 _Payment0, UInt32 _Payment1, UInt32 _Payment2, UInt32 _Payment3, bool _CheckClose, bool _FiscStatus, string _Comment, int _FPNumber, UInt64 _DateTime, int FRECNUM, out int ByteStatus, out int ByteResult, out int ByteReserv, out bool CloseOperation, List<object> Dop, out Int64 SumCheck, UInt32 RowCount)
        {
            
            ics.FPResetOrder();
            SumCheck = 0;
            Int32 RealRowCount = 0;
            bool ErrorSumCheck = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    //Что бы работало по кодам необходимо заменить StrCode на packname
                    // Это позволит убрать проблемы в работе и приведет к единому стандарту....
                    //conn.Open();
                    using (SqlCommand command = new SqlCommand(@"SELECT 
                                                                [id]
                                                                ,[DATETIME]
                                                                ,[FPNumber]
                                                                ,[SORT]
                                                                ,[Type]
                                                                ,[FRECNUM]
                                                                ,[Amount]
                                                                ,[Amount_Status]
                                                                ,[IsOneQuant]
                                                                ,[Price]
                                                                ,[NalogGroup]
                                                                ,[MemoryGoodName]
                                                                ,[GoodName]
                                                                ,[packname]
                                                         FROM [FPWork].[dbo].[tbl_SALES] with(rowlock)
                                        where [FPNumber]=@FPNumber and [DATETIME]=@DATETIME and [NumPayment]=@NumPayment
                                        order by [SORT]", conn)) //заменить StrCode на packname
                    {
                        command.Parameters.Add("@FPNumber", SqlDbType.Int); command.Parameters["@FPNumber"].Value = _FPNumber;
                        command.Parameters.Add("@DATETIME", SqlDbType.BigInt); command.Parameters["@DATETIME"].Value = _DateTime;
                        command.Parameters.Add("@NumPayment", SqlDbType.BigInt); command.Parameters["@NumPayment"].Value = _NumPayment;
                        command.CommandTimeout = sqltimeout;
                        command.Transaction = tr;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            double Sumcheck = 0;
                            
                            while (reader.Read())
                            {
                                ///Int64 Temp1 = ics.GetCheckTotal;
                                int step = (int)reader["Amount_Status"];
                                if (step != 0)
                                    Sumcheck = Sumcheck + Math.Round(Convert.ToInt32(reader["Amount"]) / System.Math.Pow(10, step) * Convert.ToInt32(reader["Price"]), 0);
                                else
                                    Sumcheck = Sumcheck + Convert.ToInt32(reader["Amount"]) * Convert.ToInt32(reader["Price"]);

                                Error = !ics.FPSaleEx(Convert.ToUInt32(reader["Amount"])
                                                         , Convert.ToByte(reader["Amount_Status"])
                                                         , Convert.ToBoolean(reader["IsOneQuant"])
                                                         , Convert.ToInt32(reader["Price"])
                                                         , Convert.ToUInt16(reader["NalogGroup"])
                                                         , Convert.ToBoolean(reader["MemoryGoodName"])
                                                         , reader["GoodName"].ToString()
                                                         , reader["packname"].ToString().Trim()); //заменить StrCode на packname

                                using (SqlCommand commandUpdate = new SqlCommand(@"UPDATE [FPWork].[dbo].[tbl_SALES]
                                                                               SET 
                                                                                    [Error] = @Error
                                                                                    ,[ByteStatus] = @ByteStatus
                                                                                    ,[ByteResult] = @ByteResult
                                                                                    ,[ByteReserv] = @ByteReserv                                                                                    
                                                                        WHERE [id]=@id                                                                                 
                                                                        ", conn))
                                {

                                    commandUpdate.Parameters.Add("@id", SqlDbType.BigInt); commandUpdate.Parameters["@id"].Value = reader["id"];
                                    commandUpdate.Parameters.Add("@Error", SqlDbType.Bit); commandUpdate.Parameters["@Error"].Value = Error;
                                    commandUpdate.Parameters.Add("@ByteStatus", SqlDbType.Int); commandUpdate.Parameters["@ByteStatus"].Value = ics.GetByteStatus;
                                    commandUpdate.Parameters.Add("@ByteResult", SqlDbType.Int); commandUpdate.Parameters["@ByteResult"].Value = ics.GetByteResult;
                                    commandUpdate.Parameters.Add("@ByteReserv", SqlDbType.Int); commandUpdate.Parameters["@ByteReserv"].Value = ics.GetByteReserv;
                                    commandUpdate.Transaction = tr;
                                    commandUpdate.CommandTimeout = sqltimeout;
                                    commandUpdate.ExecuteNonQuery();
                                    commandUpdate.Dispose();
                                }



                                //Int64 Temp = ics.GetCheckTotal;
                                //int a=0;
                                if (Error)
                                {

                                    //reader.Close();
                                    SetError();
                                    //return;
                                }
                                else RealRowCount++;
                            }

                            Int64 SumInFP = ics.GetCheckTotal;
                            double Razn = SumInFP - Sumcheck;
                            Razn = Razn < 0 ? -Razn : Razn;
                            if ((SumInFP != Sumcheck) && (Razn > 5) || (RowCount!=RealRowCount))
                            {
                                ErrorSumCheck = true;
                                ics.FPResetOrder();
                            }

                            if ((reader.HasRows) && (!ErrorSumCheck))
                            {
                                if ((Convert.ToUInt32(Dop[1]) != 0) || (Convert.ToUInt32(Dop[2]) != 0) || (Convert.ToUInt32(Dop[3]) != 0) || (Convert.ToUInt64(Dop[4]) != 0))
                                {
                                    ics.FPComment("Оплачено бонусами:");
                                    ics.FPComment(string.Format("{0:0.00}", Convert.ToDecimal(Dop[1]) / 100));
                                    ics.FPComment("Бонусов на счету:");
                                    ics.FPComment(string.Format("{0:0.00}", Convert.ToDecimal(Dop[2]) / 100));
                                    ics.FPComment("Начислено бонусов:");
                                    ics.FPComment(string.Format("{0:0.00}", Convert.ToDecimal(Dop[3]) / 100));
                                    ics.FPComment("Покупатель:");
                                    ics.FPComment(Dop[4].ToString());
                                }
                                SumCheck = ics.GetCheckTotal;


                                //edtAbsRecDisc 4  Абсолютная скидка на промежуточную сумму чека(выражается в копейках). 
                                //edtAbsRecAdd 5  Абсолютная наценка на промежуточную сумму чека(выражается в копейках). 
                                byte disc = 0;
                                UInt32 discval = 0;
                                UInt32 CheckSum = Convert.ToUInt32(Dop[0]);
                                //UInt32 payment = CheckSum;
                                if (CheckSum == 0)
                                    CheckSum = _Payment;
                                if (SumInFP > CheckSum)
                                {
                                    disc = 4;
                                    discval = (UInt32)(SumInFP - CheckSum);
                                }
                                else if (SumInFP < CheckSum)
                                {
                                    disc = 5;
                                    discval = (UInt32)(CheckSum - SumInFP);
                                }
                                if (SumInFP != CheckSum && discval > 0)
                                    ics.FPDiscount(disc, discval, "");

                                SumCheck = ics.GetCheckTotal;
                                if ((_Payment0 > 0))
                                    Error = !ics.FPPayment(0, _Payment0, false, _FiscStatus, _Comment);
                                else if ((_Payment1 > 0))
                                    Error = !ics.FPPayment(1, _Payment1, false, _FiscStatus, _Comment);
                                else if ((_Payment2 > 0))
                                    Error = !ics.FPPayment(2, _Payment2, false, _FiscStatus, _Comment);
                                else if ((_Payment3 > 0))
                                    Error = !ics.FPPayment(3, _Payment3, false, _FiscStatus, _Comment);

                                //UpdatePayment(_FPNumber, _DateTime, _NumPayment, ics.GetByteStatus, ics.GetByteResult, ics.GetByteReserv, Error, SumCheck);

                                


                                //if ((_Payment_Status == 3) && (ics.GetCheckTotal <= _Payment))
                                //{
                                //    Error = !ics.FPPayment(_Payment_Status, _Payment, false, _FiscStatus, _Comment);
                                //    //Error = !ics.FPPayment(_Payment_Status, selectrandom((UInt32)SumCheck), false, _FiscStatus, _Comment);
                                //}
                                //if (ics.GetCheckOpened)
                                //{
                                //    //edtAbsRecDisc 4  Абсолютная скидка на промежуточную сумму чека(выражается в копейках). 
                                //    //edtAbsRecAdd 5  Абсолютная наценка на промежуточную сумму чека(выражается в копейках). 
                                //    byte disc=0;
                                //    UInt32 discval=0;
                                //    UInt32 CheckSum = Convert.ToUInt32(Dop[0]);
                                //    //UInt32 payment = CheckSum;
                                //    if (CheckSum == 0)
                                //        CheckSum = _Payment;
                                //    if (SumInFP > CheckSum)
                                //    {
                                //        disc = 4;
                                //        discval = (UInt32)(SumInFP - CheckSum);
                                //    }
                                //    else if (SumInFP < CheckSum)
                                //    {
                                //        disc = 5;
                                //        discval = (UInt32)(CheckSum - SumInFP);
                                //    }
                                //    if (SumInFP != CheckSum && discval > 0)
                                //        ics.FPDiscount(disc, discval, "");
                                //    Error = !ics.FPPayment(3, _Payment, _CheckClose, _FiscStatus, _Comment);
                                //}
                            }
                            reader.Close();
                            if (Error)
                            {

                                SetError();

                            }
                        }

                    }
                    tr.Commit();
                }
                conn.Close();
                conn.Dispose();
            }
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            CloseOperation = !ErrorSumCheck;
            return !Error;
        }

        bool PrintCheckPay(UInt64 _NumPayment, UInt32 _Payment, UInt32 _Payment0, UInt32 _Payment1, UInt32 _Payment2, UInt32 _Payment3, bool _CheckClose, bool _FiscStatus, string _Comment, int _FPNumber, UInt64 _DateTime, int FRECNUM, out int ByteStatus, out int ByteResult, out int ByteReserv, out bool CloseOperation, List<object> Dop, out Int64 SumCheck)
        {
            SumCheck = 0;
            ics.FPResetOrder();
            bool ErrorSumCheck = false;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(@"SELECT [DATETIME]
                                                                ,[FPNumber]
                                                                ,[SORT]
                                                                ,[Type]
                                                                ,[FRECNUM]
                                                                ,[Amount]
                                                                ,[Amount_Status]
                                                                ,[IsOneQuant]
                                                                ,[Price]
                                                                ,[NalogGroup]
                                                                ,[MemoryGoodName]
                                                                ,[GoodName]
                                                                ,[packname]
                                                         FROM [FPWork].[dbo].[tbl_SALES] with(rowlock)
                                        where [FPNumber]=@FPNumber and [DATETIME]=@DATETIME and [NumPayment]=@NumPayment
                                        order by [SORT]", conn)) //заменить StrCode на packname
                {
                    command.Parameters.Add("@FPNumber", SqlDbType.Int); command.Parameters["@FPNumber"].Value = _FPNumber;
                    command.Parameters.Add("@DATETIME", SqlDbType.BigInt); command.Parameters["@DATETIME"].Value = _DateTime;
                    command.Parameters.Add("@NumPayment", SqlDbType.BigInt); command.Parameters["@NumPayment"].Value = _NumPayment;
                    command.CommandTimeout = sqltimeout;
                    //command.Transaction = trans;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        double Sumcheck = 0;
                        while (reader.Read())
                        {
                            int step = (int)reader["Amount_Status"];
                            if (step != 0)
                                Sumcheck = Sumcheck + Math.Round(Convert.ToInt32(reader["Amount"]) / System.Math.Pow(10, step) * Convert.ToInt32(reader["Price"]), 0);
                            else
                                Sumcheck = Sumcheck + Convert.ToInt32(reader["Amount"]) * Convert.ToInt32(reader["Price"]);

                            Error = !ics.FPPayMoneyEx(Convert.ToUInt32(reader["Amount"])
                                                     , Convert.ToByte(reader["Amount_Status"])
                                                     , Convert.ToBoolean(reader["IsOneQuant"])
                                                     , Convert.ToInt32(reader["Price"])
                                                     , Convert.ToUInt16(reader["NalogGroup"])
                                                     , Convert.ToBoolean(reader["MemoryGoodName"])
                                                     , reader["GoodName"].ToString()
                                                     , reader["packname"].ToString().Trim()); //заменить StrCode на packname
                            if (Error)
                            {

                                //reader.Close();
                                SetError();
                                //return;
                            }
                        }

                        Int64 SumInFP = ics.GetCheckTotal;
                        double Razn = SumInFP - Sumcheck;
                        Razn = Razn < 0 ? -Razn : Razn;
                        if ((SumInFP != Sumcheck) && (Razn > 5))
                        {
                            ErrorSumCheck = true;
                            ics.FPResetOrder();
                        }

                        if ((reader.HasRows) && (!ErrorSumCheck))
                        {
                            if ((Convert.ToUInt32(Dop[1]) != 0) || (Convert.ToUInt32(Dop[2]) != 0) || (Convert.ToUInt32(Dop[3]) != 0) || (Convert.ToUInt64(Dop[4]) != 0))
                            {
                                ics.FPComment("Оплачено бонусами:");
                                ics.FPComment(string.Format("{0:0.00}", Convert.ToDecimal(Dop[1]) / 100));
                                ics.FPComment("Бонусов на счету:");
                                ics.FPComment(string.Format("{0:0.00}", Convert.ToDecimal(Dop[2]) / 100));
                                ics.FPComment("Начислено бонусов:");
                                ics.FPComment(string.Format("{0:0.00}", Convert.ToDecimal(Dop[3]) / 100));
                                ics.FPComment("Покупатель:");
                                ics.FPComment(Dop[4].ToString());
                            }

                            SumCheck = ics.GetCheckTotal;


                            byte disc = 0;
                            UInt32 discval = 0;
                            UInt32 CheckSum = Convert.ToUInt32(Dop[0]);
                            //UInt32 payment = CheckSum;
                            if (CheckSum == 0)
                                CheckSum = _Payment;
                            if (SumInFP > CheckSum)
                            {
                                disc = 4;
                                discval = (UInt32)(SumInFP - CheckSum);
                            }
                            else if (SumInFP < CheckSum)
                            {
                                disc = 5;
                                discval = (UInt32)(CheckSum - SumInFP);
                            }
                            if (SumInFP != CheckSum && discval > 0)
                                ics.FPDiscount(disc, discval, "");

                            SumCheck = ics.GetCheckTotal;

                            if ((_Payment0 > 0))
                                Error = !ics.FPPayment(0, _Payment0, false, _FiscStatus, _Comment);
                            else if ((_Payment1 > 0))
                                Error = !ics.FPPayment(1, _Payment1, false, _FiscStatus, _Comment);
                            else if ((_Payment2 > 0))
                                Error = !ics.FPPayment(2, _Payment2, false, _FiscStatus, _Comment);
                            else if ((_Payment3 > 0))
                                Error = !ics.FPPayment(3, _Payment3, false, _FiscStatus, _Comment);


                            UpdatePayment(_FPNumber, _DateTime, _NumPayment, ics.GetByteStatus, ics.GetByteResult, ics.GetByteReserv, Error, SumCheck);

                            //if ((_Payment_Status == 3) && (ics.GetCheckTotal <= _Payment))
                            //    Error = !ics.FPPayment(_Payment_Status, _Payment, false, _FiscStatus, _Comment);
                            //else
                            //{
                            //    byte disc = 0;
                            //    UInt32 discval = 0;
                            //    UInt32 CheckSum = Convert.ToUInt32(Dop[0]);
                            //    //UInt32 payment = CheckSum;
                            //    if (CheckSum == 0)
                            //        CheckSum = _Payment;
                            //    if (SumInFP > CheckSum)
                            //    {
                            //        disc = 4;
                            //        discval = (UInt32)(SumInFP - CheckSum);
                            //    }
                            //    else if (SumInFP < CheckSum)
                            //    {
                            //        disc = 5;
                            //        discval = (UInt32)(CheckSum - SumInFP);
                            //    }
                            //    if (SumInFP != CheckSum && discval > 0)
                            //        ics.FPDiscount(disc, discval, "");

                            //    Error = !ics.FPPayment(_Payment_Status, _Payment, _CheckClose, _FiscStatus, _Comment);
                            //}

                        }
                        reader.Close();
                        if (Error)
                        {

                            SetError();

                        }
                    }

                }
                conn.Close();
                conn.Dispose();
            }
            ByteStatus = ics.GetByteStatus;
            ByteResult = ics.GetByteResult;
            ByteReserv = ics.GetByteReserv;
            CloseOperation = !ErrorSumCheck;
            return !Error;
        }

        bool AllGood()
        {
            bool auto = true;
            bool result = true;
            if (!(bool)ics.GetSmenaOpened)
            {

            }
            if (((bool)ics.GetCheckOpened) && result && auto)
            {
                result = ics.FPResetOrder();
            }
            //if ((ics.GetCutterStatus) && result && auto)
            //{
            //    //отключаем автообрезчик
            //    result = ics.FPCplCutter();
            //}
            //if ((ics.GetControlDisplay) && result && auto)
            //{
            //    result = ics.FPCplInd();
            //}
            if (!result)
            {
                SetError();
            }
            return result;
        }




    }

    public static class FPIKSInfo
    {
        private static int sqltimeout = 300;
        public static List<int> ComPort
        {
            get
            {
                List<int> _ComPort = new List<int>();
                string CurrDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FPIKSWork)).CodeBase);
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(CurrDir + "\\ConnectionString.xml");
                XmlNode list = xdoc.SelectSingleNode("/root/ConnectionString");
                 string ConnectionString = list.InnerText;
                 using (SqlConnection con = new SqlConnection(ConnectionString))
                 {
                     con.Open();
                     using (SqlCommand command = new SqlCommand(@"SELECT [CompName]
                                                                    ,[Port]
                                                                    ,[Init]
                                                                    ,[Error]  
                                                    FROM [FPWork].[dbo].[tbl_ComInit] with(rowlock)
                                                    WHERE CompName=@CompName and Init=1 and Error=0
                                                ", con))
                     {
                         command.Parameters.Add("@CompName", SqlDbType.NVarChar); command.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                         command.CommandTimeout = sqltimeout;
                         using (SqlDataReader reader = command.ExecuteReader())
                         {
                             while (reader.Read())
                             {
                                 _ComPort.Add(Convert.ToInt32(reader["Port"]));
                             }
                             reader.Close();
                             
                         }
                         
                         command.Dispose();
                     }
                     con.Close();
                     con.Dispose();                     
                 }
                 xdoc = null;
                 list = null;


                 return _ComPort;
            }
        }

        public static List<int> ComPortErrors
        {
            get
            {
                Thread.Sleep(1000);
                List<int> _ComPort = new List<int>();
                string CurrDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FPIKSWork)).CodeBase);
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(CurrDir + "\\ConnectionString.xml");
                XmlNode list = xdoc.SelectSingleNode("/root/ConnectionString");
                string ConnectionString = list.InnerText;
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    using (SqlCommand command = new SqlCommand(@"SELECT [CompName]
                                                                    ,[Port]
                                                                    ,[Init]
                                                                    ,[Error]  
                                                    FROM [FPWork].[dbo].[tbl_ComInit] with(rowlock)
                                                    WHERE CompName=@CompName and Init=1 and Error=1
                                                ", con))
                    {
                        command.Parameters.Add("@CompName", SqlDbType.NVarChar); command.Parameters["@CompName"].Value = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
                        command.CommandTimeout = sqltimeout;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _ComPort.Add(Convert.ToInt32(reader["Port"]));
                            }
                            reader.Close();
                        }
                        command.Dispose();
                    }
                    con.Close();
                    con.Dispose();  
                }
                list = null;
                xdoc = null;

                return _ComPort;
            }
        }

    }
}