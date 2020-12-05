using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreTest
{
    public class ActionTest
    {
        public void Callback()
        {
            counter++;
        }

        public void TestWasCalledOnce()
        {
            TestWasCalled(1);
        }
        public void TestWasCalled(int calls)
        {
            UnitTest.Test(counter == calls);
            Clear();
        }

        public void Clear()
        {
            counter = 0;
        }

        private int counter;
    }
}
