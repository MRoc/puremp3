using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CoreThreading
{
    class TestProgram
    {
        static CallbackQueue queue = new CallbackQueue();

        static void TestMultiThreadedMainFunc()
        {
            Console.Write("*");
            Thread.Sleep(10);
        }
        static void TestMultiThreadedThreadFunc(object stateInfo)
        {
            for (int i = 0; i < 100; i++)
            {
                Console.Write(".");

                queue.BeginInvoke(TestMultiThreadedMainFunc);

                if (i % 3 == 0)
                    Thread.Sleep(100);
            }

            queue.Exit();
        }
        static void TestMultiThreaded()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(TestMultiThreadedThreadFunc));
            queue.Run();
        }

        static void TestSingleThreaded()
        {
            queue.BeginInvoke(delegate() { Console.WriteLine("TS0"); } );
            queue.BeginInvoke(delegate() { Console.WriteLine("TS1"); });
            queue.BeginInvoke(delegate() { Console.WriteLine("TS2"); });
            queue.Exit();
            queue.Run();
        }

        static void TestResetEvent()
        {
            bool abort = false;
            bool threadEnded = false;
            AutoResetEvent sync = new AutoResetEvent(false);

            Thread t = new Thread(new ThreadStart(delegate()
            {
                while (!abort)
                {
                    Thread.Sleep(300);
                    Console.WriteLine("THREAD.WAIT");

                    sync.WaitOne();

                    Console.WriteLine("THREAD.WOKEUP");
                    Thread.Sleep(100);

                    Thread.Sleep(100);
                }

                threadEnded = true;
            }));

            sync.Set();
            sync.Set();

            t.Start();

            Thread.Sleep(2000);

            //Console.WriteLine("MAIN.ENTERING LOOP");
            //Thread.Sleep(10);
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.WriteLine("MAIN.SET");
            //    Thread.Sleep(10);
            //    sync.Set();

            //    //Console.WriteLine("MAIN.SLEEP");
            //    Thread.Sleep(10);
            //    Thread.Sleep(300);
            //}

            abort = true;
            sync.Set();

            while (!threadEnded)
            {
                Thread.Sleep(100);
            }
        }

        static void Main(string[] args)
        {
            TestResetEvent();
            //TestSingleThreaded();
            //TestMultiThreaded();
        }
    }
}
