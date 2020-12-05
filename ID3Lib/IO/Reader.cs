using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CoreVirtualDrive;
using CoreLogging;

namespace ID3.IO
{
    public class Reader : IDisposable
    {
        private Stream stream;

        private int byte0;
        private int byte1;
        private long length;
        private long position;

        public Reader(byte[] array)
        {
            Filename = "memory";
            stream = new MemoryStream(array);
            length = array.Length;

            SetupReader();
        }
        public Reader(FileInfo file)
        {
            Filename = file.FullName;
            stream = VirtualDrive.OpenInStream(file.FullName);
            length = stream.Length;

            SetupReader();
        }

        public long Length
        {
            get
            {
                return length;
            }
        }
        public long Position
        {
            get
            {
                if (Unsynchronization)
                {
                    if (position == Length)
                    {
                        if (byte0 == -1 && byte1 == -1)
                        {
                            return position;
                        }
                        else if (byte0 != -1 && byte1 == -1)
                        {
                            return position - 1;
                        }
                    }

                    return position - 2;
                }
                else
                {
                    return position;
                }
            }
        }
        public long Seek(long offset, SeekOrigin origin)
        {
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
            this.position = position;

            SetupReader();

            return Position;
        }

        public void Close()
        {
            stream.Close();
        }
        public void Dispose()
        {
            stream.Close();
        }

        public bool ThrowExceptions
        {
            get;
            set;
        }
        public void HandleException(Exception e)
        {
            if (ThrowExceptions)
            {
                throw e;
            }
            else
            {
                Logger.WriteLine(Tokens.Exception, Filename);
                Logger.WriteLine(Tokens.Exception, e);
            }
        }

        public bool Unsynchronization
        {
            get
            {
                return unsynchronization;
            }
            set
            {
                if (unsynchronization != value)
                {
                    long pos = Position;
                    unsynchronization = value;
                    Seek(pos, SeekOrigin.Begin);
                }
            }
        }
        private bool unsynchronization;
        public int UnsynchronizationCounter
        {
            get;
            private set;
        }
        public string Filename
        {
            get;
            private set;
        }

        public byte ReadByte()
        {
            if (Unsynchronization)
            {
                if (byte0 == 0xFF && byte1 == 0x00)
                {
                    byte1 = PrivateReadByte();
                    UnsynchronizationCounter++;
                }

                byte result = (byte)byte0;

                byte0 = byte1;
                byte1 = PrivateReadByte();

                return result;
            }
            else
            {
                return (byte)PrivateReadByte();
            }
        }
        public int ReadBigEndian2()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();

            return (b0 << 8) | b1;
        }
        public int ReadBigEndian3()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();
            byte b2 = ReadByte();

            return
                  (b0 << 16)
                | (b1 << 8)
                | (b2 << 0);
        }
        public int ReadBigEndian4()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();
            byte b2 = ReadByte();
            byte b3 = ReadByte();

            return
                  (b0 << 24)
                | (b1 << 16)
                | (b2 << 8)
                | (b3 << 0);
        }
        public int ReadBigEndian4HighestBitZero()
        {
            byte[] tmp = new byte[4];

            tmp[0] = ReadByte();
            tmp[1] = ReadByte();
            tmp[2] = ReadByte();
            tmp[3] = ReadByte();

            return Utils.RawToBigEndian4HighestBitZero(tmp);
        }

        public int PeekChar()
        {
            if (Unsynchronization)
            {
                return byte0;
            }
            else
            {
                int result = stream.ReadByte();
                stream.Seek(-1, SeekOrigin.Current);
                return result;
            }
        }

        public enum UnsyncMode
        {
            // Read count bytes exluding unsync
            CountExcludesUnsyncBytes,
            // Reat count bytes including unsync
            CountIncludesUnsyncBytes
        }
        public int ReadBytes(byte[] buffer, int offset, int count, UnsyncMode unsyncMode)
        {
            int bytesRead = 0;

            if (!Unsynchronization)
            {
                bytesRead = stream.Read(buffer, offset, count);
                position += bytesRead;
            }
            else
            {
                int i = 0;

                if (unsyncMode == UnsyncMode.CountExcludesUnsyncBytes)
                {
                    for (; i < count; ++i)
                    {
                        buffer[offset + i] = ReadByte();
                    }
                }
                else if (unsyncMode == UnsyncMode.CountIncludesUnsyncBytes)
                {
                    int unsyncBefore = UnsynchronizationCounter;
                    for (; i + (UnsynchronizationCounter - unsyncBefore) < count; i++)
                    {
                        buffer[offset + i] = ReadByte();
                    }
                }
                else
                {
                    throw new NotSupportedException("Unknown unsync mode");
                }

                bytesRead = i;
            }

            return bytesRead;
        }
        public string ReadString(int length)
        {
            StringBuilder sb = new StringBuilder();

            bool valid = true;

            for (int i = 0; i < length; ++i)
            {
                byte b = ReadByte();

                if (b != 0 && valid)
                {
                    sb.Append((char)b);
                }
                else
                {
                    valid = false;
                }
            }

            return sb.ToString();
        }

        private bool BytesAvailable()
        {
            return Position < Length;
        }
        private int PrivateReadByte()
        {
            if (BytesAvailable())
            {
                position++;
                return stream.ReadByte();
            }
            else
            {
                return -1;
            }
        }
        private void SetupReader()
        {
            if (Unsynchronization)
            {
                byte0 = PrivateReadByte();
                byte1 = PrivateReadByte();
            }
        }
    }

    public class ReaderUnsynchronizationHelper : IDisposable
    {
        public ReaderUnsynchronizationHelper(Reader reader, bool desynchronize)
        {
            Reader = reader;

            Desynchronization = Reader.Unsynchronization;
            Reader.Unsynchronization = desynchronize;
        }

        public void Dispose()
        {
            Reader.Unsynchronization = Desynchronization;
        }

        private bool Desynchronization
        {
            get;
            set;
        }
        private Reader Reader
        {
            get;
            set;
        }
    }
}
