using System.Text;

namespace AzwMetadata
{
    public class PageRecord
    {
        protected readonly Stream _stream;
        protected readonly long _pos;
        protected readonly int _len;

        protected readonly BinaryReader _reader;

        public string PageRef { get; set; }

        public PageRecord(Stream stream, long pos, uint len)
        {
            _stream = stream;
            _pos = pos;
            _len = (int)len;
            _reader = new BinaryReader(stream);
        }

        public bool TryGetRescRecord(out RescRecord rescRecord)
        {
            rescRecord = null;

            _stream.Position = _pos;

            if (!IsRecordId("RESC", 4))
            {
                return false;
            }

            rescRecord = new(_stream, _pos, (uint)_len);

            return true;
        }

        public bool IsLen1992Record()
        {
            return _len == 1992;
        }

        public bool IsDatpRecord()
        {
            _stream.Position = _pos;

            return IsRecordId("DATP", 4);
        }

        public bool IsCresRecord()
        {
            _stream.Position = _pos;

            return IsRecordId("CRES", 4);
        }

        protected bool IsRecordId(string id, int peekLen)
        {
            var bytes = _reader.ReadBytes(peekLen);

            return Encoding.ASCII.GetString(bytes).Contains(id);
        }

        public virtual Span<byte> ReadData()
        {
            _stream.Position = _pos;

            return _reader.ReadBytes(_len);
        }
    }
}
