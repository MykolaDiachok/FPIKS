using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using FPIKSWork;

namespace FPIKSService
{
    public partial class FPIKSService : ServiceBase
    {


        static Thread[] thread = new Thread[255];// = new Thread(TEST1);
        static FPIKSWork.FPIKSWork[] _fpw = new FPIKSWork.FPIKSWork[255];
        static Thread[] threadErrors = new Thread[255];
        static Mutex[] mu = new Mutex[255];
        static System.Timers.Timer timer1;
        static System.Timers.Timer timerE;
        

 
        public FPIKSService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            for (int x = 0; x < 255; x++)
            {
                mu[x] = new Mutex(false, "Com" + x.ToString());
                _fpw[x] = new FPIKSWork.FPIKSWork(x);
                //_fpw[x] = new FPIKSWork.FPIKSWork(x);
                //thread[x] = new Thread(new ParameterizedThreadStart(_fpw[x].Startjob));
                //thread[x].Name = "";
                //_fpwErrors[x] = new FPIKSWork.FPIKSWork(x);
                //threadErrors[x] = new Thread(new ParameterizedThreadStart(_fpwErrors[x].TestConnection));

            }



            timer1 = new System.Timers.Timer();
            timer1.Enabled = true;
            //Интервал 10000мс - 10с.
            timer1.Interval = 2000;
            timer1.Elapsed +=
             new System.Timers.ElapsedEventHandler(WorkJobs);
            timer1.AutoReset = true;
            timer1.Start();

            timerE = new System.Timers.Timer();
            timerE.Enabled = true;
            //Интервал 10000мс - 10с.
            timerE.Interval = 30000;
            timerE.Elapsed +=
             new System.Timers.ElapsedEventHandler(WorkJobsErrors);
            timerE.AutoReset = true;
            timerE.Start();
 
        }

        protected override void OnStop()
        {
            timer1.Enabled = false;
            timer1.Dispose();
            timerE.Enabled = false;
            timerE.Dispose();

        }




        static void WorkJobs(object sender, System.Timers.ElapsedEventArgs e)
        {


            timer1.Enabled = false;


            try
            {
                List<int> comports = FPIKSInfo.ComPort;

                foreach (int comport in comports)
                {


                    if ((thread[comport] == null) || (thread[comport].ThreadState != System.Threading.ThreadState.Running && thread[comport].ThreadState != System.Threading.ThreadState.WaitSleepJoin && thread[comport].ThreadState != System.Threading.ThreadState.Unstarted))
                    {
                        thread[comport] = new Thread(new ParameterizedThreadStart(_fpw[comport].Startjob));

                        thread[comport].Start(mu[comport]);

                    }

                }
                comports = null;

            }
            catch (Exception ex)
            {
                EventLog m_EventLog = new EventLog("");
                m_EventLog.Source = "FPIKSErrors";
                m_EventLog.WriteEntry("Ошибка в работе сервиса::WorkJobs::" + ex.Message,
                    EventLogEntryType.Warning);
                m_EventLog = null;
            }
            finally
            {
                //GC.Collect();
                Thread.Sleep(250);
                timer1.Enabled = true;
            }


        }


        static void WorkJobsErrors(object sender, System.Timers.ElapsedEventArgs e)
        {


            //timerE.Enabled = false;


            try
            {

                List<int> comportsError = FPIKSInfo.ComPortErrors;
                foreach (int comport in comportsError)
                {
                    //_fpwErrors[comport] = new FPIKSWork.FPIKSWork(comport);
                    if ((thread[comport] == null) || (thread[comport].ThreadState != System.Threading.ThreadState.Running && thread[comport].ThreadState != System.Threading.ThreadState.WaitSleepJoin && thread[comport].ThreadState != System.Threading.ThreadState.Unstarted))
                    {
                        thread[comport] = new Thread(new ParameterizedThreadStart(_fpw[comport].TestConnection));
                        thread[comport].Start(mu[comport]);

                    }
                    //_fpw.TestConnection();
                    //_fpw.Dispose();
                    //incE++;
                }
                comportsError = null;


            }
            catch (Exception ex)
            {
                EventLog m_EventLog = new EventLog("");
                m_EventLog.Source = "FPIKSErrors";
                m_EventLog.WriteEntry("Ошибка в работе сервиса::WorkJobsErrors::" + ex.Message,
                    EventLogEntryType.Warning);
                m_EventLog = null;
            }
            finally
            {
                GC.Collect();
                Thread.Sleep(250);
            }
            //serviceTimer.Change(new TimeSpan(0, 0, 5), new TimeSpan(0, 0, 5));

        }



    }
}
