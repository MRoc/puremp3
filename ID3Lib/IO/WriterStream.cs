using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ID3.IO
{
    public class WriterStream : Stream
    {
        private Writer writer = null;

        public WriterStream(
            Writer _writer)
        {
            writer = _writer;
        }

        public override bool CanRead
        {
            get
            {
                return false;
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
                return true;
            }
        }
        public override long Length
        {
            get
            {
                throw new Exception("Not supported");
            }
        }
        public override long Position
        {
            get
            {
                throw new Exception("Not supported");
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
            throw new Exception("Not supported");
        }
        public override int ReadByte()
        {
            throw new Exception("Not supported");
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
            if (offset != 0 || count != buffer.Length)
            {
                throw new NotSupportedException("Write failed with offset=" + offset);
            }

            writer.WriteBytes(buffer);
        }
        public override void WriteByte(byte value)
        {
            writer.WriteByte(value);
        }
    }
}
