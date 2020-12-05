using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CoreControls
{
    public class TextBoxLogger
    {
        public TextBoxLogger(TextBox textbox, Label status)
        {
            Textbox = textbox;
            Status = status;

            TextboxQueue = new ThreadQueue(textbox.Dispatcher, ProcessTextBoxQueue);
            StatusQueue = new ThreadQueue(Status.Dispatcher, ProcessStatusQueue);
        }

        protected void AppendText(object text)
        {
            TextboxQueue.Enqueue(text.ToString());
        }
        protected void SetStatus(object text)
        {
            StatusQueue.Enqueue(text.ToString());
        }

        private void ProcessTextBoxQueue()
        {
            string text = TextboxQueue.DequeueConcat();

            if (!String.IsNullOrEmpty(text))
            {
                Textbox.AppendText(text);
                Textbox.ScrollToEnd();
            }
        }
        private void ProcessStatusQueue()
        {
            string text = StatusQueue.DequeueLast();

            if (!String.IsNullOrEmpty(text))
                Status.Content = text;
        }

        private TextBox Textbox { get; set; }
        private Label Status { get; set; }
        private ThreadQueue TextboxQueue { get; set; }
        private ThreadQueue StatusQueue { get; set; }

        private class ThreadQueue
        {
            public ThreadQueue(
                Dispatcher dispatcher,
                Action callback)
            {
                this.dispatcher = dispatcher;
                this.callback = callback;
            }

            public void Enqueue(string obj)
            {
                Debug.Assert(!Object.ReferenceEquals(obj, null));

                lock(queue)
                {
                    queue.Enqueue(obj);
                }

                dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, callback);
            }
            public string DequeueConcat()
            {
                StringBuilder result = new StringBuilder();

                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        result.Append(queue.Dequeue());
                    }
                }

                return result.ToString();
            }
            public string DequeueLast()
            {
                string result = null;

                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        result = queue.Dequeue();
                    }
                }

                return result;
            }

            private readonly Queue<string> queue = new Queue<string>();
            private Dispatcher dispatcher;
            private Action callback;
        }
    }
}
