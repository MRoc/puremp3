using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ID3.IO
{
    public class ReaderStream : Stream
    {
        private Reader reader;
        private long length;
        private long position;
        private long originalReaderPosition;

        public ReaderStream(
            Reader _reader,
            int _length,
            Reader.UnsyncMode unsyncMode)
        {
            if (_reader.Length - _reader.Position < _length)
            {
                throw new Exception("Can't setup reader of size " + _length
                    + " on reader with " + (_reader.Length - _reader.Position) + " bytes left");
            }

            reader = _reader;
            length = _length;
            originalReaderPosition = _reader.Position;
            UnsyncMode = unsyncMode;
        }

        public bool Unsynchronization
        {
            get
            {
                return reader.Unsynchronization;
            }
        }

        public long NumBytesLeft()
        {
            return Length - Position;
        }

        public void SeekToStreamEnd()
        {
            while (Position < Length)
            {
                ReadByte();
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
        public override bool CanTimeout
        {
            get
            {
                return false;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
        public override long Length
        {
            get
            {
                return length;
            }
        }
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                throw new Exception("Not supported");
            }
        }
        public override void Close()
        {
        }
        public override void Flush()
        {
            throw new Exception("Not supported");
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > NumBytesLeft())
            {
                throw new Exception("Can not read " + count + " bytes from stream having only " + NumBytesLeft() + " left!");
            }

            int desync0 = reader.UnsynchronizationCounter;
            int bytesRead = reader.ReadBytes(buffer, offset, count, UnsyncMode);
            int desync1 = reader.UnsynchronizationCounter;

            position += bytesRead;

            if (UnsyncMode == Reader.UnsyncMode.CountIncludesUnsyncBytes)
            {
                position += (desync1 - desync0);
            }
            
            return bytesRead;
        }
        public override int ReadByte()
        {
            if (Position >= Length)
            {
                throw new Exception("Index out of bounds");
            }

            if (Unsynchronization)
            {
                int desync0 = reader.UnsynchronizationCounter;
                int result = reader.ReadByte();
                int desync1 = reader.UnsynchronizationCounter;

                position++;

                if (UnsyncMode == Reader.UnsyncMode.CountIncludesUnsyncBytes)
                {
                    position += (desync1 - desync0);
                }

                return result;
            }
            else
            {
                position++;
                return reader.ReadByte();
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new Exception("Not supported");
        }
        public override void SetLength(long value)
        {
            throw new Exception("Not supported");
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("Not supported");
        }
        public override void WriteByte(byte value)
        {
            throw new Exception("Not supported");
        }

        private Reader.UnsyncMode UnsyncMode
        {
            get;
            set;
        }
    }
}
