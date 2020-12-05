using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using CoreControls;
using CoreLogging;

namespace PureMp3
{
    public class WpfConsoleLogger : TextBoxLogger, ILogger
    {
        public WpfConsoleLogger(TextBox textbox, Label status)
            : base(textbox, status)
        {
            Logger.Instance = this;
        }

        public bool Verbose
        {
            get
            {
                return Logger.IsTokenEnabled(Tokens.InfoVerbose);
            }
            set
            {
                Logger.EnableToken(Tokens.InfoVerbose, value);
            }
        }
        public bool Warnings
        {
            get
            {
                return Logger.IsTokenEnabled(Tokens.Warning);
            }
            set
            {
                Logger.EnableToken(Tokens.Warning, value);
            }
        }

        public void Write(object text)
        {
            AppendText(text);
        }
        public void WriteLine(object text)
        {
            AppendText(text + "\n");
        }
        public void WriteStatus(object text)
        {
            SetStatus(text);
        }
    }
}
