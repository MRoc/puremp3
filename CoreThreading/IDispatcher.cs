using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreThreading
{
    // Interface for posting work on another thread
    public interface IDispatcher
    {
        void BeginInvoke(Action callback);
        void BeginInvokeLowPrio(Action callback);
    }
}
