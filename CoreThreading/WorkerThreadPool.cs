using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using CoreThreading;

namespace CoreThreading
{
    public class WorkerThreadPool
    {
        private WorkerThreadPool()
        {
            Workers = new List<WorkerThread>();
        }
        public static WorkerThreadPool Instance
        {
            get
            {
                return instance;
            }
        }

        public bool SingleThreaded { get; set; }
        public Action<IWork> BeforeCallback { get; set; }
        public Action<IWork> AfterCallback { get; set; }
        public IDispatcher InvokingThread { get; set; }

        public void Exit()
        {
            lock (Workers)
            {
                foreach (var item in Workers)
                {
                    item.Abort = true;
                }
            }

            WorkerThreadPool.Instance.WaitForWorkers();
        }

        public void StartWork(IWork work)
        {
            WorkerThread worker = new WorkerThread(work, InvokingThread);

            worker.BeforeCallback += OnBefore;
            worker.WorkEndedCallback += OnWorkEnded;
            worker.AfterCallback += OnAfter;

            lock (Workers)
            {
                Workers.Add(worker);
            }

            worker.Before();

            if (SingleThreaded)
            {
                worker.RunSingleThreaded();
            }
            else
            {
                if (work.Type == IWorkType.Invisible)
                {
                    Thread thread = new Thread(worker.Run);
                    thread.Priority = ThreadPriority.Lowest;
                    thread.Start();
                }
                else
                {
                    Debug.Assert(!Object.ReferenceEquals(InvokingThread, null));
                    ThreadPool.QueueUserWorkItem(worker.Run);
                }
            }
        }
        public bool Abort
        {
            get
            {
                lock (Workers)
                {
                    return Workers.Where(n => n.Abort).Count() == 0;
                }
            }
            set
            {
                lock (Workers)
                {
                    foreach (var worker in Workers)
                    {
                        if (worker.Work.Type != IWorkType.Invisible)
                        {
                            worker.Abort = true;
                        }
                    }
                }
            }
        }
        public bool IsWorking
        {
            get
            {
                lock (Workers)
                {
                    return Workers.Where(n => n.Work.Type != IWorkType.Invisible).Count() > 0;
                }
            }
        }
        private void WaitForWorkers()
        {
            while (true)
            {
                lock (Workers)
                {
                    if (Workers.Count == 0)
                    {
                        return;
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void OnBefore(IWork work)
        {
            if (BeforeCallback != null)
            {
                BeforeCallback(work);
            }
        }
        private void OnWorkEnded(IWork work)
        {
            lock (Workers)
            {
                Workers.Remove(Workers.Where(n => n.Work == work).First());
            }
        }
        private void OnAfter(IWork work)
        {
            if (AfterCallback != null)
            {
                AfterCallback(work);
            }
        }

        private static WorkerThreadPool instance = new WorkerThreadPool();
        private List<WorkerThread> Workers { get; set; }
    }
}
