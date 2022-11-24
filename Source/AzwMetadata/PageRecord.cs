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

            if (!IsRecordId("RESC"))
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

            return IsRecordId("DATP");
        }

        public bool IsKindleEmbedRecord()
        {
            _stream.Position = _pos;

            return IsRecordId("kindle:embed");
        }

        public bool IsCresRecord { get; protected set; }

        public int Length => _len;

#if !DEBUG
        protected bool IsRecordId(string id)
        {
            var bytes = _reader.ReadBytes(id.Length);
            return Encoding.ASCII.GetString(bytes) == id;
        }
#else
        protected bool IsRecordId(string id)
        {
            var peekLen = Math.Min(32, _len);

            var bytes = _reader.ReadBytes(peekLen);

            var idx = Encoding.ASCII.GetString(bytes).IndexOf(id);
            if (idx > 0)
            {
                throw new AzwMetadataException($"Got expected identifier {id} at unexpected postion {idx}");
            }

            return idx == 0;
        }
#endif

        public virtual Span<byte> ReadData()
        {
            _stream.Position = _pos;

            return _reader.ReadBytes(_len);
        }
    }
}
