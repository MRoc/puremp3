using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreLogging
{
    public class LoggerConsole : ILogger
    {
        public void Write(object text)
        {
            Console.Write(text);
        }
        public void WriteLine(object text)
        {
            Console.WriteLine(text);
        }
        public void WriteStatus(object text)
        {
            Console.WriteLine(text);
        }
    }
}
