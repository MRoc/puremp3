using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ID3.IO;
using ID3.Utils;
using CoreUtils;
using CoreTest;
using CoreVirtualDrive;

namespace ID3.IO
{
    public class TestID3IO
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestID3IO));
        }

        static void TestReader()
        {
            byte[] src = CreateTestArray();

            using (Reader reader = new Reader(src))
            {
                for (int i = 0; i < src.Length; ++i)
                {
                    UnitTest.Test(reader.Position == i);
                    UnitTest.Test(src[i] == reader.PeekChar());
                    UnitTest.Test(src[i] == reader.ReadByte());
                }
                UnitTest.Test(reader.UnsynchronizationCounter == 0);
                UnitTest.Test(-1 == reader.PeekChar());
            }
        }
        static void TestWriter()
        {
            byte[] src = CreateTestArray();

            using (Writer writer = new Writer())
            {
                for (int i = 0; i < src.Length; ++i)
                {
                    UnitTest.Test(writer.Position == i);
                    writer.WriteByte(src[i]);
                }

                UnitTest.Test(writer.UnsynchronizationCounter == 0);
                UnitTest.Test(ArrayUtils.IsEqual(src, writer.OutData));
            }
        }

        static void TestReaderSeekAndPosition()
        {
            byte[] src = CreateTestArray();

            using (Reader reader = new Reader(src))
            {
                UnitTest.Test(reader.Length == src.Length);
                for (int i = src.Length - 1; i > 0; --i)
                {
                    reader.Seek(i, SeekOrigin.Begin);
                    UnitTest.Test(reader.Position == i);
                    UnitTest.Test(src[i] == reader.PeekChar());
                }

                reader.Seek(-1, SeekOrigin.End);
                for (int i = src.Length - 1; i > 0; --i)
                {
                    UnitTest.Test(src[i] == reader.PeekChar());
                    UnitTest.Test(reader.Position == i);
                    reader.Seek(-1, SeekOrigin.Current);
                }
            }
        }
        static void TestWriterSeekAndPositon()
        {
            byte[] src = CreateTestArray();

            using (Writer writer = new Writer())
            {
                for (int i = 0; i < src.Length; i++)
                {
                    writer.WriteByte(src[i]);
                }

                for (int i = src.Length / 2; i < src.Length; i++)
                {
                    writer.Seek(i, SeekOrigin.Begin);
                    UnitTest.Test(writer.Position == i);

                    writer.WriteByte(src[i]);
                }
                UnitTest.Test(writer.Position == src.Length);
                UnitTest.Test(ArrayUtils.IsEqual(src, writer.OutData));
            }

        }

        static void TestReadWriteBigEndian4HighestBitZero()
        {
            int count = (1 << 16);

            for (int i = 0; i < count; i++)
            {
                byte[] data = null;
                using (Writer writer = new Writer())
                {
                    writer.WriteBigEndian4HighestBitZero(i);
                    data = writer.OutData;
                }

                UnitTest.Test(data.Length == 4);
                UnitTest.Test((data[0] & 0x80) == 0);
                UnitTest.Test((data[1] & 0x80) == 0);
                UnitTest.Test((data[2] & 0x80) == 0);
                UnitTest.Test((data[3] & 0x80) == 0);

                using (Reader reader = new Reader(data))
                {
                    int tmp = reader.ReadBigEndian4HighestBitZero();
                    UnitTest.Test(tmp == i);
                }
            }
        }
        static void TestReadWriteWriteBigEndian2()
        {
            int count = (1 << 16);

            for (int i = 0; i < count; i++)
            {
                byte[] data = null;
                using (Writer writer = new Writer())
                {
                    writer.WriteBigEndian2(i);
                    data = writer.OutData;
                }

                UnitTest.Test(data.Length == 2);

                using (Reader reader = new Reader(data))
                {
                    int tmp = reader.ReadBigEndian2();
                    UnitTest.Test(tmp == i);
                }
            }
        }
        static void TestReadWriteWriteBigEndian3()
        {
            int count = (1 << 16);

            for (int i = 0; i < count; i++)
            {
                byte[] data = null;
                using (Writer writer = new Writer())
                {
                    writer.WriteBigEndian3(i);
                    data = writer.OutData;
                }

                UnitTest.Test(data.Length == 3);

                using (Reader reader = new Reader(data))
                {
                    int tmp = reader.ReadBigEndian3();
                    UnitTest.Test(tmp == i);
                }
            }
        }
        static void TestReadWriteWriteBigEndian4()
        {
            int count = (1 << 16);

            for (int i = 0; i < count; i++)
            {
                byte[] data = null;
                using (Writer writer = new Writer())
                {
                    writer.WriteBigEndian4(i);
                    data = writer.OutData;
                }

                UnitTest.Test(data.Length == 4);

                using (Reader reader = new Reader(data))
                {
                    int tmp = reader.ReadBigEndian4();
                    UnitTest.Test(tmp == i);
                }
            }
        }
        static void TestReadWriteWriteBytes()
        {
            byte[] src = { 1, 6, 3, 7, 9 };

            byte[] data = null;
            using (Writer writer = new Writer())
            {
                for (int i = 0; i < 5; i++)
                {
                    writer.WriteBytes(src);
                }
                data = writer.OutData;
            }

            using (Reader reader = new Reader(data))
            {
                byte[] dst = new byte[src.Length];
                for (int i = 0; i < 5; i++)
                {
                    reader.ReadBytes(dst, 0, dst.Length, Reader.UnsyncMode.CountExcludesUnsyncBytes);
                    UnitTest.Test(ArrayUtils.IsEqual(dst, src));
                }
            }
        }

        static void TestReaderUnsynchronizationSingleBytes()
        {
            TestReaderUnsynchronization(
                new byte[] { 0xFF, 0, 0xFF, 0x1, 0xFF, 0x0, 0x0, 0xFF, 0x0, 0xFF, 0x0, 0xFF, 0x1 },
                new byte[] { 0xFF, 0xFF, 0x1, 0xFF, 0x0, 0xFF, 0xFF, 0xFF, 0x1 },
                4);

            TestReaderUnsynchronization(
                new byte[] { 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, (byte)'T', },
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, (byte)'T', },
                7);

            TestReaderUnsynchronization(
                new byte[] { 0xFF, 0xA0 },
                new byte[] { 0xFF, 0xA0 },
                0);
        }
        static void TestReaderUnsynchronizationBytes()
        {
            byte[] src = new byte[] { 0xFF, 0, 0xFF, 0x1, 0xFF, 0x0, 0x0 };
            byte[] dst = new byte[] { 0xFF, 0xFF, 0x1, 0xFF, 0x0 };
            byte[] result = new byte[] { 0, 0, 0, 0, 0 };

            using (Reader reader = new Reader(src))
            {
                reader.Unsynchronization = true;

                int numBytes = reader.ReadBytes(result, 0, result.Length, Reader.UnsyncMode.CountExcludesUnsyncBytes);
                UnitTest.Test(reader.UnsynchronizationCounter == 2);
                UnitTest.Test(numBytes == result.Length);
                UnitTest.Test(ArrayUtils.IsEqual(result, dst));
            }

            using (Reader reader = new Reader(src))
            {
                reader.Unsynchronization = true;

                int numBytes = reader.ReadBytes(result, 0, src.Length, Reader.UnsyncMode.CountIncludesUnsyncBytes);
                UnitTest.Test(reader.UnsynchronizationCounter == 2);
                UnitTest.Test(numBytes == result.Length);
                UnitTest.Test(ArrayUtils.IsEqual(result, dst));
            }
        }
        static void TestWriterUnsynchronization()
        {
            TestWriterUnsynchronization(
                new byte[] { 0xFF, 0xFF, 0x1, 0xFF, 0x0, 0xFF, 0xFF, 0xFF, 0x1 },
                new byte[] { 0xFF, 0, 0xFF, 0x1, 0xFF, 0x0, 0x0, 0xFF, 0x0, 0xFF, 0x0, 0xFF, 0x1 },
                4);

            TestWriterUnsynchronization(
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, (byte)'T', },
                new byte[] { 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, 0, 0xFF, (byte)'T', },
                7);

            TestWriterUnsynchronization(
                new byte[] { 0xFF, 0xA0 },
                new byte[] { 0xFF, 0xA0 },
                0);
        }

        static void TestReaderStreamUnsynchronizationBytes()
        {
            byte[] src = new byte[] { 0xFF, 0, 0xFF, 0x1, 0xFF, 0x0, 0x0 };
            byte[] dst = new byte[] { 0xFF, 0xFF, 0x1, 0xFF, 0x0 };
            byte[] result = new byte[] { 0, 0, 0, 0, 0 };

            using (Reader reader = new Reader(src))
            {
                reader.Unsynchronization = true;

                using (ReaderStream stream = new ReaderStream(
                    reader, result.Length, Reader.UnsyncMode.CountExcludesUnsyncBytes))
                {
                    int numBytes = stream.Read(result, 0, result.Length);
                    UnitTest.Test(stream.Position == stream.Length);
                    UnitTest.Test(numBytes == result.Length);
                    UnitTest.Test(ArrayUtils.IsEqual(result, dst));
                }

                reader.Seek(0, SeekOrigin.Begin);

                using (ReaderStream stream = new ReaderStream(
                    reader, src.Length, Reader.UnsyncMode.CountIncludesUnsyncBytes))
                {
                    int numBytes = stream.Read(result, 0, src.Length);
                    UnitTest.Test(stream.Position == stream.Length);
                    UnitTest.Test(numBytes == result.Length);
                    UnitTest.Test(ArrayUtils.IsEqual(result, dst));
                }
            }
        }

        private static byte[] CreateTestArray()
        {
            byte[] arr = new byte[8];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (byte)i;
            }
            return arr;
        }
        private static void TestWriterUnsynchronization(byte[] src, byte[] dst, int unsynchronizationCount)
        {
            byte[] result = null;

            using (Writer writer = new Writer())
            {
                writer.Unsynchronization = true;

                for (int i = 0; i < src.Length; ++i)
                {
                    writer.WriteByte(src[i]);
                }
                UnitTest.Test(writer.UnsynchronizationCounter == unsynchronizationCount);
                result = writer.OutData;
            }

            UnitTest.Test(ArrayUtils.IsEqual(result, dst));
        }
        private static void TestReaderUnsynchronization(byte[] src, byte[] dst, int unsynchronizationCount)
        {
            byte[] result = new byte[dst.Length];

            using (Reader reader = new Reader(src))
            {
                reader.Unsynchronization = true;

                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = reader.ReadByte();
                }
                UnitTest.Test(-1 == reader.PeekChar());
                UnitTest.Test(reader.UnsynchronizationCounter == unsynchronizationCount);
            }

            UnitTest.Test(ArrayUtils.IsEqual(result, dst));
        }
    }
}
