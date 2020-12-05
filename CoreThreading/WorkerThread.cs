using System;
using System.Diagnostics;
using System.Threading;

namespace CoreThreading
{
    class WorkerThread
    {
        public WorkerThread(
            IWork work,
            IDispatcher invokingThread)
        {
            Work = work;
            InvokingThread = invokingThread;
        }

        public Action<IWork> BeforeCallback { get; set; }
        public Action<IWork> WorkEndedCallback { get; set; }
        public Action<IWork> AfterCallback { get; set; }

        public bool Abort
        {
            get
            {
                return Work.Abort;
            }
            set
            {
                Work.Abort = value;
            }
        }
        public void WaitForWorkerThread()
        {
            while (!Object.ReferenceEquals(Work, null))
            {
                Thread.Sleep(10);
            }
        }

        public IWork Work { get; private set; }
        private IDispatcher InvokingThread { get; set; }

        public void Before()
        {
            if (BeforeCallback != null)
            {
                BeforeCallback(Work);
            }

            Work.Before();
        }
        public void Run(Object state)
        {
            Work.Run();

            if (WorkEndedCallback != null)
            {
                WorkEndedCallback(Work);
            }

            if (InvokingThread != null)
            {
                InvokingThread.BeginInvoke(delegate()
                {
                    After();
                });
            }
        }
        public void After()
        {
            Work.After();

            if (AfterCallback != null)
            {
                AfterCallback(Work);
            }

            Work = null;
        }
        public void RunSingleThreaded()
        {
            Work.Run();
            Work.After();

            if (AfterCallback != null)
                AfterCallback(Work);
        }
    }
}
