using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLogging;
using ID3;
using ID3.Processor;
using System.IO;

namespace ID3Lib.Processor
{
    public class FrameByMeaningMissingProcessor : IProcessorMutable
    {
        public FrameByMeaningMissingProcessor(
            FrameMeaning meaning,
            IProcessorMutable processor)
        {
            Meaning = meaning;
            Processor = processor;
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            if (TagUtils.HasTag(file))
            {
                Tag tag = TagUtils.ReadTag(file);

                FrameDescription desc = tag.DescriptionMap[Meaning];

                if (Object.ReferenceEquals(desc, null)
                    || tag.Frames.Where(n => n.FrameId == desc.FrameId).Count() == 0)
                {
                    Processor.Process(obj);
                }
            }
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            Processor.ProcessMessage(message);
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                yield return Processor;
            }
        }

        private FrameMeaning Meaning
        {
            get;
            set;
        }
        private IProcessorMutable Processor
        {
            get;
            set;
        }
    }
}
