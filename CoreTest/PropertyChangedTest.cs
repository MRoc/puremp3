using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreTest
{
    public class PropertyChangedTest
    {
        public void TestSenders<T>(Func<T, bool> test) where T : class
        {
            UnitTest.Test((from s in senders where test((T)s) select s).Count()
                == senders.Count);
            Clear();
        }

        public void TestArgs<T>(Func<T, bool> test) where T : EventArgs
        {
            UnitTest.Test((from e in args where test((T)e) select e).Count()
                == args.Count);
            Clear();
        }

        public void TestWasCalledOnce()
        {
            TestWasCalled(1);
        }
        public void TestWasCalled(int calls)
        {
            UnitTest.Test(args.Count == calls);
            Clear();
        }

        public void PropertyChanged(object sender, EventArgs e)
        {
            senders.Add(sender);
            args.Add(e);
        }

        public void Clear()
        {
            senders.Clear();
            args.Clear();
        }
        
        private List<object> senders = new List<object>();
        private List<EventArgs> args = new List<EventArgs>();
    }
}
