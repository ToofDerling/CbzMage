using System.Buffers;

namespace CbzMage.Shared.ManagedBuffers
{
    public sealed class BufferCache : Cache<byte[]>
    {
        private readonly int _size;

        public int BufferRemainingThreshold { get; }

        public BufferCache(int size, int bufferRemainingThreshold)
        {
            _size = size;

            BufferRemainingThreshold = bufferRemainingThreshold;

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

        /// <summary>
        /// This method does nothing.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            //NOP
        }
    }
}
