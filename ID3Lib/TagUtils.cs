using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using ID3;
using ID3.Codec;
using ID3.Processor;
using ID3.Utils;
using ID3.IO;
using CoreUtils;
using CoreTest;
using CoreVirtualDrive;
using CoreLogging;

namespace ID3
{
    public class Rewriter
    {
        public static readonly int BlockSize = 1024;

        public enum Strategy
        {
            Exact,
            NeverShrink,
            Quantize
        }

        public Rewriter(Strategy strategy)
        {
            CurrentStrategy = strategy;
        }
        public int Rewrite(int bytesRequired, FileInfo file)
        {
            int padding = CalculatePadding(bytesRequired, file);

            if (padding < bytesRequired)
            {
                throw new Exception("CalculatePadding failed! Padding too small!");
            }

            if (TagUtils.TagSizeV2(file) != padding)
            {
                TagUtils.StripTagV2(file, padding);
            }

            return padding;
        }

        private Strategy CurrentStrategy
        {
            get;
            set;
        }
        private int CalculatePadding(int bytesRequired, FileInfo file)
        {
            int padding = 0;

            switch (CurrentStrategy)
            {
                case Strategy.Exact:
                    padding = bytesRequired;
                    break;
                case Strategy.NeverShrink:
                    padding = Math.Max(bytesRequired, TagUtils.TagSizeV2(file));
                    break;
                case Strategy.Quantize:
                    padding = CeilBlockSize(bytesRequired);
                    break;
                default:
                    throw new Exception("Unknown write strategy");
            }

            return padding;
        }
        private int CeilBlockSize(int value)
        {
            return (int)(Math.Ceiling((double)value
                / (double)BlockSize) * (double)BlockSize);
        }
    }
    public class TagUtils
    {
        public static bool HasTagV1(FileInfo file)
        {
            if (VirtualDrive.FileLength(file.FullName) < HeaderV1.totalSize)
            {
                return false;
            }

            byte[] arr = new byte[3];

            try
            {
                using (Stream stream = OpenInStream(file))
                {
                    stream.Seek(
                        stream.Length - HeaderV1.totalSize,
                        System.IO.SeekOrigin.Begin);

                    stream.Read(arr, 0, arr.Length);
                }
            }
            catch (System.Exception)
            {
            }

            return HasTagV1(arr);
        }
        public static bool HasTagV2(FileInfo file)
        {
            if (VirtualDrive.FileLength(file.FullName) < HeaderV2.headerSize)
            {
                return false;
            }

            byte[] arr = new byte[3];

            try
            {
                using (Stream stream = OpenInStream(file))
                {
                    stream.Read(arr, 0, arr.Length);
                }
            }
            catch (System.Exception)
            {
            }

            return HasTagV2(arr);
        }
        public static bool HasTag(FileInfo file)
        {
            return HasTagV2(file) || HasTagV1(file);
        }

        public static bool HasTagV1(byte[] raw)
        {
            return raw != null && raw.Length >= 3
                && raw[0] == 'T' && raw[1] == 'A' && raw[2] == 'G';
        }
        public static bool HasTagV2(byte[] raw)
        {
            return raw != null && raw.Length >= 3
                && raw[0] == 'I' && raw[1] == 'D' && raw[2] == '3';
        }
        public static bool HasTag(byte[] raw)
        {
            return HasTagV2(raw) || HasTagV1(raw);
        }

        public static int TagSizeV1(FileInfo file)
        {
            if (VirtualDrive.ExistsFile(file.FullName) && HasTagV1(file))
            {
                return HeaderV1.totalSize;
            }
            else
            {
                return 0;
            }
        }
        public static int TagSizeV2(FileInfo file)
        {
            if (VirtualDrive.ExistsFile(file.FullName) && HasTagV2(file))
            {
                HeaderV2 header = new HeaderV2();
                using (Reader reader = new Reader(file))
                {
                    header.Read(reader);
                }
                return header.TotalSize;
            }

            return 0;
        }
        public static int TagSize(FileInfo file)
        {
            if (HasTagV2(file))
            {
                return TagSizeV2(file);
            }
            else if (HasTagV1(file))
            {
                return TagSizeV1(file);
            }
            else
            {
                return 0;
            }
        }
        public static int TagSize(Tag tag)
        {
            using (Writer memWriter = new Writer())
            {
                tag.Codec.Header.Size = TagHeader.sizeUnitinialized;

                tag.Write(memWriter);

                return memWriter.OutData.Length;
            }
        }
        public static int TagSize(byte[] raw)
        {
            if (HasTagV2(raw))
            {
                HeaderV2 header = new HeaderV2();
                using (Reader reader = new Reader(raw))
                {
                    header.Read(reader);
                }

                return header.TotalSize;
            }
            else if (HasTagV2(raw))
            {
                return HeaderV1.totalSize;
            }

            return 0;
        }

        public static Tag ReadTagV1(FileInfo file)
        {
            Debug.Assert(HasTagV1(file));

            Tag tag = new Tag(Version.v1_0);

            using (Reader reader = new Reader(file))
            {
                tag.Read(reader);
            }

            return tag;
        }
        public static Tag ReadTagV2(FileInfo file)
        {
            Debug.Assert(HasTagV2(file));

            Tag tag = new Tag(new TagCodecV2());

            using (Reader reader = new Reader(file))
            {
                tag.Read(reader);
            }

            return tag;
        }
        public static Tag ReadTagV2ThrowExceptions(FileInfo file)
        {
            Tag tag = new Tag(new TagCodecV2());

            using (Reader reader = new Reader(file))
            {
                reader.ThrowExceptions = true;
                tag.Read(reader);
            }

            return tag;
        }
        public static Tag ReadTag(FileInfo file)
        {
            if (HasTagV2(file))
            {
                return ReadTagV2(file);
            }
            else if (HasTagV1(file))
            {
                return ReadTagV1(file);
            }
            else
            {
                return null;
            }
        }

        public static byte[] ReadTagV1Raw(FileInfo file)
        {
            Debug.Assert(HasTagV1(file));

            CheckMpegOffset(file);
            
            using (BinaryReader reader = OpenReader(file))
            {
                reader.BaseStream.Seek(
                    VirtualDrive.FileLength(file.FullName) - HeaderV1.totalSize,
                    SeekOrigin.Begin);

                return reader.ReadBytes(HeaderV1.totalSize);
            }
        }
        public static byte[] ReadTagV2Raw(FileInfo file)
        {
            Debug.Assert(HasTagV2(file));

            CheckMpegOffset(file);

            int tagSize = TagSizeV2(file);

            using (BinaryReader reader = OpenReader(file))
            {
                return reader.ReadBytes(tagSize);
            }
        }
        public static byte[] ReadTagRaw(FileInfo file)
        {
            if (HasTagV2(file))
            {
                return ReadTagV2Raw(file);
            }
            else if (HasTagV1(file))
            {
                return ReadTagV1Raw(file);
            }
            else
            {
                return null;
            }
        }
        public static byte[][] ReadTagsRaw(FileInfo file)
        {
            List<byte[]> result = new List<byte[]>();

            if (HasTagV2(file))
            {
                result.Add(ReadTagV2Raw(file));
            }
            if (HasTagV1(file))
            {
                result.Add(ReadTagV1Raw(file));
            }

            return result.ToArray();
        }

        public static Version ReadVersion(FileInfo file)
        {
            if (HasTag(file))
            {
                return ReadTag(file).DescriptionMap.Version;
            }
            else
            {
                return null;
            }
        }
        public static byte[] RecreateTagWithNewTagSize(byte[] raw, int newSize)
        {
            Debug.Assert(HasTagV2(raw));
            Debug.Assert(TagSize(raw) <= newSize);

            HeaderV2 header = new HeaderV2();
            using (Reader reader = new Reader(raw))
            {
                header.Read(reader);
            }

            int oldSize = header.TotalSize;
            header.Size = newSize - header.HeaderSize;

            byte[] result = null;

            using (Writer writer = new Writer(raw))
            {
                header.Write(writer);
                writer.Seek(0, SeekOrigin.End);

                for (int i = 0; i < (newSize - oldSize); ++i)
                {
                    writer.WriteByte(0);
                }

                result = writer.OutData;
            }

            CheckTagSizeV2(result);

            return result;
        }

        public static void WriteTagV1(Tag tag, FileInfo file)
        {
            using (VirtualDriveLock fileLock = new VirtualDriveLock(file.FullName, AccessObserver.AccessType.Write))
            {
                bool existingTag = HasTagV1(file);
                long fileLength = VirtualDrive.FileLength(file.FullName);

                using (Writer writer = new Writer(file))
                {
                    if (existingTag)
                    {
                        writer.Seek(fileLength - HeaderV1.totalSize, SeekOrigin.Begin);
                    }
                    else
                    {
                        writer.Seek(fileLength, SeekOrigin.Begin);
                    }
                    tag.Write(writer);
                }
            }
        }
        public static void WriteTagV2(Tag tag, FileInfo file, Rewriter.Strategy strategy = Rewriter.Strategy.Exact)
        {
            Debug.Assert(tag.DescriptionMap.Version != Version.v1_0);

            using (VirtualDriveLock fileLock = new VirtualDriveLock(file.FullName, AccessObserver.AccessType.Write))
            {
                int tagSize = TagSize(tag);
                tag.Codec.Header.Size = tagSize - tag.Codec.Header.HeaderSize;

                if (VirtualDrive.ExistsFile(file.FullName))
                {
                    Rewriter rewriter = new Rewriter(strategy);
                    int padding = rewriter.Rewrite(tagSize, file);

                    if (padding != tagSize)
                    {
                        tag.Codec.Header.Size = padding - tag.Codec.Header.HeaderSize;
                    }
                }
                
                using (Writer writer = new Writer(file))
                {
                    tag.Write(writer);
                }
            }
        }
        public static void WriteTag(Tag tag, FileInfo file, Rewriter.Strategy strategy = Rewriter.Strategy.Exact)
        {
            using (VirtualDriveLock fileLock = new VirtualDriveLock(file.FullName, AccessObserver.AccessType.Write))
            {
                if (tag.DescriptionMap.Version == Version.v1_0)
                {
                    WriteTagV1(tag, file);
                }
                else
                {
                    WriteTagV2(tag, file, strategy);
                }
            }
        }

        public static void WriteTagV1(byte[] raw, FileInfo file)
        {
            using (VirtualDriveLock fileLock = new VirtualDriveLock(file.FullName, AccessObserver.AccessType.Write))
            {
                bool existingTag = HasTagV1(file);
                long fileLength = VirtualDrive.FileLength(file.FullName);

                using (BinaryWriter writer = OpenWriter(file))
                {
                    if (existingTag)
                    {
                        writer.BaseStream.Seek(fileLength - HeaderV1.totalSize, SeekOrigin.Begin);
                    }
                    else
                    {
                        writer.BaseStream.Seek(fileLength, SeekOrigin.Begin);
                    }

                    writer.Write(raw);
                }
            }

            CheckMpegOffset(file);
        }
        public static void WriteTagV2(byte[] raw, FileInfo file, Rewriter.Strategy strategy = Rewriter.Strategy.Exact)
        {
            CheckTagSizeV2(raw);
            CheckMpegOffset(file);

            using (VirtualDriveLock fileLock = new VirtualDriveLock(file.FullName, AccessObserver.AccessType.Write))
            {
                if (VirtualDrive.ExistsFile(file.FullName))
                {
                    Rewriter rewriter = new Rewriter(strategy);
                    int padding = rewriter.Rewrite(raw.Length, file);

                    if (padding != TagSize(raw))
                    {
                        raw = RecreateTagWithNewTagSize(raw, padding);
                    }
                }

                using (BinaryWriter writer = OpenWriter(file))
                {
                    writer.Write(raw);
                }
            }

            CheckMpegOffset(file);
        }
        public static void WriteTag(byte[] raw, FileInfo file, Rewriter.Strategy strategy = Rewriter.Strategy.Exact)
        {
            using (VirtualDriveLock fileLock = new VirtualDriveLock(file.FullName, AccessObserver.AccessType.Write))
            {
                if (HasTagV1(raw))
                {
                    WriteTagV1(raw, file);
                }
                else if (HasTagV2(raw))
                {
                    WriteTagV2(raw, file, strategy);
                }
                else
                {
                    StripTags(file, 0, 0);
                }
            }

            CheckMpegOffset(file);
        }

        public static Tag RawToTagV1(byte[] raw)
        {
            Tag tag = new Tag(new TagCodecV1());

            using (Reader reader = new Reader(raw))
            {
                tag.Read(reader);
            }

            return tag;
        }
        public static Tag RawToTagV2(byte[] raw)
        {
            Tag tag = new Tag(new TagCodecV2());

            using (Reader reader = new Reader(raw))
            {
                reader.ThrowExceptions = false;
                tag.Read(reader);
            }

            return tag;
        }
        public static Tag RawToTag(byte[] raw)
        {
            if (HasTagV1(raw))
            {
                return RawToTagV1(raw);
            }
            else if (HasTagV2(raw))
            {
                return RawToTagV2(raw);
            }
            else
            {
                throw new Exception("No tag provided");
            }
        }

        public static byte[] TagToRaw(Tag tag)
        {
            tag.Codec.Header.Size = TagSize(tag) - tag.Codec.Header.HeaderSize;

            using (Writer writer = new Writer())
            {
                tag.Write(writer);
                return writer.OutData;
            }
        }

        public static void StripTagV2(FileInfo file, int padding)
        {
            long cutBeg = 0;

            if (HasTagV2(file))
            {
                cutBeg = TagSizeV2(file) + OffsetTagToMpegHeader(file);
            }

            Rewrite(file, cutBeg, 0, padding, 0);
        }
        public static void StripTagV1(FileInfo file)
        {
            Debug.Assert(HasTagV1(file));
            Rewrite(file, 0, HeaderV1.totalSize, 0, 0);
        }
        public static void StripTags(FileInfo file, int padBegin, int padEnd)
        {
            long cutBeg = 0;
            long cutEnd = 0;

            if (HasTagV1(file))
            {
                cutEnd = ReadTagV1(file).Codec.Header.TotalSize;
            }

            if (HasTagV2(file))
            {
                cutBeg = ReadTagV2(file).Codec.Header.TotalSize
                    + OffsetTagToMpegHeader(file);
            }

            Rewrite(file, cutBeg, cutEnd, padBegin, padEnd);
        }

        public static long OffsetTagToMpegHeader(FileInfo file)
        {
            long tagSize = TagUtils.TagSizeV2(file);

            using (Stream s = VirtualDrive.OpenInStream(file.FullName))
            {
                s.Seek(tagSize, SeekOrigin.Begin);

                long offset = TagUtils.OffsetToFirstMpegHeader(s);

                if (offset > 0)
                {
                    Logger.WriteLine(Tokens.Warning, "Tag V2 has wrong size. Offset to MPEG header: " + offset
                        + " in file " + file.FullName);
                }

                return offset;
            }
        }
        public static long OffsetToFirstMpegHeader(Stream s)
        {
            long offset = 0;

            int v0 = s.ReadByte();
            int v1 = s.ReadByte();

            if (v0 != -1 && v1 != -1)
            {
                while (v0 != 0xFF || (v1 & 0xE0) != 0xE0)
                {
                    v0 = v1;
                    v1 = s.ReadByte();

                    if (v1 == -1)
                    {
                        break;
                    }
                    offset++;
                }
            }

            if (v1 == -1)
            {
                offset = 0;
            }

            return offset;
        }
        public static long MpegDataSize(FileInfo file)
        {
            long fileLength = VirtualDrive.FileLength(file.FullName);

            if (HasTagV2(file))
            {
                fileLength -= (TagSizeV2(file) + OffsetTagToMpegHeader(file));
            }
            if (HasTagV1(file))
            {
                fileLength -= TagSizeV1(file);
            }

            return fileLength;
        }
        public static long OffsetToMpegHeader(FileInfo file)
        {
            long tagSize = TagUtils.TagSizeV2(file);

            using (Stream s = VirtualDrive.OpenInStream(file.FullName))
            {
                s.Seek(tagSize, SeekOrigin.Begin);
                return tagSize + TagUtils.OffsetToFirstMpegHeader(s);
            }
        }

        public static BinaryReader OpenReader(FileInfo file)
        {
            return new BinaryReader(
                VirtualDrive.OpenInStream(file.FullName),
                System.Text.Encoding.BigEndianUnicode);
        }
        public static BinaryWriter OpenWriter(FileInfo file)
        {
            return new BinaryWriter(
                VirtualDrive.OpenOutStream(file.FullName),
                System.Text.Encoding.BigEndianUnicode);
        }
        public static Stream OpenInStream(FileInfo file)
        {
            return VirtualDrive.OpenInStream(file.FullName);
        }

        public static void Rewrite(FileInfo file, long cutBeg, long cutEnd, long padBeg, long padEnd)
        {
            long beg = cutBeg;
            long end = VirtualDrive.FileLength(file.FullName) - cutEnd;

            BinaryReader reader = OpenReader(file);
            BinaryWriter writer = OpenWriter(new FileInfo(file + ".tmp"));

            WriteZeroBytes(writer, padBeg);
            CopyBytes(reader, writer, beg, end - beg);
            WriteZeroBytes(writer, padEnd);

            reader.Close();
            writer.Close();

            VirtualDrive.ReplaceFile(file.FullName + ".tmp", file.FullName);
        }
        private static void WriteZeroBytes(BinaryWriter w, long numBytes)
        {
            for (long i = 0; i < numBytes; ++i)
            {
                w.Write((byte)0);
            }
        }
        private static void CopyBytes(BinaryReader src, BinaryWriter dst, long begin, long numBytes)
        {
            if (numBytes < 0)
            {
                throw new Exception("Can't copy less than zero bytes");
            }

            long blockSize = 1024;

            src.BaseStream.Seek(begin, System.IO.SeekOrigin.Begin);

            while (numBytes > 0)
            {
                long numBytesNow = Math.Min(blockSize, numBytes);
                dst.Write(src.ReadBytes((int)numBytesNow));
                numBytes -= numBytesNow;
            }
        }

        private static void CheckTagSizeV2(byte[] raw)
        {
            int tagSize = TagSize(raw);
            if (tagSize != raw.Length)
            {
                throw new Exception("Size in header " + tagSize
                    + " not equal size of content " + raw.Length);
            }
        }
        private static void CheckMpegOffset(FileInfo file)
        {
            if (VirtualDrive.ExistsFile(file.FullName))
            {
                long offset = OffsetTagToMpegHeader(file);
                if (offset > 0)
                {
                    Logger.WriteLine(Tokens.Warning, file.FullName + " has MPEG offset " + offset);
                }
            }
        }
    }

    public class TestTagUtils
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagUtils));
        }

        private static void TestTagUtilsReadWrite()
        {
            int counter = 0;
            foreach (var demoTag in TestTags.Demotags)
            {
                FileInfo fileInfo = VirtualDrive.CreateVirtualFileInfo(
                    "TestTagUtilsReadWrite\\" + counter + ".tag");

                TagUtils.WriteTag(demoTag, fileInfo);

                Tag tag0 = TagUtils.ReadTag(fileInfo);

                UnitTest.Test(TestTags.IsDemoTag(new TagEditor(tag0)));

                TagUtils.WriteTag(tag0, fileInfo);

                UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagRaw(fileInfo), demoTag));
                
                counter++;
            }
        }
        private static void TestTagUtilsReadWriteRaw()
        {
            int counter = 0;
            foreach (var demoTag in TestTags.Demotags)
            {
                FileInfo fileInfo = VirtualDrive.CreateVirtualFileInfo(
                    "TestTagUtilsReadWriteRaw\\" + counter + ".tag");

                TagUtils.WriteTag(demoTag, fileInfo);

                byte[] tagRaw = TagUtils.ReadTagRaw(fileInfo);

                UnitTest.Test(ArrayUtils.IsEqual(tagRaw, demoTag));

                counter++;
            }
        }

        private static void TestTagUtilsWriteRawBiggerExact()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteRawBiggerExact\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_3);

            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(TestTags.demoTag2_4, fileInfo, Rewriter.Strategy.Exact);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == TestTags.demoTag2_4.Length + TestTags.mpegDummy.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteRawBiggerNeverShrink()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteRawBiggerNeverShrink\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_3);

            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(TestTags.demoTag2_4, fileInfo, Rewriter.Strategy.NeverShrink);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == TestTags.demoTag2_4.Length + TestTags.mpegDummy.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteRawBiggerQuantize()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteRawBiggerQuantize\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_3);

            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(TestTags.demoTag2_4, fileInfo, Rewriter.Strategy.Quantize);
            UnitTest.Test(TagUtils.TagSize(fileInfo) % Rewriter.BlockSize == 0);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteRawSmallerExact()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteRawSmallerExact\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_4);

            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(TestTags.demoTag2_3, fileInfo, Rewriter.Strategy.Exact);
            UnitTest.Test(TagUtils.TagSizeV2(fileInfo) == TestTags.demoTag2_3.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteRawSmallerNeverShrink()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteRawSmallerNeverShrink\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_4);

            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(TestTags.demoTag2_3, fileInfo, Rewriter.Strategy.NeverShrink);
            UnitTest.Test(TagUtils.TagSizeV2(fileInfo) == TestTags.demoTag2_4.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteRawSmallerQuantizeAbove()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteRawSmallerQuantizeAbove\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_4);

            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(TestTags.demoTag2_3, fileInfo, Rewriter.Strategy.Quantize);
            UnitTest.Test(TagUtils.TagSizeV2(new FileInfo(fileName)) % Rewriter.BlockSize == 0);
            TagUtils.StripTagV2(new FileInfo(fileName), 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }

        private static void TestTagUtilsWriteTagBiggerExact()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteTagBiggerExact\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_3);

            FileInfo fileInfo = new FileInfo(fileName);
            Tag tag = TagUtils.ReadTag(fileInfo);
            Frame f = new Frame(tag.DescriptionMap, "MCDI");
            (f.Content as FrameContentBinary).Content = new byte[1024];
            tag.Add(f);

            TagUtils.WriteTagV2(tag, fileInfo, Rewriter.Strategy.Exact);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 1153);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteTagBiggerNeverShrink()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteTagBiggerNeverShrink\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_3);

            FileInfo fileInfo = new FileInfo(fileName);
            Tag tag = TagUtils.ReadTag(fileInfo);
            Frame f = new Frame(tag.DescriptionMap, "MCDI");
            (f.Content as FrameContentBinary).Content = new byte[1024];
            tag.Add(f);

            TagUtils.WriteTagV2(tag, fileInfo, Rewriter.Strategy.NeverShrink);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 1153);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteTagBiggerQuantize()
        {
            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteTagBiggerQuantize\\t1.mp3");
            
            TestTags.WriteDemoMp3(fileName, TestTags.demoTag2_3);
            FileInfo fileInfo = new FileInfo(fileName);
            Tag tag = TagUtils.ReadTag(fileInfo);
            Frame f = new Frame(tag.DescriptionMap, "MCDI");
            (f.Content as FrameContentBinary).Content = new byte[1024];
            tag.Add(f);

            TagUtils.WriteTagV2(tag, fileInfo, Rewriter.Strategy.Quantize);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 2048 + TestTags.mpegDummy.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteTagSmallerExact()
        {
            Tag tag = TagUtils.RawToTag(TestTags.demoTag2_3);
            Frame f = new Frame(tag.DescriptionMap, "MCDI");
            (f.Content as FrameContentBinary).Content = new byte[1024];
            tag.Add(f);

            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteTagSmallerExact\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TagUtils.TagToRaw(tag));

            tag.Remove(f);
            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(tag, fileInfo, Rewriter.Strategy.Exact);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == TagUtils.TagToRaw(tag).Length + TestTags.mpegDummy.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteTagSmallerNeverShrink()
        {
            Tag tag = TagUtils.RawToTag(TestTags.demoTag2_3);
            Frame f = new Frame(tag.DescriptionMap, "MCDI");
            (f.Content as FrameContentBinary).Content = new byte[1024];
            tag.Add(f);

            int len = TagUtils.TagSize(tag);

            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteTagSmallerNeverShrink\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TagUtils.TagToRaw(tag));

            tag.Remove(f);
            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(tag, fileInfo, Rewriter.Strategy.NeverShrink);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == len + TestTags.mpegDummy.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }
        private static void TestTagUtilsWriteTagSmallerQuantize()
        {
            Tag tag = TagUtils.RawToTag(TestTags.demoTag2_3);
            Frame f = new Frame(tag.DescriptionMap, "MCDI");
            (f.Content as FrameContentBinary).Content = new byte[2048];
            tag.Add(f);

            int len = TagUtils.TagSize(tag);

            string fileName = VirtualDrive.VirtualFileName("TestTagUtilsWriteTagSmallerNeverShrink\\t1.mp3");

            TestTags.WriteDemoMp3(fileName, TagUtils.TagToRaw(tag));

            tag.Remove(f);
            FileInfo fileInfo = new FileInfo(fileName);
            TagUtils.WriteTagV2(tag, fileInfo, Rewriter.Strategy.Quantize);
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 1024 + TestTags.mpegDummy.Length);
            TagUtils.StripTagV2(fileInfo, 0);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }

        private static void TestTagUtilsBuildByCodeAndSerializeInVirtualStore()
        {
            foreach (ID3.Version version in ID3.Version.Versions)
            {
                string filename = VirtualDrive.VirtualFileName(
                    "TestID3TagBuildByCodeAndSerializeInVirtualStore\\" + version.ToString() + ".tag");

                Tag tag0 = TestTags.CreateDemoTag(version);

                TagUtils.WriteTag(tag0, new FileInfo(filename));

                Tag tag1 = TagUtils.ReadTag(new FileInfo(filename));

                TagEditor editor0 = new TagEditor(tag0);
                TagEditor editor1 = new TagEditor(tag1);

                UnitTest.Test(editor1.Equals(editor0));
            }
        }
        private static void TestTagUtilsTestCloneUsingRaw()
        {
            ID3.Tag tag = TestTags.CreateDemoTag(Version.v2_3);

            byte[] b0 = TagUtils.TagToRaw(tag);
            byte[] b1 = TagUtils.TagToRaw(tag.Clone());

            UnitTest.Test(ArrayUtils.IsEqual(b0, b1));
        }
        private static void TestTagUtilsRewrite()
        {
            byte[] data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            string fileName = VirtualDrive.VirtualFileName("TestID3TagUtilsRewrite\\t0.mp3");

            Id3FileUtils.SaveFileBinary(fileName, data);

            TagUtils.Rewrite(new FileInfo(fileName), 4, 3, 2, 1);

            Stream s = VirtualDrive.OpenInStream(fileName);
            UnitTest.Test(s.ReadByte() == 0);
            UnitTest.Test(s.ReadByte() == 0);
            UnitTest.Test(s.ReadByte() == 4);
            UnitTest.Test(s.ReadByte() == 5);
            UnitTest.Test(s.ReadByte() == 6);
            UnitTest.Test(s.ReadByte() == 7);
            UnitTest.Test(s.ReadByte() == 8);
            UnitTest.Test(s.ReadByte() == 0);
            UnitTest.Test(s.ReadByte() == -1);
            s.Close();
        }
        private static void TestTagUtilsRewriteMP3()
        {
            byte[] tag20 = TagUtils.TagToRaw(TestTags.CreateDemoTag(Version.v2_3));
            byte[] tag10 = TestTags.demoTag1_0;

            string fileName = VirtualDrive.VirtualFileName("TestID3TagUtilsRewrite\\t1.mp3");

            using (Stream s = VirtualDrive.OpenOutStream(fileName))
            {
                s.Write(tag20, 0, tag20.Length);
                s.WriteByte(0);
                s.WriteByte(0);
                s.Write(TestTags.mpegDummy, 0, TestTags.mpegDummy.Length);
                s.Write(tag10, 0, tag10.Length);
            }

            long offset = TagUtils.OffsetTagToMpegHeader(new FileInfo(fileName));
            UnitTest.Test(offset == 2);

            TagUtils.StripTags(new FileInfo(fileName), 0, 0);

            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(fileName), TestTags.mpegDummy));
        }

        private static void TestSetTagSizeRawV2()
        {
            int inc = 10;

            byte[] raw0 = (byte[]) TestTags.demoTag2_3.Clone();

            int size0 = TagUtils.TagSize(raw0);
            int size1 = size0 + inc;

            byte[] raw1 = TagUtils.RecreateTagWithNewTagSize(raw0, size1);

            UnitTest.Test(raw1.Length == raw0.Length + inc);
            UnitTest.Test(TagUtils.TagSize(raw1) == size1);
        }
    }
}