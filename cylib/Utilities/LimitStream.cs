using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace cylib
{
    /// <summary>
    /// A special type of stream that wraps an underlying stream, but only allows a certain portion of the stream to be read.
    /// Useful for giving to apis that expect to consume an entire stream.
    /// 
    /// Does not support 'SetLength' -- maybe we should just turn off support for Writing in general.
    /// </summary>
    class LimitStream : Stream
    {
        private Stream baseStream;

        /// <summary>
        /// position of the underlying stream that we represent as the start
        /// </summary>
        private long pos;

        /// <summary>
        /// length of the underlying stream we represent
        /// </summary>
        private long len;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseStream">The underyling stream</param>
        public LimitStream(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        public void SetLimits(long pos, long len)
        {
            if (pos < 0 || pos > baseStream.Length)
                throw new ArgumentException("Setting an invalid position limit in LimitStream");
            if (len < 0 || pos + len > baseStream.Length)
                throw new ArgumentException("Setting an invalid length limit in LimitStream");

            this.pos = pos;
            this.len = len;

            baseStream.Seek(pos, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get
            {
                return baseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return baseStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return baseStream.CanWrite;
            }
        }

        /// <summary>
        /// Total length of the stream.
        /// </summary>
        public override long Length
        {
            get
            {
                return len;
            }
        }

        /// <summary>
        /// Current position.
        /// </summary>
        public override long Position
        {
            get
            {
                long toReturn = baseStream.Position - pos;

                //do some error checking here
                if (toReturn < 0)
                    throw new InvalidDataException("Base stream position is somehow less than the starting position? " + toReturn);
                if (toReturn > len)
                    throw new InvalidDataException("Base stream position is somehow greater than starting position + len? " + toReturn);

                return toReturn;
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Setting a LimitStream position to less than zero: " + value);
                if (value > len) //setting to len exactly is fine, that just means end-of-stream
                    throw new ArgumentOutOfRangeException("Setting a LimitStream position to greater than len: " + value);

                baseStream.Position = value + pos;
            }
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count">Maximum number of bytes</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            //we need to limit the length we pass in to the base stream, so we don't read past length
            int c = (int)Math.Min(len - Position, count);

            return baseStream.Read(buffer, offset, c);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long toSeek = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    toSeek = pos;
                    break;
                case SeekOrigin.Current:
                    toSeek = baseStream.Position;
                    break;
                case SeekOrigin.End: //how does this even work? can you pass in a negative offset? (yes)
                    toSeek = pos + len;
                    break;
            }

            //seeking beyond the end of stream is allowed, but we want to limit the seek we actually pass through
            toSeek = Math.Max(Math.Min(toSeek + offset, pos + len), pos);

            return baseStream.Seek(toSeek, SeekOrigin.Begin);

        }

        /// <summary>
        /// This is supposed to set the total length of the stream.
        /// If value is less than the current length, the stream becomes truncated.
        /// If value is larger than the current length, the stream is expanded.
        /// 
        /// We would have to insert data into the base stream, or delete data from it, which could be very costly if we're in the middle of a 100's of MB blob.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("LimitStream doesn't support SetLength");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //throw an argument exception if this tries to write past the end of our stream
            if (Position + count > len)
                throw new ArgumentException("Trying to write past the end of a limit stream");

            baseStream.Write(buffer, offset, count);
        }
    }
}
