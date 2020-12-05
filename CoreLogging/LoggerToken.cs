using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreLogging
{
    public class LoggerToken : ILoggerToken
    {
        public LoggerToken(string token)
        {
            Token = token;
        }

        public string Token
        {
            get;
            private set;
        }
        public bool IsEnabled
        {
            get;
            set;
        }
    }
}
