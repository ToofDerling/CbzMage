namespace AzwMetadata
{
    public class ImageRecordHD : PageRecord
    {
        public ImageRecordHD(Stream stream, long pos, uint len) : base(stream, pos, len)
        {
            _stream.Position = pos;

            IsCresRecord = IsRecordId("CRES");
        }

        public override Span<byte> ReadData()
        {
            // Take into account the length between the CRES marker
            // and the start of the HD image.
            if (IsCresRecord)
            {
                _stream.Position = _pos + 12;
                return _reader.ReadBytes(_len - 12);
            }

            _stream.Position = _pos;
            return _reader.ReadBytes(_len);
        }
    }
}
