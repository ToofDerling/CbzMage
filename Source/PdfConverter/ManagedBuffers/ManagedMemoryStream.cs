namespace PdfConverter.ManagedBuffers
{
    public class ManagedMemoryStream : MemoryStream
    {
        internal static BufferCache Cache { get; set; }

        public static byte[] ManagedBuffer()
        {
            return Cache.Get();
        }

        private readonly byte[] _buffer;

        public ManagedMemoryStream(byte[] buffer) : base(buffer)
        {
            _buffer = buffer;
            SetLength(0);
        }

        public override byte[] GetBuffer()
        {
            return _buffer;
        }

        public void Release()
        {
            Cache.Release(_buffer);
        }
    }
}
