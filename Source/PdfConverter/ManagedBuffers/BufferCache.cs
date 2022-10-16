namespace PdfConverter.ManagedBuffers
{
    public class BufferCache : Cache<byte[]>
    {
        public BufferCache(int size) : base(size)
        {
            ManagedBuffer.Cache = this;
            ManagedMemoryStream.Cache = this;
        }

        protected override byte[] CreateNew()
        {
            return new byte[_size];
        }
    }
}
