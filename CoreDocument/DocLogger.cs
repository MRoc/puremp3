using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CoreDocument
{
    public class DocLogger
    {
        public static bool doLog = false;
        public static bool verbose = false;

        [Conditional("DEBUG")]
        public static void Write(string text)
        {
            if (doLog)
            {
                Console.Write(text);
            }
        }
        [Conditional("DEBUG")]
        public static void WriteLine(string text)
        {
            if (doLog)
            {
                Console.WriteLine(text);
            }
        }
        [Conditional("DEBUG")]
        public static void WriteLineVerbose(string text)
        {
            if (doLog && verbose)
            {
                Console.WriteLine(text);
            }
        }
    }
}
