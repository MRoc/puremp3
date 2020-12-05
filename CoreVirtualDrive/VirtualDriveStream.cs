using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CoreVirtualDrive
{
    public class VirtualDriveStream : Stream
    {
        public delegate void StreamClosingCallback(Stream stream);

        private Stream internalStream;
        private StreamClosingCallback streamClosingCallback;

        public VirtualDriveStream(Stream stream, StreamClosingCallback streamClosingCallback)
        {
            this.internalStream = stream;
            this.streamClosingCallback = streamClosingCallback;
        }

        public override bool CanRead
        {
            get
            {
                return internalStream.CanRead;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return internalStream.CanSeek;
            }
        }
        public override bool CanTimeout
        {
            get
            {
                return internalStream.CanTimeout;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return internalStream.CanWrite;
            }
        }
        public override long Length
        {
            get
            {
                return internalStream.Length;
            }
        }
        public override long Position
        {
            get
            {
                return internalStream.Position;
            }
            set
            {
                internalStream.Position = value;
            }
        }
        public override int ReadTimeout
        {
            get
            {
                return internalStream.ReadTimeout;
            }
            set
            {
                internalStream.ReadTimeout = value;
            }
        }
        public override int WriteTimeout
        {
            get
            {
                return internalStream.WriteTimeout;
            }
            set
            {
                internalStream.WriteTimeout = value;
            }
        }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return internalStream.BeginRead(buffer, offset, count, callback, state);
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return internalStream.BeginWrite(buffer, offset, count, callback, state);
        }
        public override void Close()
        {
            if (!IsClosed)
            {
                IsClosed = true;

                internalStream.Close();
                streamClosingCallback(this);
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            internalStream.Dispose();
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            return internalStream.EndRead(asyncResult);
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            internalStream.EndWrite(asyncResult);
        }
        public override void Flush()
        {
            internalStream.Flush();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return internalStream.Read(buffer, offset, count);
        }
        public override int ReadByte()
        {
            return internalStream.ReadByte();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return internalStream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            internalStream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            internalStream.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            internalStream.WriteByte(value);
        }

        public bool IsClosed
        {
            get;
            private set;
        }
    }
}
