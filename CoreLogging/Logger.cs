using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreLogging
{
    public class Tokens
    {
        public static readonly ILoggerToken Debug = new LoggerToken("Debug");
        public static readonly ILoggerToken InfoVerbose = new LoggerToken("InfoVerbose");
        public static readonly ILoggerToken Info = new LoggerToken("Info");
        public static readonly ILoggerToken Warning = new LoggerToken("Warning");
        public static readonly ILoggerToken Exception = new LoggerToken("Exception");
        public static readonly ILoggerToken Status = new LoggerToken("Status");
    }
    public class Logger
    {
        static Logger()
        {
            Instance = new LoggerConsole();

            EnableToken(Tokens.Debug, true);
            EnableToken(Tokens.Info, true);
            EnableToken(Tokens.Warning, true);
            EnableToken(Tokens.Exception, true);
            EnableToken(Tokens.Status, true);
        }
        public static ILogger Instance
        {
            get;
            set;
        }

        public static void Write(ILoggerToken token, object text)
        {
            if (IsTokenEnabled(token))
            {
                if (token == Tokens.Status)
                {
                    Instance.WriteStatus(text);
                }
                else
                {
                    Instance.Write(text);
                }
            }
        }
        public static void WriteLine(ILoggerToken token, object obj)
        {
            if (IsTokenEnabled(token))
            {
                if (token == Tokens.Status)
                {
                    Instance.WriteStatus(obj);
                }
                else if (token == Tokens.Exception && obj is Exception)
                {
                    Instance.WriteLine((obj as Exception).Message);
                    Instance.WriteLine((obj as Exception).StackTrace);

                    Console.WriteLine((obj as Exception).Message);
                    Console.WriteLine((obj as Exception).StackTrace);
                }
                else
                {
                    Instance.WriteLine(obj);
                }
            }
        }

        public static void EnableToken(ILoggerToken token, bool value)
        {
            (token as LoggerToken).IsEnabled = value;
        }
        public static bool IsTokenEnabled(ILoggerToken token)
        {
            return token.IsEnabled;
        }
    }
}
