using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using ID3.Utils;
using CoreLogging;
using CoreUtils;
using System.Diagnostics;

namespace ID3.Processor
{
    public class TagVersionProcessor : ID3.Processor.IProcessorMutable
    {
        private Version dstVersion;
        private TagDescription dstMap;

        public TagVersionProcessor(Version targetVersion)
        {
            dstVersion = targetVersion;
            dstMap = TagDescriptionMap.Instance[dstVersion];
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Tag) };
        }
        public void Process(object obj)
        {
            Tag tag = obj as Tag;

            if (tag.DescriptionMap.Version != dstVersion)
            {
                Logger.WriteLine(Tokens.InfoVerbose, "Convert version from \""
                    + tag.DescriptionMap.Version + "\" to \"" + dstVersion.ToString() + "\"");

                Version srcVersion = tag.DescriptionMap.Version;

                tag.CheckVersion(srcVersion);

                RemoveCodecs(tag);

                ConvertFrames(tag, srcVersion);
                ConvertCodecs(tag, srcVersion);

                tag.CheckVersion(dstVersion);
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

        private void RemoveCodecs(Tag tag)
        {
            foreach (Frame frame in tag.Frames)
            {
                frame.Content.Codec = null;
                frame.Content.DescriptionMap = null;

                frame.Codec = null;
                frame.DescriptionMap = null;
            }

            tag.DescriptionMap = null;
        }
        private void ConvertFrames(Tag tag, Version src)
        {
            Version[] conversionPath = Version.BuildPath(src, dstVersion).ToArray();

            for (int i = 0; i < conversionPath.Length - 1; ++i)
            {
                VersionConversionFrameId.DropNonConvertableFrames(
                    tag, conversionPath[i], conversionPath[i + 1]);

                VersionConversionFrameId.ConvertFrames(
                    tag, conversionPath[i], conversionPath[i + 1]);
            }
        }
        private void ConvertCodecs(Tag tag, Version srcVersion)
        {
            tag.Codec = dstMap.CreateTagCodec();

            tag.DescriptionMap = dstMap;

            foreach (Frame f in tag.Frames)
            {
                f.DescriptionMap = dstMap;
                f.Content.DescriptionMap = dstMap;
            }
        }
    }

    class VersionConversionFrameId
    {
        public static void DropNonConvertableFrames(Tag tag, Version src, Version dst)
        {
            List<Frame> framesToRemove =
                (from frame
                in tag.Frames
                 where !CanConvert(frame.FrameId, src, dst)
                     || (MustConvertContent(frame.FrameId, src, dst)
                     && !CanConvertContent(frame.FrameId, src, dst))
                 select frame).ToList();

            foreach (var frame in framesToRemove)
            {
                Logger.WriteLine(Tokens.InfoVerbose, "Dropping " + frame.FrameId);
                tag.Remove(frame);
            }
        }
        public static void ConvertFrames(Tag tag, Version src, Version dst)
        {
            foreach (var frame in tag.Frames)
            {
                string srcFrameId = frame.FrameId;
                string dstFrameId = ConvertFrameId(frame.FrameId, src, dst);

                frame.FrameId = dstFrameId;
                
                if (MustConvertContent(srcFrameId, src, dst))
                {
                    Debug.Assert(CanConvertContent(srcFrameId, src, dst));
                    frame.Content = ConvertContent(frame.Content, dstFrameId, dst);
                }
            }
        }

        private static bool CanConvert(string frameId, Version src, Version dst)
        {
            int comparison = Version.Compare(src, dst);

            if (comparison > 0)
            {
                return CanConvertDown(frameId, src);
            }
            else if (comparison < 0)
            {
                return CanConvertUp(frameId, src);
            }
            else
            {
                return true;
            }
        }
        private static bool CanConvertUp(string frameId, Version src)
        {
            return TagDescriptionMap.Instance[
                src].Conversion.HasConversion(frameId);
        }
        private static bool CanConvertDown(string frameId, Version src)
        {
            return TagDescriptionMap.Instance[
                Version.PreviousVersion(src)].Conversion.HasConversionDown(frameId);
        }

        private static string ConvertFrameId(string frameId, Version src, Version dst)
        {
            int comparison = Version.Compare(src, dst);

            if (comparison > 0)
            {
                return ConvertFrameIdDown(frameId, src, dst);
            }
            else if (comparison < 0)
            {
                return ConvertFrameIdUp(frameId, src, dst);
            }
            else
            {
                return frameId;
            }
        }
        private static string ConvertFrameIdUp(string frameId, Version src, Version dst)
        {
            return TagDescriptionMap.Instance[src].Conversion.Conversion(frameId);
        }
        private static string ConvertFrameIdDown(string frameId, Version src, Version dst)
        {
            return TagDescriptionMap.Instance[
                Version.PreviousVersion(src)].Conversion.ConversionDown(frameId);
        }

        private static bool MustConvertContent(string srcFrameId, Version src, Version dst)
        {
            string dstFrameId = ConvertFrameId(srcFrameId, src, dst);

            TagDescription srcMap = TagDescriptionMap.Instance[src];
            TagDescription dstMap = TagDescriptionMap.Instance[dst];

            FrameDescription srcDesc = srcMap[srcFrameId];
            FrameDescription dstDesc = dstMap[dstFrameId];

            if (srcDesc.Type != dstDesc.Type)
            {
                return true;
            }

            if (srcDesc.Type != FrameDescription.FrameType.Binary
                && !srcMap.ContentClass(srcDesc.Type).Equals(dstMap.ContentClass(dstDesc.Type)))
            {
                return true;
            }

            return false;
        }
        private static bool CanConvertContent(string srcFrameId, Version src, Version dst)
        {
            string dstFrameId = ConvertFrameId(srcFrameId, src, dst);

            TagDescription srcMap = TagDescriptionMap.Instance[src];
            TagDescription dstMap = TagDescriptionMap.Instance[dst];

            Type srcType = srcMap.ContentClass(srcMap[srcFrameId].Type);
            Type dstType = dstMap.ContentClass(dstMap[dstFrameId].Type);

            return FrameContentConverter.CanConvert(srcType, dstType);
        }
        private static FrameContent ConvertContent(FrameContent content, string dstFrameId, Version dst)
        {
            TagDescription dstMap = TagDescriptionMap.Instance[dst];
            Type dstType = dstMap.ContentClass(dstMap[dstFrameId].Type);

            return FrameContentConverter.Convert(content, dstType);
        }
    }

    public class TestTagVersionProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagVersionProcessor));
        }

        static void TestConversionUp()
        {
            Tag[] tags = (from raw in TestTags.Demotags select TagUtils.RawToTag(raw)).ToArray();

            TagVersionProcessor processor = new TagVersionProcessor(Version.v2_4);
            tags.ForEach(n => processor.Process(n));

            tags.ForEach(n => UnitTest.Test(n.DescriptionMap.Version == Version.v2_4));

            TagEditor[] actual = (from tag in tags select new TagEditor(tag)).ToArray();
            TagEditor[] expected = (from raw in TestTags.Demotags select new TagEditor(TagUtils.RawToTag(raw))).ToArray();

            UnitTest.Test(actual.SequenceEqual(expected));
        }
        static void TestConversionDown()
        {
            Tag[] tags = (from raw in TestTags.Demotags select TagUtils.RawToTag(raw)).ToArray();

            TagVersionProcessor processor = new TagVersionProcessor(Version.v1_0);
            tags.ForEach(n => processor.Process(n));

            tags.ForEach(n => UnitTest.Test(n.DescriptionMap.Version == Version.v1_0));

            TagEditor[] actual = (from tag in tags select new TagEditor(tag)).ToArray();
            TagEditor[] expected = (from raw in TestTags.Demotags select new TagEditor(TagUtils.RawToTag(raw))).ToArray();

            UnitTest.Test(actual.SequenceEqual(expected));
        }
    }
}
