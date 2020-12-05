using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreThreading
{
    public enum IWorkType
    {
        Invisible,
        Background,
        AbortableLock,
        Lock
    }

    public interface IWork
    {
        void Before();
        void Run();
        void After();

        IWorkType Type { get; }
        bool Abort { get; set; }
    }
}
