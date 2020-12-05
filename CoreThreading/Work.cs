using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreThreading
{
    public class Work : IWork
    {
        public Work()
        {
        }
        public Work(
            Action<IWork> before,
            Action<IWork> run,
            Action<IWork> after)
            : this()
        {
            BeforeCallback = before;
            RunCallback = run;
            AfterCallback = after;
        }

        public Action<IWork> BeforeCallback { get; set; }
        public Action<IWork> RunCallback { get; set; }
        public Action<IWork> AfterCallback { get; set; }

        public void Before()
        {
            if (BeforeCallback != null)
                BeforeCallback(this);
        }
        public void Run()
        {
            RunCallback(this);
        }
        public void After()
        {
            if (AfterCallback != null)
                AfterCallback(this);
        }
        public IWorkType Type
        {
            get
            {
                return IWorkType.Background;
            }
        }
        public bool Abort
        {
            get
            {
                return false;
            }
            set
            {
            }
        }
    }
}
