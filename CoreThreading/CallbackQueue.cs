using System.Collections.Generic;
using System.Threading;
using System;

namespace CoreThreading
{
    // Implements an IDispatcher than calls all enqueued callbacks in Run()
    public class CallbackQueue : IDispatcher
    {
        public void BeginInvoke(Action callback)
        {
            lock (queue)
            {
                queue.Enqueue(callback);
            }
            queueEvent.Set();
        }
        public void BeginInvokeLowPrio(Action callback)
        {
            BeginInvoke(callback);
        }
        public void Exit()
        {
            BeginInvoke(delegate() { Alive = false; });
        }
        public void Run()
        {
            Alive = true;

            while (Alive)
            {
                queueEvent.WaitOne();

                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        queue.Dequeue()();
                    }
                }
            }
        }

        private readonly Queue<Action> queue = new Queue<Action>();
        private readonly AutoResetEvent queueEvent = new AutoResetEvent(false);
        private bool Alive { get; set; }
    }
}
