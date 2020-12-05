using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using ID3.Utils;
using CoreUtils;
using CoreTest;
using ID3.IO;

namespace ID3.Codec
{
    public abstract class TextCodec
    {
        public abstract string ReadString(Stream stream, bool terminating);
        public abstract void WriteString(Stream stream, string text);
        public abstract void WriteDelimiter(Stream stream);

        public virtual IEnumerable<string> ReadStrings(Stream stream)
        {
            List<string> result = new List<string>();

            while (stream.Position < stream.Length)
            {
                result.Add(ReadString(stream, false));
            }

            return result;
        }

        public virtual void WriteStrings(Stream stream, List<string> texts)
        {
            for (int i = 0; i < texts.Count; ++i)
            {
                if (i != 0)
                {
                    WriteDelimiter(stream);
                }
                WriteString(stream, texts[i]);
            }
        }

        public abstract TextCodec Clone();
    }
    public class TextEncoderISO_8859_1 : TextCodec
    {
        public override string ReadString(Stream stream, bool terminating)
        {
            StringBuilder sb = new StringBuilder();

            if (terminating
                && stream is ReaderStream
                && !(stream as ReaderStream).Unsynchronization)
            {
                byte[] data = new byte[stream.Length - stream.Position];
                stream.Read(data, 0, data.Length);

                foreach (byte b0 in data)
                {
                    if (b0 != 0)
                    {
                        sb.Append((char)b0);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                while (stream.Position < stream.Length)
                {
                    byte b0 = (byte)stream.ReadByte();

                    if (b0 != 0)
                    {
                        sb.Append((char)b0);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return sb.ToString();
        }
        public string ReadStringFixedLength(Stream stream, int length)
        {
            StringBuilder sb = new StringBuilder();

            int count = 0;
            while (stream.Position < stream.Length && count < length)
            {
                byte b0 = (byte)stream.ReadByte();

                if (b0 != 0)
                {
                    sb.Append((char)b0);
                }

                count++;
            }

            return sb.ToString();
        }

        public override void WriteString(Stream stream, string text)
        {
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                stream.WriteByte((byte)(chars[i] & 0xFF));
            }
        }
        public void WriteStringFixedLength(Stream stream, string text, int length)
        {
            char[] chars = text.ToCharArray();
            for (int i = 0; i < length; i++)
            {
                if (i < chars.Length)
                {
                    stream.WriteByte((byte)(chars[i] & 0xFF));
                }
                else
                {
                    stream.WriteByte(0);
                }
            }
        }
        public override void WriteDelimiter(Stream stream)
        {
            stream.WriteByte((byte)0);
        }

        public override TextCodec Clone()
        {
            return Instance;
        }

        private static TextEncoderISO_8859_1 instance = new TextEncoderISO_8859_1();
        public static TextEncoderISO_8859_1 Instance
        {
            get
            {
                return instance;
            }
        }

        private static ASCIIEncoding asciiEncoder = new ASCIIEncoding();
    }
    public class TextEncoderUnicodeUcs2 : TextCodec
    {
        // UCS-2 is similar to UTF16: http://www.unicode.org/faq/basic_q.html#14

        public enum ByteOrder
        {
            BigEndian,
            LittleEndian,
            Invalid
        }

        private static int SIZE_OF_CHAR = 2;

        private ByteOrder byteOrder = ByteOrder.LittleEndian;

        public TextEncoderUnicodeUcs2()
        {
        }
        public TextEncoderUnicodeUcs2(ByteOrder byteOrder)
        {
            this.byteOrder = byteOrder;
        }

        public override string ReadString(Stream stream, bool terminating)
        {
            if (StreamMayHaveCharsLeftToRead(stream))
            {
                byte byte0 = (byte)stream.ReadByte();
                byte byte1 = (byte)stream.ReadByte();

                if (byte0 == 0 && byte1 == 0)
                {
                    return "";
                }
                else
                {
                    byteOrder = IsBom(byte0, byte1);
                    return ReadString(stream, byteOrder, terminating);
                }
            }

            return "";
        }
        public override void WriteString(Stream stream, string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                WriteBom(stream, byteOrder);
                WriteString(stream, text, byteOrder);
            }
        }
        public override void WriteDelimiter(Stream stream)
        {
            stream.WriteByte((byte)0);
            stream.WriteByte((byte)0);
        }
        public override IEnumerable<string> ReadStrings(Stream stream)
        {
            List<string> result = new List<string>();

            while (StreamMayHaveCharsLeftToRead(stream))
            {
                result.Add(ReadString(stream, false));
            }

            return result;
        }
        public override void WriteStrings(Stream stream, List<string> texts)
        {
            for (int i = 0; i < texts.Count; ++i)
            {
                if (i != 0)
                {
                    WriteDelimiter(stream);
                }
                WriteString(stream, texts[i]);
            }
        }
        private ByteOrder IsBom(byte b0, byte b1)
        {
            if (b0 == 0xFF && b1 == 0xFE)
            {
                return ByteOrder.LittleEndian;
            }
            else if (b0 == 0xFE && b1 == 0xFF)
            {
                return ByteOrder.BigEndian;
            }
            else
            {
                throw new TextCodecException("Wrong byte order marker");
            }
        }
        private void WriteBom(Stream stream, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.BigEndian)
            {
                stream.WriteByte((byte)0xFE);
                stream.WriteByte((byte)0xFF);
            }
            else if (byteOrder == ByteOrder.LittleEndian)
            {
                stream.WriteByte((byte)0xFF);
                stream.WriteByte((byte)0xFE);
            }
            else
            {
                throw new Exception("Invalid byteorder");
            }
        }
        private void WriteString(Stream stream, string text, ByteOrder byteOrder)
        {
            char[] chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (byteOrder == ByteOrder.BigEndian)
                {
                    stream.WriteByte((byte)((chars[i] & 0xFF00) >> 8));
                    stream.WriteByte((byte)((chars[i] & 0x00FF) >> 0));
                }
                else if (byteOrder == ByteOrder.LittleEndian)
                {
                    stream.WriteByte((byte)((chars[i] & 0x00FF) >> 0));
                    stream.WriteByte((byte)((chars[i] & 0xFF00) >> 8));
                }
            }
        }
        private string ReadString(Stream stream, ByteOrder byteOrder, bool terminating)
        {
            StringBuilder sb = new StringBuilder();

            if (terminating
                && stream is ReaderStream
                && !(stream as ReaderStream).Unsynchronization)
            {
                byte[] data = new byte[stream.Length - stream.Position];
                stream.Read(data, 0, data.Length);

                int stringLength = data.Length / SIZE_OF_CHAR;

                for (int i = 0; i < stringLength; i++)
                {
                    byte b0 = data[2 * i + 0];
                    byte b1 = data[2 * i + 1];

                    if (b0 != 0 || b1 != 0)
                    {
                        if (byteOrder == ByteOrder.BigEndian)
                        {
                            sb.Append((char)((b0 << 8) | (b1 << 0)));
                        }
                        else if (byteOrder == ByteOrder.LittleEndian)
                        {
                            sb.Append((char)((b0 << 0) | (b1 << 8)));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                int stringLength = (int)((stream.Length - stream.Position) / SIZE_OF_CHAR);

                for (int i = 0; i < stringLength; i++)
                {
                    byte b0 = (byte)stream.ReadByte();
                    byte b1 = (byte)stream.ReadByte();

                    if (b0 != 0 || b1 != 0)
                    {
                        if (byteOrder == ByteOrder.BigEndian)
                        {
                            sb.Append((char)((b0 << 8) | (b1 << 0)));
                        }
                        else if (byteOrder == ByteOrder.LittleEndian)
                        {
                            sb.Append((char)((b0 << 0) | (b1 << 8)));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return sb.ToString();
        }
        private bool StreamMayHaveCharsLeftToRead(Stream stream)
        {
            return (stream.Length - stream.Position) / SIZE_OF_CHAR > 0;
        }

        public override TextCodec Clone()
        {
            return new TextEncoderUnicodeUcs2(byteOrder);
        }

        public override string ToString()
        {
            return base.ToString() + " " + byteOrder;
        }
    }
    public class TextEncoderUnicodeUtf8 : TextCodec
    {
        public override string ReadString(Stream stream, bool terminating)
        {
            byte[] buffer = new byte[stream.Length - stream.Position];

            int count = 0;
            int value = 0;
            while (stream.Position < stream.Length && (value = stream.ReadByte()) != 0)
            {
                buffer[count] = (byte)value;
                count++;
            }

            return System.Text.Encoding.UTF8.GetString(buffer, 0, count);
        }
        public override void WriteString(Stream stream, string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
        public override void WriteDelimiter(Stream stream)
        {
            stream.WriteByte((byte)0);
        }

        public override TextCodec Clone()
        {
            return instance;
        }

        public static TextEncoderUnicodeUtf8 instance = new TextEncoderUnicodeUtf8();
    }

    public class TestTextEncoders
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTextEncoders));
        }

        private static void TestTestReadWriteISO_8859_1()
        {
            TestEncodeEqualsDecode(new ID3.Codec.TextEncoderISO_8859_1(), "Hello World");
        }
        private static void TestReadWriteUnicodeUcs2()
        {
            TestEncodeEqualsDecode(new ID3.Codec.TextEncoderUnicodeUcs2(), "Hello World");
        }
        private static void TestReadWriteUnicodeUtf8()
        {
            TestEncodeEqualsDecode(new ID3.Codec.TextEncoderUnicodeUtf8(), "Hello World");
        }
        private static void TestEncodeEqualsDecode(TextCodec encoder, string text)
        {
            MemoryStream wstream = new MemoryStream();
            encoder.WriteString(wstream, text);

            MemoryStream rstream = new MemoryStream(wstream.ToArray());
            string text2 = encoder.ReadString(rstream, false);

            UnitTest.Test(text.Equals(text2));
        }

        private static void TestWriteISO_8859_1()
        {
            string text = "A";

            MemoryStream stream = new MemoryStream();

            TextCodec codec = new TextEncoderISO_8859_1();
            codec.WriteString(stream, text);

            UnitTest.Test(stream.Length == 1);
            UnitTest.Test(stream.GetBuffer()[0] == (byte)'A');
        }
        private static void TestWriteUtf8()
        {
            string text = "A";

            MemoryStream stream = new MemoryStream();

            TextCodec codec = new TextEncoderUnicodeUtf8();
            codec.WriteString(stream, text);

            UnitTest.Test(stream.Length == 1);
            UnitTest.Test(stream.GetBuffer()[0] == (byte)'A');
        }
        private static void TestWriteUcs2LittleEndian()
        {
            string text = "A";

            MemoryStream stream = new MemoryStream();

            TextCodec codec = new TextEncoderUnicodeUcs2(
                TextEncoderUnicodeUcs2.ByteOrder.LittleEndian);

            codec.WriteString(stream, text);

            byte[] dst = { 0xFF, 0xFE, (byte)'A', 0 };
            UnitTest.Test(stream.GetBuffer().StartsWith(dst));
        }
        private static void TestWriteUcs2BigEndian()
        {
            string text = "A";

            MemoryStream stream = new MemoryStream();

            TextCodec codec = new TextEncoderUnicodeUcs2(
                TextEncoderUnicodeUcs2.ByteOrder.BigEndian);

            codec.WriteString(stream, text);

            byte[] dst = { 0xFE, 0xFF, 0, (byte)'A' };
            UnitTest.Test(stream.GetBuffer().StartsWith(dst));
        }

        private static void TestReadNullTerminatedISO_8859_1()
        {
            TestReadString(
                new byte[] { (byte)'A', 0, (byte)'B' },
                new TextEncoderISO_8859_1());
        }
        private static void TestReadNullTerminatedUtf8()
        {
            TestReadString(
                new byte[] { (byte)'A', 0, (byte)'B' },
                new TextEncoderUnicodeUtf8());
        }
        private static void TestReadNullTerminatedUcs2()
        {
            TestReadString(
                new byte[]
                {
                    0xFF, 0xFE, (byte)'A', 0,         0, 0,
                    0xFE, 0xFF, 0,         (byte)'B', 0, 0
                },
                new TextEncoderUnicodeUcs2());
        }

        private static void TestReadString(byte[] buffer, TextCodec codec)
        {
            TestReadStringNullTerminated(buffer, codec);
            TestReadStrings(buffer, codec);
        }
        private static void TestReadStringNullTerminated(byte[] buffer, TextCodec codec)
        {
            string result0 = null;
            string result1 = null;
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                result0 = codec.ReadString(stream, false);
                result1 = codec.ReadString(stream, false);
            }
            UnitTest.Test(result0 == "A");
            UnitTest.Test(result1 == "B");
        }
        private static void TestReadStrings(byte[] buffer, TextCodec codec)
        {
            IEnumerable<string> result = null;
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                result = codec.ReadStrings(stream);
            }
            UnitTest.Test(result.Count() == 2);
            UnitTest.Test(result.ElementAt(0) == "A");
            UnitTest.Test(result.ElementAt(1) == "B");
        }
    }
}
