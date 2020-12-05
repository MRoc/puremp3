using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CoreThreading
{
    public class DelayedStringThreadQueue
    {
        public DelayedStringThreadQueue()
        {
            DelayInMilliSecs = 250;
        }

        public void Enqueue(string obj)
        {
            Debug.Assert(!Object.ReferenceEquals(obj, null));

            lock (queue)
            {
                lastEnqueue = DateTime.Now;
                queue.Enqueue(obj);
            }

            queueEvent.Set();
        }
        public string DequeueLast()
        {
            queueEvent.WaitOne();

            while ((DateTime.Now - LastEnqueue).TotalMilliseconds < DelayInMilliSecs && !Abort)
            {
                Thread.Sleep(50);
            }

            string result = null;

            lock (queue)
            {
                while (queue.Count > 0)
                {
                    result = queue.Dequeue();
                }
            }

            return result;
        }

        public int Count
        {
            get
            {
                lock (queue)
                {
                    return queue.Count;
                }
            }
        }
        public int DelayInMilliSecs
        {
            get;
            set;
        }

        private DateTime LastEnqueue
        {
            get
            {
                lock (queue)
                {
                    return lastEnqueue;
                }
            }
        }

        private bool abort;
        public bool Abort
        {
            set
            {
                abort = value;

                if (value)
                {
                    queueEvent.Set();
                }
            }
            get
            {
                return abort;
            }
        }

        private readonly AutoResetEvent queueEvent = new AutoResetEvent(false);
        private readonly Queue<string> queue = new Queue<string>();
        private DateTime lastEnqueue;
    }
}
