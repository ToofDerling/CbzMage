using System;
using System.IO;

namespace PdfConverter.ManagedBuffers
{
    public class ManagedBuffer
    {
        internal static BufferCache Cache { get; set; }

        private readonly int _originalLength;

        public byte[] Buffer { get; private set; }

        public int Count { get; private set; }

        public ManagedBuffer()
        {
            Buffer = Cache.Get();

            _originalLength = Buffer.Length;
        }

        public ManagedBuffer(ManagedBuffer startWith, int offset, int length) : this()
        {
            Array.Copy(startWith.Buffer, offset, Buffer, 0, length);

            Count = length;
        }

        public int ReadFrom(Stream stream)
        {
            var remaining = Buffer.Length - Count;
            var readCount = stream.Read(Buffer, Count, remaining);

            if (readCount > 0)
            {
                Count += readCount;

                if (remaining < (Buffer.Length / 10))
                {
                    var newBuffer = new byte[Buffer.Length + _originalLength];

                    Array.Copy(Buffer, 0, newBuffer, 0, Count);
                    Buffer = newBuffer;
                }
            }

            return readCount;
        }
        public void Release()
        {
            if (Cache != null)
            {
                Cache.Release(Buffer);
            }
            else
            {
                Buffer = null;
            }
        }

        public bool StartsWith(int offset, int count, byte[] pattern)
        {
            if (count < pattern.Length)
            {
                return false;
            }

            for (int i = 0, sz = pattern.Length; i < sz; i++)
            {
                if (Buffer[offset + i] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
