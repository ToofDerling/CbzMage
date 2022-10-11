namespace AzwMetadata
{
    public class PDBRecordInfo
    {
        private readonly byte[] recordDataOffset = new byte[4];
        private readonly byte recordAttributes = 0;
        private readonly byte[] uniqueID = new byte[3];

        public PDBRecordInfo(Stream stream)
        {
            stream.Read(recordDataOffset, 0, recordDataOffset.Length);
            recordAttributes = (byte)stream.ReadByte();
            stream.Read(uniqueID, 0, uniqueID.Length);
        }

        public uint RecordDataOffset => Converter.ToUInt32(recordDataOffset);
    }
}
