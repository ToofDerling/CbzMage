namespace AzwMetadata
{
    public class EXTHRecord
    {
        private readonly byte[] recordTypeData = new byte[4];
        private readonly byte[] recordLength = new byte[4];
        private readonly byte[] recordData;

        public EXTHRecord(Stream stream)
        {
            stream.Read(recordTypeData, 0, recordTypeData.Length);

            stream.Read(recordLength, 0, recordLength.Length);
            if (RecordLength < 8)
            {
                throw new IOException("Invalid EXTH record length");
            }

            recordData = new byte[RecordLength - 8];
            stream.Read(recordData, 0, recordData.Length);
        }

        //Properties
        public int DataLength => recordData.Length;

        public int Size => DataLength + 8;

        public uint RecordLength => Converter.ToUInt32(recordLength);

        public uint RecordType => Converter.ToUInt32(recordTypeData);

        public byte[] RecordData => recordData;
    }
}
