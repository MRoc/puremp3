using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using CoreThreading;

namespace CoreControls.Threading
{
    // Implementation of IDispatcher forwarding to
    // a System.Windows.Threading.Dispatcher
    public class WpfDispatcher : IDispatcher
    {
        public WpfDispatcher(Dispatcher dispatcher)
        {
            WorkDispatcher = dispatcher;
        }
        public void BeginInvoke(Action callback)
        {
            WorkDispatcher.BeginInvoke(callback);
        }
        public void BeginInvokeLowPrio(Action callback)
        {
            WorkDispatcher.BeginInvoke(callback, DispatcherPriority.ContextIdle);
        }

        private Dispatcher WorkDispatcher { get; set; }
    }
}
