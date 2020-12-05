using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;

namespace ID3.Processor
{
    public class TestProcessor : IProcessorMutable
    {
        public TestProcessor(Type[] types)
        {
            Types = types;
            Objects = new List<object>();
            Messages = new List<object>();
        }

        public Type[] SupportedClasses()
        {
            return Types;
        }

        public virtual void Process(object obj)
        {
            UnitTest.Test(!Object.ReferenceEquals(obj, null));
            UnitTest.Test(Types.Contains(obj.GetType()));
            Objects.Add(obj);
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            Messages.Add(message);
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }

        public List<object> Objects
        {
            get;
            private set;
        }
        public List<object> Messages
        {
            get;
            private set;
        }

        private Type[] Types
        {
            get;
            set;
        }
    }
}
