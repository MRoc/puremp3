using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.IO;
using CoreTest;
using System.Diagnostics;
using CoreUtils;

namespace ID3.Codec
{
    public abstract class FrameCodec : IVersionable
    {
        public abstract int ReadHeader(Reader reader, Frame f);
        public abstract int ReadContent(Reader reader, Frame f);

        public abstract void Write(Writer writer, Frame f);

        public abstract int SizeHeader
        {
            get;
        }
        public abstract int SizeContent
        {
            get;
            protected set;
        }

        public abstract TagDescription DescriptionMap
        {
            get;
        }

        public abstract Version[] SupportedVersions
        {
            get;
        }
        public void CheckVersion(Version version)
        {
            if (!this.IsSupported(version))
            {
                throw new VersionInvariant("Check failed: frame codec version");
            }
        }

        public abstract FrameCodec Clone();

        public override string ToString()
        {
            return GetType().Name;
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        protected void CheckPreconditions(Frame f)
        {
            if (f.Codec != this)
            {
                throw new Exception("Codec unrelated to frame");
            }
            if (!this.IsSupported(f.DescriptionMap.Version))
            {
                throw new VersionInvariant("FrameCodec " + GetType().Name
                    + " version not matching frame version " + f.DescriptionMap.Version);
            }
        }

        protected bool CheckFrameId(Frame f, Reader reader)
        {
            if (!DescriptionMap.IsValidID(f.FrameId) && !FrameDescription.IsExperimentalFrameId(f.FrameId))
            {
                if (DescriptionMap.IsInvalidButKnownPadding(f.FrameId))
                {
                    return false;
                }
                else if (FrameDescription.MaybeFrameId(f.FrameId))
                {
                    reader.HandleException(new InvalidFrameException(reader, f.FrameId));
                }
                else
                {
                    throw new InvalidFrameException(reader, f.FrameId);
                }
            }

            return true;
        }
    }

    class FrameCodec2_4 : FrameCodec
    {
        private const int flagTagAlterPreservation  = 0x4000;
        private const int flagFileAlterPreservation = 0x2000;
        private const int flagReadOnly              = 0x1000;
        private const int flagGroupingIdentity      = 0x40;
        private const int flagCompression           = 0x8;
        private const int flagEncryption            = 0x4;
        private const int flagUnsynchronisation     = 0x2;
        private const int flagDataLengthIndicator   = 0x1;

        private const int flagInvalidMask           = 0x8FB0;

        public FrameCodec2_4()
        {
        }

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs2_4;
            }
        }

        public override int ReadHeader(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            f.FrameId = reader.ReadString(4);

            if (!CheckFrameId(f, reader))
            {
                return -1;
            }

            SizeContent = reader.ReadBigEndian4HighestBitZero();
            Flags = reader.ReadBigEndian2();

            if (!AreValidFlags)
            {
                throw new InvalidHeaderFlagsException(
                    "Exception in file \"" + reader.Filename + "\".\n"
                    + "   The frame \"" + f.FrameId + "\" contains INVALID flags (" + Flags + ")");
            }
            if (!IgnoreUnsupportedFlags && !AreSupportedFlags)
            {
                throw new NotSupportedException(
                    "Exception in file \"" + reader.Filename + "\".\n"
                    + "   The frame \"" + f.FrameId + "\" contains not supported flags (" + Flags + ")\n"
                    + "      Compression..: \"" + IsCompression + "\"\n"
                    + "      Encryption...: \"" + IsEncryption + "\"\n"
                    + "      GroupIdentity: \"" + IsGroupingIdentity + "\"");
            }

            return SizeHeader;
        }
        public override int ReadContent(Reader reader, Frame f)
        {
            CheckPreconditions(f);
            
            if (SizeContent > 0)
            {
                using (ReaderUnsynchronizationHelper helper = new ReaderUnsynchronizationHelper(
                    reader, reader.Unsynchronization || IsUnsynchronisation))
                {
                    using (ReaderStream tmpStream = new ReaderStream(
                        reader, SizeContent, Reader.UnsyncMode.CountIncludesUnsyncBytes))
                    {
                        try
                        {
                            f.Content.Codec.Read(tmpStream, SizeContent, f.Content);
                        }
                        catch (CorruptFrameContentException e)
                        {
                            throw e;
                        }
                        finally
                        {
                            tmpStream.SeekToStreamEnd();
                        }
                    }
                }
            }

            return SizeContent;
        }
        public override void Write(Writer writer, Frame f)
        {
            CheckPreconditions(f);

            SizeContent = f.Content.Codec.RequiredBytes(
                f.Content, writer.Unsynchronization || IsUnsynchronisation,
                Reader.UnsyncMode.CountIncludesUnsyncBytes);

            writer.WriteString(f.FrameId, 4);
            writer.WriteBigEndian4HighestBitZero(SizeContent);
            writer.WriteBigEndian2(Flags);

            if (SizeContent > 0)
            {
                using (WriterUnsynchronizationHelper helper = new WriterUnsynchronizationHelper(
                    writer, writer.Unsynchronization || IsUnsynchronisation))
                {
                    using (WriterStream stream = new WriterStream(writer))
                    {
                        f.Content.Codec.Write(stream, f.Content);
                    }
                }
            }
        }
        public override int SizeHeader
        {
            get
            {
                return 10;
            }
        }
        public override int SizeContent
        {
            get;
            protected set;
        }
        public override TagDescription DescriptionMap
        {
            get
            {
                return TagDescriptionMap.Instance[Version.v2_4];
            }
        }

        public bool AreValidFlags
        {
            get { return (Flags & flagInvalidMask) == 0; }
        }
        public bool AreSupportedFlags
        {
            get
            {
                return !IsCompression && !IsEncryption && !IsGroupingIdentity;
            }
        }
        public bool IgnoreUnsupportedFlags
        {
            get;
            set;
        }

        public bool IsTagAlterPreservation
        {
            get { return ((Flags & flagTagAlterPreservation) == flagTagAlterPreservation); }
        }
        public bool IsFileAlterPreservation
        {
            get { return ((Flags & flagFileAlterPreservation) == flagFileAlterPreservation); }
        }
        public bool IsReadOnly
        {
            get { return ((Flags & flagReadOnly) == flagReadOnly); }
        }
        public bool IsCompression
        {
            get { return ((Flags & flagCompression) == flagCompression); }
        }
        public bool IsEncryption
        {
            get { return ((Flags & flagEncryption) == flagEncryption); }
        }
        public bool IsGroupingIdentity
        {
            get { return ((Flags & flagGroupingIdentity) == flagGroupingIdentity); }
        }
        public bool IsUnsynchronisation
        {
            get
            {
                return (Flags & flagUnsynchronisation) == flagUnsynchronisation;
            }
            set
            {
                if (value)
                {
                    Flags |= flagUnsynchronisation;
                }
                else
                {
                    Flags &= ~flagUnsynchronisation;
                }
            }
        }
        public bool IsDataLengthIndicator
        {
            get { return ((Flags & flagDataLengthIndicator) == flagDataLengthIndicator); }
        }
        
        private int Flags
        {
            get;
            set;
        }
        
        public override FrameCodec Clone()
        {
            FrameCodec2_4 result = new FrameCodec2_4();
            result.SizeContent = SizeContent;
            result.Flags = Flags;
            return result;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(GetType().Name);
            result.Append(" Size: ");
            result.Append(SizeContent);

            if (Flags != 0)
            {
                result.Append(" Flags:");
                if (IsTagAlterPreservation) result.Append(" TagAlterPreservation");
                if (IsFileAlterPreservation) result.Append(" FileAlterPreservation");
                if (IsReadOnly) result.Append(" ReadOnly");
                if (IsGroupingIdentity) result.Append(" GroupingIdentity");
                if (IsCompression) result.Append(" Compression");
                if (IsEncryption) result.Append(" Encryption");
                if (IsUnsynchronisation) result.Append(" Unsynchronisation");
                if (IsDataLengthIndicator) result.Append(" DataLengthIndicator");
            }

            return result.ToString();
        }
    }
    class FrameCodec2_3 : FrameCodec
    {
        private const int flagTagAlterPreservation  = 0x8000;
        private const int flagFileAlterPreservation = 0x4000;
        private const int flagReadOnly              = 0x2000;
        private const int flagCompression           = 0x80;
        private const int flagEncryption            = 0x40;
        private const int flagGroupingIdentity      = 0x20;

        private const int flagInvalidMask           = 0x1F1F;

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs2_3;
            }
        }

        public FrameCodec2_3()
        {
        }

        public override int ReadHeader(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            f.FrameId = reader.ReadString(4);

            if (!CheckFrameId(f, reader))
            {
                return -1;
            }

            SizeContent = reader.ReadBigEndian4();
            Flags = reader.ReadBigEndian2();

            if (!AreValidFlags)
            {
                throw new InvalidHeaderFlagsException(
                    "Exception in file \"" + reader.Filename + "\".\n"
                    + "   The frame \"" + f.FrameId + "\" contains INVALID flags (" + Flags + ")");
            }
            if (!IgnoreUnsupportedFlags && !AreSupportedFlags)
            {
                throw new NotSupportedException(
                    "Exception in file \"" + reader.Filename + "\".\n"
                    + "   The frame \"" + f.FrameId + "\" contains not supported flags (" + Flags + ")\n"
                    + "      Compression..: \"" + IsCompression + "\"\n"
                    + "      Encryption...: \"" + IsEncryption + "\"\n"
                    + "      GroupIdentity: \"" + IsGroupingIdentity + "\"");
            }

            return SizeHeader;
        }
        public override int ReadContent(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            if (SizeContent > 0)
            {
                using (ReaderStream tmpStream = new ReaderStream(
                    reader, SizeContent, Reader.UnsyncMode.CountExcludesUnsyncBytes))
                {
                    try
                    {
                        f.Content.Codec.Read(tmpStream, SizeContent, f.Content);
                    }
                    catch (CorruptFrameContentException e)
                    {
                        throw e;
                    }
                    finally
                    {
                        tmpStream.SeekToStreamEnd();
                    }
                }
            }

            return SizeContent;
        }
        private void WriteHeader(Writer writer, Frame f)
        {
            CheckPreconditions(f);

            writer.WriteString(f.FrameId, 4);
            writer.WriteBigEndian4(SizeContent);
            writer.WriteBigEndian2(Flags);
        }
        public override void Write(Writer writer, Frame f)
        {
            CheckPreconditions(f);

            long pos0 = writer.Position;
            WriteHeader(writer, f);

            long pos1 = writer.Position;
            using (WriterStream stream = new WriterStream(writer))
            {
                f.Content.Codec.Write(stream, f.Content);
            }
            long pos2 = writer.Position;

            SizeContent = (int)(pos2 - pos1) - writer.UnsynchronizationCounter;

            writer.Seek(pos0, System.IO.SeekOrigin.Begin);
            WriteHeader(writer, f);

            writer.Seek(pos2, System.IO.SeekOrigin.Begin);
        }
        public override int SizeHeader
        {
            get
            {
                return 10;
            }
        }
        public override int SizeContent
        {
            get;
            protected set;
        }
        public override TagDescription DescriptionMap
        {
            get
            {
                return TagDescriptionMap.Instance[Version.v2_3];
            }
        }

        private bool AreValidFlags
        {
            get { return (Flags & flagInvalidMask) == 0; }
        }
        public bool AreSupportedFlags
        {
            get
            {
                return !IsCompression && !IsEncryption && !IsGroupingIdentity;
            }
        }
        public bool IgnoreUnsupportedFlags
        {
            get;
            set;
        }

        public bool IsTagAlterPreservation
        {
            get { return ((Flags & flagTagAlterPreservation) == flagTagAlterPreservation); }
        }
        public bool IsFileAlterPreservation
        {
            get { return ((Flags & flagFileAlterPreservation) == flagFileAlterPreservation); }
        }
        public bool IsReadOnly
        {
            get { return ((Flags & flagReadOnly) == flagReadOnly); }
        }
        public bool IsCompression
        {
            get { return ((Flags & flagCompression) == flagCompression); }
        }
        public bool IsEncryption
        {
            get { return ((Flags & flagEncryption) == flagEncryption); }
        }
        public bool IsGroupingIdentity
        {
            get { return ((Flags & flagGroupingIdentity) == flagGroupingIdentity); }
        }

        private int Flags
        {
            get;
            set;
        }

        public override FrameCodec Clone()
        {
            FrameCodec2_3 result = new FrameCodec2_3();
            result.SizeContent = SizeContent;
            result.Flags = Flags;
            return result;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(GetType().Name);
            result.Append(" Size: ");
            result.Append(SizeContent);

            if (Flags != 0)
            {
                result.Append(" Flags:");
                if (IsTagAlterPreservation) result.Append(" TagAlterPreservation");
                if (IsFileAlterPreservation) result.Append(" FileAlterPreservation");
                if (IsReadOnly) result.Append(" ReadOnly");
                if (IsGroupingIdentity) result.Append(" GroupingIdentity");
                if (IsCompression) result.Append(" Compression");
                if (IsEncryption) result.Append(" Encryption");
            }

            return result.ToString();
        }
    }
    class FrameCodec2_0 : FrameCodec
    {
        public FrameCodec2_0()
        {
        }

        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs2_0;
            }
        }

        public override int ReadHeader(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            f.FrameId = reader.ReadString(3);

            if (!CheckFrameId(f, reader))
            {
                return -1;
            }

            SizeContent = reader.ReadBigEndian3();

            return SizeHeader;
        }
        public override int ReadContent(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            if (SizeContent > 0)
            {
                using (ReaderStream tmpStream = new ReaderStream(reader, SizeContent, Reader.UnsyncMode.CountExcludesUnsyncBytes))
                {
                    try
                    {
                        f.Content.Codec.Read(tmpStream, SizeContent, f.Content);
                    }
                    catch (CorruptFrameContentException e)
                    {
                        throw e;
                    }
                    finally
                    {
                        tmpStream.SeekToStreamEnd();
                    }
                }
            }

            return SizeContent;
        }
        public override void Write(Writer writer, Frame f)
        {
            CheckPreconditions(f);

            SizeContent = f.Content.Codec.RequiredBytes(f.Content, writer.Unsynchronization, Reader.UnsyncMode.CountExcludesUnsyncBytes);

            writer.WriteString(f.FrameId, 3);
            writer.WriteBigEndian3(SizeContent);

            if (SizeContent > 0)
            {
                using (WriterStream stream = new WriterStream(writer))
                {
                    f.Content.Codec.Write(stream, f.Content);
                }
            }
        }
        public override int SizeHeader
        {
            get
            {
                return 6;
            }
        }
        public override int SizeContent
        {
            get;
            protected set;
        }
        public override TagDescription DescriptionMap
        {
            get
            {
                return TagDescriptionMap.Instance[Version.v2_0];
            }
        }
                
        public override FrameCodec Clone()
        {
            FrameCodec2_0 result = new FrameCodec2_0();
            result.SizeContent = SizeContent;
            return result;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(GetType().Name);
            result.Append(" Size: ");
            result.Append(SizeContent);

            return result.ToString();
        }
    }
    class FrameCodec1_0 : FrameCodec
    {
        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs1_0;
            }
        }

        public override int ReadHeader(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            SizeContent = TagDescriptionV1_0.SizeByFrameId[f.FrameId];

            return 0;
        }
        public override int ReadContent(Reader reader, Frame f)
        {
            CheckPreconditions(f);

            FrameDescription.FrameType type = DescriptionMap[f.FrameId].Type;

            using (ReaderStream stream = new ReaderStream(reader, SizeContent, Reader.UnsyncMode.CountExcludesUnsyncBytes))
            {
                (f.Content.Codec as FrameContentCodec1_0).FrameId = f.FrameId;
                f.Content.Codec.Read(stream, SizeContent, f.Content);
                stream.SeekToStreamEnd();
            }

            return SizeContent;
        }
        public override void Write(Writer writer, Frame f)
        {
            CheckPreconditions(f);

            int size = TagDescriptionV1_0.SizeByFrameId[f.FrameId];
            FrameDescription.FrameType type = DescriptionMap[f.FrameId].Type;

            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            (f.Content.Codec as FrameContentCodec1_0).FrameId = f.FrameId;
            f.Content.Codec.Write(stream, f.Content);

            byte[] buffer = stream.GetBuffer();
            for (int i = 0; i < size; ++i)
            {
                if (i < stream.Length)
                {
                    writer.WriteByte(buffer[i]);
                }
                else
                {
                    writer.WriteByte(0);
                }
            }
        }
        public override int SizeHeader
        {
            get
            {
                return 0;
            }
        }
        public override int SizeContent
        {
            get;
            protected set;
        }
        public override TagDescription DescriptionMap
        {
            get
            {
                return TagDescriptionMap.Instance[Version.v1_0];
            }
        }

        public override FrameCodec Clone()
        {
            FrameCodec1_0 result = new FrameCodec1_0();
            result.SizeContent = SizeContent;
            return result;
        }
    }

    public class TestFrameCodecs
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestFrameCodecs));
        }

        private static void TestFrameCodec1_0()
        {
            byte[] data = new byte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            FrameCodec1_0 codec = new FrameCodec1_0();
            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v1_0], "TT2");
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(codec.SizeContent == 30);

            using (Writer writer = new Writer())
            {
                codec.Write(writer, frame);
            }
        }
        private static void TestFrameCodec2_0()
        {
            byte[] data = new byte[]
            {
                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'I',

                // Size
                0, 0, 0
            };

            FrameCodec2_0 codec = new FrameCodec2_0();
            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v2_0]);
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(frame.FrameId == "MCI");
            UnitTest.Test(codec.SizeContent == 0);

            using (Writer writer = new Writer())
            {
                codec.Write(writer, frame);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
        private static void TestFrameCodec2_3()
        {
            byte[] data = new byte[]
            {
                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'D', (byte) 'I',

                // Size
                0, 0, 0, 0,

                // Flags
                0xE0, 0xE0
            };

            FrameCodec2_3 codec = new FrameCodec2_3();
            codec.IgnoreUnsupportedFlags = true;

            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v2_3]);
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(frame.FrameId == "MCDI");
            UnitTest.Test(codec.SizeContent == 0);

            UnitTest.Test(codec.IsTagAlterPreservation);
            UnitTest.Test(codec.IsFileAlterPreservation);
            UnitTest.Test(codec.IsReadOnly);
            UnitTest.Test(codec.IsGroupingIdentity);
            UnitTest.Test(codec.IsCompression);
            UnitTest.Test(codec.IsEncryption);

            using (Writer writer = new Writer())
            {
                codec.Write(writer, frame);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
        private static void TestFrameCodec2_4()
        {
            byte[] data = new byte[]
            {
                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'D', (byte) 'I',

                // Size
                0, 0, 0, 0,

                // Flags
                0x70, 0x4F
            };

            FrameCodec2_4 codec = new FrameCodec2_4();
            codec.IgnoreUnsupportedFlags = true;

            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v2_4]);
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(frame.FrameId == "MCDI");
            UnitTest.Test(codec.SizeContent == 0);

            UnitTest.Test(codec.IsTagAlterPreservation);
            UnitTest.Test(codec.IsFileAlterPreservation);
            UnitTest.Test(codec.IsReadOnly);
            UnitTest.Test(codec.IsGroupingIdentity);
            UnitTest.Test(codec.IsCompression);
            UnitTest.Test(codec.IsEncryption);
            UnitTest.Test(codec.IsUnsynchronisation);
            UnitTest.Test(codec.IsDataLengthIndicator);

            using (Writer writer = new Writer())
            {
                codec.Write(writer, frame);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }

        private static void TestFrameCodec2_0Desynchronized()
        {
            byte[] data = new byte[]
            {
                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'I',

                // Size
                0, 0, 6,

                // Payload
                0xFF, 0, 0xFF, 0x1, 0x2, 0xFF, 0, 0xFF
            };

            byte[] dstPlayload = new byte[] { 0xFF, 0xFF, 0x1, 0x2, 0xFF, 0xFF };

            FrameCodec2_0 codec = new FrameCodec2_0();

            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v2_0]);
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                reader.Unsynchronization = true;
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(ArrayUtils.IsEqual((frame.Content as FrameContentBinary).Content, dstPlayload));

            using (Writer writer = new Writer())
            {
                writer.Unsynchronization = true;
                codec.Write(writer, frame);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
        private static void TestFrameCodec2_3Desynchronized()
        {
            byte[] data = new byte[]
            {
                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'D', (byte) 'I',

                // Size
                0, 0, 0, 6,

                // Flags
                0xE0, 0xE0,

                // Payload
                0xFF, 0, 0xFF, 0x1, 0x2, 0xFF, 0, 0xFF
            };

            byte[] dstPlayload = new byte[] { 0xFF, 0xFF, 0x1, 0x2, 0xFF, 0xFF };

            FrameCodec2_3 codec = new FrameCodec2_3();
            codec.IgnoreUnsupportedFlags = true;

            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v2_3]);
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                reader.Unsynchronization = true;
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(ArrayUtils.IsEqual((frame.Content as FrameContentBinary).Content, dstPlayload));

            using (Writer writer = new Writer())
            {
                writer.Unsynchronization = true;
                codec.Write(writer, frame);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
        private static void TestFrameCodec2_4Desynchronized()
        {
            byte[] data = new byte[]
            {
                // Frame ID
                (byte) 'M', (byte) 'C', (byte) 'D', (byte) 'I',

                // Size
                0, 0, 0, 8,

                // Flags
                0x70, 0x4F,

                // Payload
                0xFF, 0, 0xFF, 0x1, 0x2, 0xFF, 0, 0xFF
            };

            byte[] dstPlayload = new byte[] { 0xFF, 0xFF, 0x1, 0x2, 0xFF, 0xFF };

            FrameCodec2_4 codec = new FrameCodec2_4();
            codec.IgnoreUnsupportedFlags = true;

            Frame frame = new Frame(TagDescriptionMap.Instance[Version.v2_4]);
            frame.Codec = codec;

            using (Reader reader = new Reader(data))
            {
                codec.ReadHeader(reader, frame);
                codec.ReadContent(reader, frame);
            }

            UnitTest.Test(ArrayUtils.IsEqual(
                (frame.Content as FrameContentBinary).Content, dstPlayload));

            using (Writer writer = new Writer())
            {
                codec.Write(writer, frame);
                UnitTest.Test(ArrayUtils.IsEqual(writer.OutData, data));
            }
        }
    }
}
