using System.Text;

namespace AzwMetadata
{
    public class EXTHHead : BaseHeader
    {
        private readonly byte[] identifier = new byte[4];
        private readonly byte[] headerLength = new byte[4];
        private readonly byte[] recordCount = new byte[4];

        private readonly EXTHRecord[] recordList;

        public EXTHHead()
        {
            recordList = Array.Empty<EXTHRecord>();
        }

        public EXTHHead(Stream stream)
        {
            stream.Read(identifier, 0, identifier.Length);

            if (IdentifierAsString != "EXTH")
            {
                throw new IOException("Did not get expected EXTH identifier");
            }

            stream.Read(headerLength, 0, headerLength.Length);
            stream.Read(recordCount, 0, recordCount.Length);

            recordList = new EXTHRecord[RecordCount];
            for (int i = 0; i < RecordCount; i++)
            {
                recordList[i] = new EXTHRecord(stream);
            }
        }

        public bool IsEmpty => !recordList.Any();

        public int Size
        {
            get
            {
                if (IsEmpty)
                {
                    return 0;
                }
                var dataSize = recordList.Sum(r => r.Size);
                return 12 + dataSize + GetPaddingSize(dataSize);
            }
        }

        private static int GetPaddingSize(int dataSize)
        {
            int paddingSize = dataSize % 4;

            if (paddingSize != 0)
            {
                paddingSize = 4 - paddingSize;
            }

            return paddingSize;
        }

        //Properties
        public string IdentifierAsString => Encoding.UTF8.GetString(identifier).Replace("\0", string.Empty);

        public uint HeaderLength => Converter.ToUInt32(headerLength);

        public uint RecordCount => Converter.ToUInt32(recordCount);

        public string Author => GetRecordAsString(100);

        public string Publisher => GetRecordAsString(101);

        public string Imprint => GetRecordAsString(102);

        public string Description => GetRecordAsString(103);

        public string IBSN => GetRecordAsString(104);

        public string Subject => GetRecordAsString(105);

        public string PublishedDate => GetRecordAsString(106);

        public string Review => GetRecordAsString(107);

        public string Contributor => GetRecordAsString(108);

        public string Rights => GetRecordAsString(109);

        public string SubjectCode => GetRecordAsString(110);

        public string Type => GetRecordAsString(111);

        public string Source => GetRecordAsString(112);

        public string ASIN => GetRecordAsString(113);

        public string VersionNumber => GetRecordAsString(114);

        public string RetailPrice => GetRecordAsString(118);

        public string RetailPriceCurrency => GetRecordAsString(119);

        public uint Kf8BoundaryOffset => GetRecordAsUint(121);

        public string BookType => GetRecordAsString(123);

        public uint RescOffset => GetRecordAsUint(131);

        public string DictionaryShortName => GetRecordAsString(200);

        public uint CoverOffset => GetRecordAsUint(201);

        public uint ThumbOffset => GetRecordAsUint(202);

        public uint HasFakeCover => GetRecordAsUint(203);

        public string CDEType => GetRecordAsString(501);

        public string UpdatedTitle => GetRecordAsString(503);

        public string ASIN2 => GetRecordAsString(504);

        private string GetRecordAsString(int recType)
        {
            var record = GetRecord(recType);
            return record != null ? Encoding.UTF8.GetString(record.RecordData) : default;
        }

        private uint GetRecordAsUint(int recType)
        {
            var record = GetRecord(recType);
            return record != null ? Converter.ToUInt32(record.RecordData) : uint.MaxValue;
        }

        private EXTHRecord GetRecord(int recType)
        {
            return recordList.FirstOrDefault(rec => rec.RecordType == recType);
        }
    }
}
