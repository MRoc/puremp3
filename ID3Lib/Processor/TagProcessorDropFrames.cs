using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using ID3.Utils;
using CoreLogging;

namespace ID3.Processor
{
    public class TagProcessorDropFrames : IProcessorMutable
    {
        public TagProcessorDropFrames(Version v)
        {
            Version = v;
            KeepFrameIds = DefaultFrameIds(v);
        }
        public TagProcessorDropFrames(Version v, IEnumerable<string> frameIdsToKeep)
        {
            Version = v;
            KeepFrameIds = frameIdsToKeep;
        }

        public Version Version { get; set; }
        public IEnumerable<string> KeepFrameIds { get; set; }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Tag) };
        }
        public void Process(object obj)
        {
            Tag tag = obj as Tag;

            List<Frame> framesToRemove =
                (from frame
                 in tag.Frames
                 where !KeepFrameIds.Contains(frame.FrameId)
                 select frame).ToList();

            foreach (Frame f in framesToRemove)
            {
                Logger.WriteLine(Tokens.InfoVerbose, "Dropping frame \"" + f.FrameId + "\"");
                tag.Remove(f);
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

        public static IEnumerable<string> DefaultFrameIds(Version v)
        {
            return from n in TagDescriptionMap.Instance[v].FrameDescs
                where n.Meaning != FrameMeaning.Unknown
                select n.FrameId;
        }
    }

    public class TestTagProcessorDropFrames
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagProcessorDropFrames));
        }

        private static void TestDropFrames()
        {
            Version v = Version.v2_3;
            TagDescription td = TagDescriptionMap.Instance[v];

            Tag tag = new Tag(td);
            tag.Add(new Frame(td, "TALB", "My Album"));
            tag.Add(new Frame(td, "EQUA"));

            UnitTest.Test(tag.Contains("TALB"));
            UnitTest.Test(tag.Contains("EQUA"));

            new TagProcessorDropFrames(v).Process(tag);

            UnitTest.Test(tag.Contains("TALB"));
            UnitTest.Test(!tag.Contains("EQUA"));            
        }
    }
}
