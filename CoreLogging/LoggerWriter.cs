using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLogging;

namespace CoreLogging
{
    public class LoggerWriter
    {
        public static void WriteLine(ILoggerToken token, object obj)
        {
            WriteLineImpl(token, obj);
        }
        public static void WriteDelimiter(ILoggerToken token)
        {
            WriteLineImpl(token, "-------------------------------------------------------------------------------------------------------------------------------------");
        }

        public static void WriteStepHeader(ILoggerToken token, string header)
        {
            WriteImpl(token, header);

            for (int i = header.Length; i < 19; i++)
            {
                WriteImpl(token, ".");
            }

            WriteImpl(token, ": ");
        }
        public static void WriteStepFinish(ILoggerToken token, object msg)
        {
            WriteLineImpl(token, msg);
        }
        public static void WriteStep(ILoggerToken token, string header, object msg)
        {
            WriteStepHeader(token, header);
            WriteStepFinish(token, msg);
        }
        public static void WriteStepIndent(ILoggerToken token, object msg)
        {
            for (int i = 0; i < 21; i++)
            {
                WriteImpl(token, " ");
            }
            WriteStepFinish(token, msg);
        }

        private static void WriteImpl(ILoggerToken token, object obj)
        {
            Logger.Write(token, obj);
        }
        private static void WriteLineImpl(ILoggerToken token, object obj)
        {
            Logger.WriteLine(token, obj);
        }
    }
}
