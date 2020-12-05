using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CoreTest;

namespace ID3.Processor
{
    public class TagProcessor : IProcessorMutable
    {
        public TagProcessor(IProcessorMutable processor)
        {
            if (!processor.SupportedClasses().Contains(typeof(Frame)))
            {
                throw new Exception(GetType().Name
                    + " can't handle processor of type " + processor.GetType().Name);
            }

            Processor = processor;
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Tag) };
        }
        public virtual void Process(object obj)
        {
            foreach (var f in (obj as Tag).Frames)
            {
                Processor.Process(f);
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

        public IProcessorMutable Processor
        {
            get;
            private set;
        }
    }

    public class TestTagProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagProcessor));
        }

        private static void TestTagProcessorLoop()
        {
            TestProcessor testProcessor = new TestProcessor(new Type[] { typeof(Frame) });
            TagProcessor tagProcessor = new TagProcessor(testProcessor);
            Tag tag = TagUtils.RawToTag(TestTags.demoTag2_3);

            tagProcessor.ProcessMessage(new ProcessorMessageInit());
            tagProcessor.Process(tag);

            UnitTest.Test(testProcessor.Objects.Where(
                n => n.GetType() == typeof(Frame)).Count() == tag.Frames.Count());

            UnitTest.Test(testProcessor.Messages.Where(
                n => n.GetType() == typeof(ProcessorMessageInit)).Count() == 1);
        }
    }
}
