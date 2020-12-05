using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CoreVirtualDrive;

namespace ID3.IO
{
    public class Writer : IDisposable
    {
        private Stream stream;

        private int byte0 = -1;
        private int byte1 = -1;
        private int unsynchronizationOffset = 2;
        
        public Writer()
        {
            Filename = "memory";
            stream = new MemoryStream();
        }
        public Writer(byte[] mem)
        {
            Filename = "memory";
            stream = new MemoryStream();
            stream.Write(mem, 0, mem.Length);
            stream.Seek(0, SeekOrigin.Begin);
        }
        public Writer(FileInfo file)
        {
            Filename = file.FullName;
            stream = VirtualDrive.OpenOutStream(file.FullName);
        }

        public bool Unsynchronization
        {
            get;
            set;
        }
        public string Filename
        {
            get;
            private set;
        }

        public long Length
        {
            get
            {
                return stream.Length;
            }
        }
        public long Position
        {
            get
            {
                return stream.Position - unsynchronizationOffset + 2;
            }
        }
        public long Seek(long offset, SeekOrigin origin)
        {
            Flush();

            long position = Position;

            switch (origin)
            {
                case System.IO.SeekOrigin.Begin: position = offset; break;
                case System.IO.SeekOrigin.Current: position = Position + offset; break;
                case System.IO.SeekOrigin.End: position = Length + offset; break;
            }

            if (position < 0 || position > stream.Length)
            {
                throw new IndexOutOfRangeException("Seek to " + position + " with stream of length " + Length);
            }

            stream.Seek(position, System.IO.SeekOrigin.Begin);

            UnsynchronizationCounter = 0;

            return Position;
        }
        public void Flush()
        {
            if (byte1 != -1)
            {
                stream.WriteByte((byte)byte1);
                byte1 = -1;
            }

            if (byte0 != -1)
            {
                stream.WriteByte((byte)byte0);
                byte0 = -1;
            }

            unsynchronizationOffset = 2;
        }

        public void Close()
        {
            Flush();
            stream.Close();
        }
        public void Dispose()
        {
            Close();
        }

        public void WriteByte(byte b)
        {
            WriteSingleByte(b);
        }
        public void WriteBigEndian2(int value)
        {
            WriteByte((byte)((value & 0xFF00) >> 8));
            WriteByte((byte)((value & 0x00FF) >> 0));
        }
        public void WriteBigEndian3(int value)
        {
            WriteByte((byte)((value & 0xFF0000) >> 16));
            WriteByte((byte)((value & 0x00FF00) >> 8));
            WriteByte((byte)((value & 0x0000FF) >> 0));
        }
        public void WriteBigEndian4(int value)
        {
            WriteByte((byte)((value & 0xFF000000) >> 24));
            WriteByte((byte)((value & 0x00FF0000) >> 16));
            WriteByte((byte)((value & 0x0000FF00) >> 8));
            WriteByte((byte)((value & 0x000000FF) >> 0));
        }
        public void WriteBigEndian4HighestBitZero(int value)
        {
            byte[] tmp = Utils.BigEndian4HighestBitZeroToRaw(value);
            WriteByte(tmp[0]);
            WriteByte(tmp[1]);
            WriteByte(tmp[2]);
            WriteByte(tmp[3]);
        }
        public void WriteBytes(byte[] values)
        {
            if (Unsynchronization)
            {
                for (int i = 0; i < values.Length; ++i)
                {
                    WriteByte(values[i]);
                }
            }
            else
            {
                Flush();
                stream.Write(values, 0, values.Length);
            }
        }
        public void WriteString(string text, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (i < text.Length)
                {
                    WriteByte((byte)text[i]);
                }
                else
                {
                    WriteByte(0);
                }
            }
        }

        private void WriteSingleByte(byte b)
        {
            if (unsynchronizationOffset == 0)
            {
                stream.WriteByte((byte)byte1);
            }
            else
            {
                unsynchronizationOffset--;
            }

            byte1 = byte0;
            byte0 = b;

            if (Unsynchronization
                && (byte1 == 0xFF && byte0 == 0x00
                 || byte1 == 0xFF && (byte0 & 0xE0) == 0xE0))
            {
                stream.WriteByte((byte)byte1);

                byte1 = 0x00; // Insert 0x00 here

                UnsynchronizationCounter++;
            }
        }

        public int UnsynchronizationCounter
        {
            get;
            private set;
        }
        public byte[] OutData
        {
            get
            {
                if (!(stream is MemoryStream))
                {
                    throw new Exception("Only available for memory streams");
                }

                Flush();

                byte[] result = new byte[Length];
                Array.Copy((stream as MemoryStream).GetBuffer(), result, Length);

                return result;
            }
        }
    }

    public class WriterUnsynchronizationHelper : IDisposable
    {
        public WriterUnsynchronizationHelper(Writer writer, bool desynchronize)
        {
            Writer = writer;

            Desynchronization = Writer.Unsynchronization;
            Writer.Unsynchronization = desynchronize;
        }

        public void Dispose()
        {
            Writer.Unsynchronization = Desynchronization;
        }

        private bool Desynchronization
        {
            get;
            set;
        }
        private Writer Writer
        {
            get;
            set;
        }
    }
}
