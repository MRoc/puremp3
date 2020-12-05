using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLogging;
using CoreTest;
using CoreUtils;
using ID3Lib;
using ID3.Utils;

namespace ID3.Processor
{
    public class TagProcessorTrackNumber : IProcessorMutable
    {
        public static readonly string DefaultPattern = "0";

        public TagProcessorTrackNumber()
        {
            TrackNumberGenerator = new TrackNumberGenerator(DefaultPattern);
        }
        public TagProcessorTrackNumber(TrackNumberGenerator trackNumberGenerator)
        {
            TrackNumberGenerator = trackNumberGenerator;
        }

        public int TrackNumber { get; set; }
        public int TrackCount { get; set; }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Tag) };
        }
        public virtual void Process(object obj)
        {
            string trackNumberText = TrackNumberGenerator.ApplyPattern(TrackNumber, TrackCount);

            Logger.WriteLine(Tokens.InfoVerbose, "Creating track number " + trackNumberText);

            new TagEditor(obj as Tag).TrackNumber = trackNumberText;
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            if (message is DirectoryProcessor.Message)
            {
                DirectoryProcessor.Message dmsg = message as DirectoryProcessor.Message;
                TrackNumber = dmsg.Index + 1;
                TrackCount = dmsg.Count;
            }
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }


        private TrackNumberGenerator TrackNumberGenerator
        {
            get;
            set;
        }
    }

    public class TestTagProcessorTrackNumber
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagProcessorTrackNumber));
        }

        static void TestCreateTrackNumbers()
        {
            Tag[] tags =
            {
                TestTags.CreateDemoTag(Version.v2_3),
                TestTags.CreateDemoTag(Version.v2_3),
                TestTags.CreateDemoTag(Version.v2_3)
            };

            string[] patterns =
            {
                "0",
                "00",
                "0/0",
                "00/00",
                "0 0",
            };
            string[,] expected =
            {
                { "1", "2", "3" },
                { "01", "02", "03" },
                { "1/3", "2/3", "3/3" },
                { "01/03", "02/03", "03/03" },
                { "1 3", "2 3", "3 3" },
            };

            for (int i = 0; i < patterns.Length; i++)
            {
                TagProcessorTrackNumber processor = new TagProcessorTrackNumber(
                    new TrackNumberGenerator(patterns[i]));

                for (int j = 0; j < tags.Length; j++)
                {
                    processor.ProcessMessage(new DirectoryProcessor.Message(tags.Length, j));
                    processor.Process(tags[j]);
                }

                for (int j = 0; j < tags.Length; j++)
                {
                    UnitTest.Test(new TagEditor(tags[j]).TrackNumber == expected[i, j]);
                }
            }
        }
    }
}
