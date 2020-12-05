using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreLogging
{
    public interface ILoggerToken
    {
        string Token
        {
            get;
        }
        bool IsEnabled
        {
            get;
        }
    }
}
