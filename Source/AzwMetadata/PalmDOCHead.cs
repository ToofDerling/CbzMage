namespace AzwMetadata
{
    public class PalmDOCHead : BaseHeader
    {
        private readonly byte[] compression = new byte[2];
        private readonly byte[] unused0 = new byte[2];
        private readonly byte[] textLength = new byte[4];
        private readonly byte[] recordCount = new byte[2];
        private readonly byte[] recordSize = new byte[2];
        private readonly byte[] encryptionType = new byte[2];
        private readonly byte[] unused1 = new byte[2];

        public long Position { get; private set; }

        public PalmDOCHead(Stream stream)
        {
            Position = stream.Position;

            stream.Read(compression, 0, compression.Length);
            stream.Read(unused0, 0, unused0.Length);
            stream.Read(textLength, 0, textLength.Length);
            stream.Read(recordCount, 0, recordCount.Length);

            stream.Read(recordSize, 0, recordSize.Length);
            stream.Read(encryptionType, 0, encryptionType.Length);
            stream.Read(unused1, 0, unused1.Length);
        }

        //Properties
        public ushort Compression => Converter.ToUInt16(compression);

        public string CompressionAsString => Compression switch
        {
            1 => "None",
            2 => "PalmDOC",
            17480 => "HUFF/CDIC",
            _ => $"Unknown (0)",
        };

        public uint TextLength => Converter.ToUInt32(textLength);

        public ushort RecordCount => Converter.ToUInt16(recordCount);

        public ushort RecordSize => Converter.ToUInt16(recordSize);

        public ushort EncryptionType => Converter.ToUInt16(encryptionType);

        public string EncryptionTypeAsString
        {
            get
            {
                switch (EncryptionType)
                {
                    case 0: return "None";
                    case 1: return "Old Mobipocket";
                    case 2: return "Mobipocket"; ;
                    default:
                        return $"Unknown (0)";
                }
            }
        }
    }
}
