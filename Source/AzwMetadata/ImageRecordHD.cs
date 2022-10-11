namespace AzwMetadata
{
    public class ImageRecordHD : PageRecord
    {
        public ImageRecordHD(Stream stream, long pos, uint len) : base(stream, pos, len)
        { }

        public override Span<byte> ReadData()
        {
            // Take into account that IsCresRecord has moved the position and
            // the length between the CRES marker and the start of the HD image.
            _stream.Position += 8;
            return _reader.ReadBytes(_len - 12);
        }
    }
}
