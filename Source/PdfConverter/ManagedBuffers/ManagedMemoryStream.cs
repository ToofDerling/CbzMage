namespace PdfConverter.ManagedBuffers
{
    public sealed class ManagedMemoryStream : MemoryStream
    {
        internal static BufferCache Cache { get; set; }

        public static byte[] ManagedBuffer()
        {
            return Cache.Get();
        }

        private readonly byte[]? _buffer;

        public ManagedMemoryStream(int size) : base(size)
        {
            _buffer = null;
        }

        public ManagedMemoryStream(byte[] buffer) : base(buffer)
        {
            _buffer = buffer;
            SetLength(0);
        }

        public override byte[] GetBuffer()
        {
            if (_buffer == null)
            { 
                return base.GetBuffer();
            }
            return _buffer;
        }

        public void Release()
        {
            if (_buffer != null)
            {
                Cache.Release(_buffer);
            }
        }
    }
}
