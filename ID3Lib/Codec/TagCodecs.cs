using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ID3.IO;
using ID3.Utils;
using CoreUtils;
using CoreTest;

namespace ID3.Codec
{
    public abstract class TagHeader : IVersionable
    {
        public static readonly int sizeUnitinialized = Int32.MinValue;

        public abstract int VersionMajor { get; set; }
        public abstract int VersionMinor { get; set; }
        public abstract int HeaderSize { get; }
        public abstract int TotalSize { get; }
        public abstract int Size { get; set; }
        public abstract void Read(Reader reader);
        public abstract void Write(Writer writer);

        public abstract Version[] SupportedVersions { get; }
        public void CheckVersion(Version version)
        {
            if (!this.IsSupported(version))
            {
                throw new VersionInvariant("Check failed: tag header version");
            }

            if (version.Major != VersionMajor
                || version.Major != VersionMajor)
            {
                throw new VersionInvariant("Check failed: header version not up-to-date");
            }
        }

        public abstract TagHeader Clone();
    }

    public abstract class TagCodec : IVersionable
    {
        public abstract TagHeader Header { get; }
        public abstract void Read(Tag tag, Reader reader);
        public abstract void Write(Tag tag, Writer writer);

        public void CheckVersion(Version version)
        {
            if (!this.IsSupported(version))
            {
                throw new VersionInvariant("Check failed: tag codec version");
            }

            Header.CheckVersion(version);
        }
        public abstract Version[] SupportedVersions { get; }

        public override string ToString()
        {
            return Header.ToString();
        }

        public abstract TagCodec Clone();
    }

    class TagCodecV2 : TagCodec
    {
        private HeaderV2 header = new HeaderV2();

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs2_0And2_3And2_4;
            }
        }
        public override TagHeader Header { get { return header; } }
        public override void Read(Tag tag, Reader reader)
        {
            header.Read(reader);

            tag.DescriptionMap = TagDescriptionMap.Instance[
                Version.VersionByMajorMinor(header.VersionMajor, header.VersionMinor)];

            if (header.size > 1)
            {
                long bytesToRead = header.size;
                bool invalidPadding = false;

                try
                {
                    while (bytesToRead > 0
                        && reader.PeekChar() != 0
                        && reader.PeekChar() != -1
                        && !invalidPadding)
                    {
                        long pos0 = reader.Position;

                        long numBytesRead = ReadFrame(tag, reader);

                        long pos1 = reader.Position;

                        if (numBytesRead == -1)
                        {
                            // Invalid but known padding (e.g. MP3e)
                            invalidPadding = true;
                        }

                        bytesToRead -= (pos1 - pos0);
                    }

                    if (!invalidPadding && bytesToRead < 0)
                    {
                        throw new Exception(GetType().Name + ": Read failed");
                    }
                }
                catch (VersionInvariant e)
                {
                    throw e;
                }
                catch (InvalidHeaderFlagsException e)
                {
                    reader.HandleException(e);
                }
                catch (InvalidFrameException e)
                {
                    reader.HandleException(e);
                }
                catch (NotSupportedException e)
                {
                    reader.HandleException(e);
                }
            }
        }
        public override void Write(Tag tag, Writer writer)
        {
            header.Write(writer);

            long bytesToWrite = header.Size;

            bool allFramesUnsynchronized = true;

            foreach (Frame frame in tag.Frames)
            {
                long pos0 = writer.Position;
                int unsync0 = writer.UnsynchronizationCounter;

                frame.Codec.Write(writer, frame);

                int unsync1 = writer.UnsynchronizationCounter;
                long pos1 = writer.Position;

                if (unsync0 == unsync1)
                {
                    allFramesUnsynchronized = false;
                }

                if (header.size != HeaderV2.sizeUnitinialized)
                {
                    long writtenBytes = pos1 - pos0;

                    bytesToWrite -= writtenBytes;

                    if (bytesToWrite < 0)
                    {
                        throw new Exception(GetType().Name + ": frame or header sizes wrong");
                    }
                }
            }

            if (header.size != HeaderV2.sizeUnitinialized)
            {
                for (long i = bytesToWrite; i > 0; i--)
                {
                    writer.WriteByte(0);
                }
            }

            if (header.VersionMajor == 4)
            {
                long pos0 = writer.Position;

                writer.Seek(0, SeekOrigin.Begin);

                header.IsUnsynchronized = allFramesUnsynchronized;
                header.Write(writer);

                writer.Seek(pos0, SeekOrigin.Begin);
            }
        }

        private int ReadFrame(Tag tag, Reader reader)
        {
            Frame frame = tag.DescriptionMap.CreateFrame();

            int numBytesHeaderRead = frame.Codec.ReadHeader(reader, frame);
            if (numBytesHeaderRead == -1)
            {
                // Invalid but known padding (e.g. 'MP3e')
                return -1;
            }
            if (frame.Codec.SizeHeader != numBytesHeaderRead)
            {
                throw new Exception(GetType().Name + " failed: headersize="
                    + frame.Codec.SizeHeader + " bytesRead=" + numBytesHeaderRead);
            }


            int numBytesContentRead = frame.Codec.SizeContent;

            bool exceptionOccured = false;

            try
            {
                numBytesContentRead = frame.Codec.ReadContent(reader, frame);
            }
            catch (CorruptFrameContentException e)
            {
                exceptionOccured = true;
                e.Handle();
            }
            if (frame.Codec.SizeContent != numBytesContentRead)
            {
                throw new Exception(GetType().Name + " failed: numBytesContent="
                    + frame.Codec.SizeContent + " numBytesContentRead=" + numBytesContentRead);
            }

            if (!exceptionOccured
                || CorruptFrameContentException._handling
                == CorruptFrameContentException.Handling.Ignore)
            {
                tag.Add(frame);
            }

            return frame.Codec.SizeHeader + frame.Codec.SizeContent;
        }

        public override TagCodec Clone()
        {
            TagCodecV2 c = new TagCodecV2();
            c.header = (HeaderV2)header.Clone();
            return c;
        }
    }

    class TagCodecV1 : TagCodec
    {
        private HeaderV1 header = new HeaderV1();

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs1_0;
            }
        }
        public override TagHeader Header { get { return header; } }
        public override void Read(Tag tag, Reader reader)
        {
            tag.Clear();

            tag.DescriptionMap = TagDescriptionMap.Instance[
                Version.VersionByMajorMinor(header.VersionMajor, header.VersionMinor)];

            List<string> frameIds = TagDescriptionV1_0.FrameIds;
            foreach (string frameId in frameIds)
            {
                tag.Add(new Frame(TagDescriptionMap.Instance[Version.v1_0], frameId));
            }

            reader.Seek(reader.Length - HeaderV1.totalSize, SeekOrigin.Begin);
            header.Read(reader);

            foreach (Frame f in tag.Frames)
            {
                f.Codec.ReadHeader(reader, f);
                f.Codec.ReadContent(reader, f);
            }
        }
        public override void Write(Tag tag, Writer writer)
        {
            header.Write(writer);

            List<string> frameIds = TagDescriptionV1_0.FrameIds;
            foreach (string frameId in frameIds)
            {
                Frame f = null;

                if (tag.Contains(frameId))
                {
                    f = tag[frameId];
                }
                else
                {
                    f = new Frame(TagDescriptionMap.Instance[Version.v1_0], frameId);
                }

                f.Codec.Write(writer, f);
            }
        }

        public override TagCodec Clone()
        {
            TagCodecV1 c = new TagCodecV1();
            c.header = (HeaderV1)header.Clone();
            return c;
        }
    }
    class TagDescriptionV1_0
    {
        public static Dictionary<string, int> SizeByFrameId
        {
            get
            {
                Dictionary<string, int> sizeMap = new Dictionary<string, int>();
                sizeMap.Add("TT2", 30);
                sizeMap.Add("TP1", 30);
                sizeMap.Add("TAL", 30);
                sizeMap.Add("TYE", 4);
                sizeMap.Add("COM", 29);
                sizeMap.Add("TRK", 1);
                sizeMap.Add("TCO", 1);
                return sizeMap;
            }
        }
        public static List<string> FrameIds
        {
            get
            {
                List<string> tagList = new List<string>();
                tagList.Add("TT2");
                tagList.Add("TP1");
                tagList.Add("TAL");
                tagList.Add("TYE");
                tagList.Add("COM");
                tagList.Add("TRK");
                tagList.Add("TCO");
                return tagList;
            }
        }
    }

    class HeaderV1 : TagHeader
    {
        private const byte byte0 = (byte)'T';
        private const byte byte1 = (byte)'A';
        private const byte byte2 = (byte)'G';

        public const int versionMajor = 1;
        public const int versionMinor = 0;
        public const int headerSize = 3;
        public const int totalSize = 128;

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs1_0;
            }
        }

        public override int VersionMajor
        {
            get
            {
                return versionMajor;
            }
            set { }
        }
        public override int VersionMinor
        {        
            get
            {
                return versionMinor;
            }
            set
            { }
        }
        public override int HeaderSize
        {
            get
            {
                return headerSize;
            }
        }
        public override int TotalSize
        {
            get
            {
                return totalSize;
            }
        }
        public override int Size
        {
            get
            {
                return totalSize - headerSize;
            }
            set { }
        }
        public override void Read(Reader reader)
        {
            if (!VerifiedRead(reader))
            {
                throw new NoTagException(reader.Filename);
            }
        }
        public override void Write(Writer writer)
        {
            writer.WriteByte(byte0);
            writer.WriteByte(byte1);
            writer.WriteByte(byte2);
        }

        public bool VerifiedRead(Reader reader)
        {
            byte byteTmp0 = reader.ReadByte();
            byte byteTmp1 = reader.ReadByte();
            byte byteTmp2 = reader.ReadByte();

            return byteTmp0 == byte0 && byteTmp1 == byte1 && byteTmp2 == byte2;
        }

        public override string ToString()
        {
            return "  Header 1.0\n";
        }

        public override TagHeader Clone()
        {
            return new HeaderV1();
        }
    }

    public class HeaderV2 : TagHeader
    {
        private byte byte0;
        private byte byte1;
        private byte byte2;
        private byte versionMajor;
        private byte versionMinor;
        private byte flags;

        public int size = sizeUnitinialized;

        public const int headerSize = 10;
        public const byte flagUnsynchronisation = 128;
        public const byte flagExtendedHeader = 64;
        public const byte flagExperimentalStage = 32;
        public const byte flagFooterPresent = 16;

        private static bool IsVersionSupported(int _major)
        {
            return _major == 2
                || _major == 3
                || _major == 4;
        }
        private static bool AreValidFlags(int major, int flags)
        {
            return (major == 2 && (flags & 0x3F) != 0)
                || (major == 3 && (flags & 0x1F) != 0)
                || (major == 4 && (flags & 0xF) != 0);
        }

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs2_0And2_3And2_4;
            }
        }

        public override int VersionMajor
        {
            get { return versionMajor; }
            set { versionMajor = (byte)value; }
        }
        public override int VersionMinor
        {
            get { return versionMinor; }
            set { versionMinor = (byte)value; }
        }
        public override int HeaderSize
        {
            get { return headerSize; }
        }
        public override int TotalSize
        {
            get { return HeaderSize + size; }
        }
        public override int Size
        {
            get { return size; }
            set { size = value; }
        }

        public override void Read(Reader reader)
        {
            byte0 = reader.ReadByte();
            byte1 = reader.ReadByte();
            byte2 = reader.ReadByte();
            if (byte0 != 'I' || byte1 != 'D' || byte2 != '3')
            {
                throw new NoTagException(reader.Filename);
            }

            versionMajor = reader.ReadByte();
            versionMinor = reader.ReadByte();
            if (!IsVersionSupported(versionMajor))
            {
                throw new InvalidVersionException(reader.Filename, versionMajor, versionMinor);
            }

            flags = reader.ReadByte();
            if (AreValidFlags(versionMajor, flags))
            {
                throw new InvalidHeaderFlagsException(
                    "Exception in file \"" + reader.Filename + "\".\n"
                    + "   The tag contains INVALID flags (" + flags + ")");
            }
            if (!IgnoreUnsupportedFlags && HasExtendedHeader)
            {
                throw new NotSupportedException("Extended header found: " + reader.Filename);
            }
            if (!IgnoreUnsupportedFlags && HasFooterPresent)
            {
                throw new NotSupportedException("Footer found: " + reader.Filename);
            }

            size = reader.ReadBigEndian4HighestBitZero();
                
            reader.Unsynchronization = IsUnsynchronized;
        }
        public override void Write(Writer writer)
        {
            writer.WriteByte((byte)'I');
            writer.WriteByte((byte)'D');
            writer.WriteByte((byte)'3');
            writer.WriteByte(versionMajor);
            writer.WriteByte(versionMinor);
            writer.WriteByte(flags);
            writer.WriteBigEndian4HighestBitZero(size);

            writer.Unsynchronization = IsUnsynchronized;
        }

        public bool HasExtendedHeader
        {
            get
            {
                return (flags & flagExtendedHeader) != 0;
            }
        }
        public bool IsUnsynchronized
        {
            get
            {
                return (flags & flagUnsynchronisation) != 0;
            }
            set
            {
                if (value)
                {
                    flags |= flagUnsynchronisation;
                }
                else
                {
                    flags &= (byte)((~flagUnsynchronisation) & 0xFF);
                }
            }
        }
        public bool HasExperimentalStage
        {
            get
            {
                return (flags & flagExperimentalStage) != 0;
            }
        }
        public bool HasFooterPresent
        {
            get
            {
                return (flags & flagFooterPresent) != 0;
            }
        }

        public bool IgnoreUnsupportedFlags
        {
            get;
            set;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("  Header ");
            sb.Append(versionMajor);
            sb.Append(".");
            sb.Append(versionMinor);
            sb.Append(" Flags: ");
            sb.Append(flags);
            sb.Append(" Size: ");
            sb.Append(size);
            sb.Append(" Bytes\n");

            if (HasExtendedHeader)
            {
                sb.Append("  Extended header found\n");
            }
            if (IsUnsynchronized)
            {
                sb.Append("  Unsynchronisation found\n");
            }
            if (HasExperimentalStage)
            {
                sb.Append("  Header in experimental stage\n");
            }

            return sb.ToString();
        }

        public override TagHeader Clone()
        {
            HeaderV2 h = new HeaderV2();

            h.byte0 = byte0;
            h.byte1 = byte1;
            h.byte2 = byte2;
            h.versionMajor = versionMajor;
            h.versionMinor = versionMinor;
            h.flags = flags;
            h.size = size;

            return h;
        }
    }

    public class TestTagCodecs
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagCodecs));
        }

        private static void TestHeaderV1()
        {
            HeaderV1 header = new HeaderV1();
            header.CheckVersion(Version.v1_0);
            UnitTest.Test(header.HeaderSize == 3);

            byte[] data = { (byte)'T', (byte)'A', (byte)'G' };
            using (Reader reader = new Reader(data))
            {
                header.Read(reader);
            }


            using (Writer writer = new Writer())
            {
                header.Write(writer);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
        private static void TestHeaderV2()
        {
            HeaderV2 header = new HeaderV2();
            header.IgnoreUnsupportedFlags = true;

            byte[] data =
            {
                // File identificator
                (byte)'I', (byte)'D', (byte)'3',

                // Version
                3, 0,

                // Flags
                0xE0,

                // Size
                0, 0, 0, 0
            };

            using (Reader reader = new Reader(data))
            {
                header.Read(reader);
                header.CheckVersion(Version.v2_3);
            }

            UnitTest.Test(header.HeaderSize == 10);
            UnitTest.Test(header.VersionMajor == 3);
            UnitTest.Test(header.VersionMinor == 0);
            UnitTest.Test(header.IsUnsynchronized);
            UnitTest.Test(header.HasExtendedHeader);
            UnitTest.Test(header.HasExperimentalStage);

            using (Writer writer = new Writer())
            {
                header.Write(writer);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }

        private static void TestTagCodec1_0()
        {
            TestTagCodecReadWrite(ID3.TestTags.demoTag1_0);
        }
        private static void TestTagCodec2_0()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_0, ID3.TestTags.mcdiPayload);
        }
        private static void TestTagCodec2_3()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_3, ID3.TestTags.mcdiPayload);
        }
        private static void TestTagCodec2_4()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_4, ID3.TestTags.mcdiPayload);
        }

        private static void TestTagCodec2_0Unsynchronized()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_0Unsynchronized, ID3.TestTags.mcdiPayload);
        }
        private static void TestTagCodec2_3Unsynchronized()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_3Unsynchronized, ID3.TestTags.mcdiPayload);
        }
        private static void TestTagCodec2_4Unsynchronized()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_4Unsynchronized, ID3.TestTags.mcdiPayload);
        }
        private static void TestTagCodec2_4UnsynchronizedAll()
        {
            TestTagCodecV2(ID3.TestTags.demoTag2_4UnsynchronizedAll, ID3.TestTags.mcdiPayload);
        }

        private static void TestTagCodec2_0ExperimentalFrameId()
        {
            byte[] data =
            {
                // File identificator
                (byte)'I', (byte)'D', (byte)'3',
                // Version
                2, 0,
                // Flags
                0x0,
                // Size
                0, 0, 0, 14,

                // Frame ID
                (byte) 'X', (byte) 'T', (byte) 'T',
                // Frame Size
                0, 0, 8,
                // Payload
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
            };

            TestTagCodecReadWrite(data);
        }
        private static void TestTagCodec2_3ExperimentalFrameId()
        {
            byte[] data =
            {
                // File identificator
                (byte)'I', (byte)'D', (byte)'3',
                // Version
                3, 0,
                // Flags
                0x0,
                // Size
                0, 0, 0, 18,

                // Frame ID
                (byte) 'X', (byte) 'T', (byte) 'T', (byte) 'T',
                // Frame Size
                0, 0, 0, 8,
                // Flags
                0, 0,
                // Payload
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
            };

            TestTagCodecReadWrite(data);
        }
        private static void TestTagCodec2_4ExperimentalFrameId()
        {
            byte[] data =
            {
                // File identificator
                (byte)'I', (byte)'D', (byte)'3',
                // Version
                4, 0,
                // Flags
                0x0,
                // Size
                0, 0, 0, 18,

                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'D', (byte) 'I',
                // Frame Size
                0, 0, 0, 8,
                // Flags
                0, 0,
                // Payload
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
            };

            TestTagCodecReadWrite(data);
        }

        private static void TestTagCodec2_0InvalidFrameId()
        {
            Tag tag = new Tag(new TagCodecV2());

            using (Reader reader = new Reader(TestTags.demoTag2_0BrokenInvalidFrameId))
            {
                tag.Read(reader);
            }

            UnitTest.Test(tag.Frames.Count() == 7);

            using (Writer writer = new Writer())
            {
                tag.Write(writer);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, TestTags.demoTag2_0BrokenInvalidFrameId));
            }
        }
        private static void TestTagCodec2_0InvalidFrameIdThrows()
        {
            Tag tag = new Tag(new TagCodecV2());

            using (Reader reader = new Reader(TestTags.demoTag2_0BrokenInvalidFrameId))
            {
                reader.ThrowExceptions = true;
                UnitTest.TestException(() => tag.Read(reader), typeof(InvalidFrameException));
            }

            UnitTest.Test(tag.Frames.Count() == 0);
        }

        private static void TestTagCodecV2(byte[] data, byte[] mcdiPayload)
        {
            Tag tag = new Tag(new TagCodecV2());

            using (Reader reader = new Reader(data))
            {
                tag.Read(reader);
            }

            UnitTest.Test(tag.Frames.Count() == 6);
            TagEditor editor = new TagEditor(tag);
            UnitTest.Test(editor.Album == "Album");
            UnitTest.Test(editor.Artist == "Artist");
            UnitTest.Test(editor.Title == "Title");
            UnitTest.Test(editor.Comment == "Comment");
            UnitTest.Test(editor.TrackNumber == "1");
            UnitTest.Test(ArrayUtils.IsEqual(editor.MusicCdIdentifier, mcdiPayload));

            using (Writer writer = new Writer())
            {
                tag.Write(writer);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
        private static void TestTagCodecReadWrite(byte[] data)
        {
            Tag tag = new Tag(TagUtils.HasTagV1(data)
                ? (TagCodec)new TagCodecV1()
                : (TagCodec)new TagCodecV2());

            using (Reader reader = new Reader(data))
            {
                tag.Read(reader);
            }

            using (Writer writer = new Writer())
            {
                tag.Write(writer);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
    }
}
