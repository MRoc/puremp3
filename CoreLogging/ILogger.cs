using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreLogging
{
    public interface ILogger
    {
        void Write(object text);
        void WriteLine(object text);

        void WriteStatus(object text);
    }
}
