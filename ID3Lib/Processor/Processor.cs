using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Utils;
using CoreUtils;
using System.Xml;
using System.IO;

namespace ID3.Processor
{
    public interface IProcessorMessage
    {
    }
    public interface IProcessor
    {
        Type[] SupportedClasses();
        void ProcessMessage(IProcessorMessage message);
        IEnumerable<IProcessor> Processors
        {
            get;
        }
    }
    public interface IProcessorImmutable : IProcessor
    {
        object Process(object obj);
    }
    public interface IProcessorMutable : IProcessor
    {
        void Process(object obj);
    }

    public class ProcessorMessageInit : IProcessorMessage
    {
    }
    public class ProcessorMessageExit : IProcessorMessage
    {
    }

    public class ProcessorMessageAbort : IProcessorMessage
    {
        public ProcessorMessageAbort(bool abort)
        {
            Abort = abort;
        }
        public bool Abort
        {
            get;
            private set;
        }
    }
    public class ProcessorMessageQueryAbort : IProcessorMessage
    {
        public ProcessorMessageQueryAbort()
        {
        }
        public bool Abort
        {
            get;
            set;
        }
    }

    public class ProcessorListMutable : IProcessorMutable
    {
        private List<IProcessorMutable> processors = new List<IProcessorMutable>();
        public IList<IProcessorMutable> ProcessorList
        {
            get
            {
                return processors;
            }
        }

        public Type[] SupportedClasses()
        {
            if (ProcessorList.Count > 0)
            {
                List<Type> types = new List<Type>();

                Processors.ForEach(n => types.AddRange(n.SupportedClasses()));

                return types.ToArray();
            }
            else
            {
                return new Type[] { typeof(object) };
            }
        }
        public void Process(object obj)
        {
            ProcessorList.ForEach((n) => n.Process(obj));
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            Processors.ForEach(n => n.ProcessMessage(message));
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorList;
            }
        }
    }

    public static class ProcessorUtils
    {
        private static List<IProcessor> emptyList = new List<IProcessor>();
        public static IEnumerable<IProcessor> Empty
        {
            get
            {
                return emptyList;
            }
        }

        public static string CreateDump(IProcessor root)
        {
            XmlDocument doc = new XmlDocument();
            Append(doc, doc, root);

            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            return sw.ToString();
        }

        private static void Append(XmlDocument doc, XmlNode parent, IProcessor processor)
        {
            XmlNode element = doc.CreateElement(processor.GetType().Name);
            parent.AppendChild(element);

            foreach (var item in processor.Processors)
            {
                Append(doc, element, item);
            }
        }
    }
}
