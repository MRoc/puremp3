using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using ID3.Utils;
using CoreLogging;

namespace ID3.Processor
{
    public class DropCodecsProcessor : IProcessorMutable
    {
        public DropCodecsProcessor()
        {
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Tag) };
        }
        public void Process(object obj)
        {
            Logger.WriteLine(Tokens.InfoVerbose, "Dropping codecs");

            Tag tag = obj as Tag;

            tag.Codec = null;

            foreach (var frame in tag.Frames)
            {
                frame.Codec = null;
            }
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }
    }
}
