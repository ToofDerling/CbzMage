using System.Text;

namespace AzwMetadata
{
    public partial class PDBHead : BaseHeader
    {
        private readonly byte[] name = new byte[32];

        private readonly byte[] attributes = new byte[2];
        private readonly byte[] version = new byte[2];
        private readonly byte[] creationDate = new byte[4];
        private readonly byte[] modificationDate = new byte[4];
        private readonly byte[] lastBackupDate = new byte[4];
        private readonly byte[] modificationNumber = new byte[4];
        private readonly byte[] appInfoID = new byte[4];
        private readonly byte[] sortInfoID = new byte[4];
        private readonly byte[] type = new byte[4];
        private readonly byte[] creator = new byte[4];
        private readonly byte[] uniqueIDSeed = new byte[4];
        private readonly byte[] nextRecordListID = new byte[4];
        private readonly byte[] numRecords = new byte[2];
        private readonly PDBRecordInfo[] recordInfoList;
        private readonly byte[] gapToData = new byte[2];

        public PDBHead(Stream fs)
        {
            fs.Read(name, 0, name.Length);
            fs.Read(attributes, 0, attributes.Length);
            fs.Read(version, 0, version.Length);
            fs.Read(creationDate, 0, creationDate.Length);
            fs.Read(modificationDate, 0, modificationDate.Length);
            fs.Read(lastBackupDate, 0, lastBackupDate.Length);
            fs.Read(modificationNumber, 0, modificationNumber.Length);
            fs.Read(appInfoID, 0, appInfoID.Length);
            fs.Read(sortInfoID, 0, sortInfoID.Length);

            fs.Read(type, 0, type.Length);
            fs.Read(creator, 0, creator.Length);
            fs.Read(uniqueIDSeed, 0, uniqueIDSeed.Length);
            fs.Read(nextRecordListID, 0, nextRecordListID.Length);
            fs.Read(numRecords, 0, numRecords.Length);

            int recordCount = Converter.ToInt16(numRecords);
            recordInfoList = new PDBRecordInfo[recordCount];
            for (int i = 0; i < recordCount; i++)
            {
                recordInfoList[i] = new PDBRecordInfo(fs);
            }

            fs.Read(gapToData, 0, gapToData.Length);
        }

        public bool IsHDImageContainer => TypeAsString == "RBIN" && CreatorAsString == "CONT";

        public string Name => Encoding.ASCII.GetString(name).Replace("\0", string.Empty);

        public ushort Attributes => Converter.ToUInt16(attributes);

        public ushort Version => Converter.ToUInt16(version);

        public uint CreationDate => Converter.ToUInt32(creationDate);

        public uint ModificationDate => Converter.ToUInt32(creationDate);

        public uint LastBackupDate => Converter.ToUInt32(lastBackupDate);

        public uint ModificationNumber => Converter.ToUInt32(modificationNumber);

        public uint AppInfoID => Converter.ToUInt32(appInfoID);

        public uint SortInfoID => Converter.ToUInt32(sortInfoID);

        public uint Type => Converter.ToUInt32(type);

        public string TypeAsString => Encoding.ASCII.GetString(type).Replace("\0", string.Empty);

        public uint Creator => Converter.ToUInt32(creator);

        public string CreatorAsString => Encoding.ASCII.GetString(creator).Replace("\0", string.Empty);

        public uint UniqueIDSeed => Converter.ToUInt32(uniqueIDSeed);

        public ushort NumRecords => Converter.ToUInt16(numRecords);

        public ushort GapToData => Converter.ToUInt16(gapToData);

        public uint MobiHeaderSize => recordInfoList.Length > 1
                                        ? recordInfoList[1].RecordDataOffset - recordInfoList[0].RecordDataOffset
                                        : 0;
        public PDBRecordInfo[] Records => recordInfoList;
    }
}
