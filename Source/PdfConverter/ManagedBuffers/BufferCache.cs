using System.Buffers;

namespace PdfConverter.ManagedBuffers
{
    public sealed class BufferCache : Cache<byte[]>
    {
        private readonly int _size;

        public BufferCache(int size)
        {
            _size = size;

            ManagedBuffer.Cache = this;
            ManagedMemoryStream.Cache = this;
        }

        public override byte[] Get()
        {
            return Get(_size);
        }

        public byte[] Get(int size)
        {
            return ArrayPool<byte>.Shared.Rent(size);
        }

        public override void Release(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        protected override byte[] CreateNew()
        {
            return Get();
        }
    }
}
