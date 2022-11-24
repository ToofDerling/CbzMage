using System.Text;

namespace AzwMetadata
{
    public class MobiHead : BaseHeader
    {
        private readonly byte[] identifier = new byte[4];
        private readonly byte[] headerLength = new byte[4];
        private readonly byte[] mobiType = new byte[4];
        private readonly byte[] textEncoding = new byte[4];
        private readonly byte[] uniqueID = new byte[4];
        private readonly byte[] fileVersion = new byte[4];
        private readonly byte[] orthographicIndex = new byte[4];
        private readonly byte[] inflectionIndex = new byte[4];
        private readonly byte[] indexNames = new byte[4];
        private readonly byte[] indexKeys = new byte[4];
        private readonly byte[] extraIndex0 = new byte[4];
        private readonly byte[] extraIndex1 = new byte[4];
        private readonly byte[] extraIndex2 = new byte[4];
        private readonly byte[] extraIndex3 = new byte[4];
        private readonly byte[] extraIndex4 = new byte[4];
        private readonly byte[] extraIndex5 = new byte[4];
        private readonly byte[] firstNonBookIndex = new byte[4];
        private readonly byte[] fullNameOffset = new byte[4];
        private readonly byte[] fullNameLength = new byte[4];
        private readonly byte[] locale = new byte[4];
        private readonly byte[] inputLanguage = new byte[4];
        private readonly byte[] outputLanguage = new byte[4];
        private readonly byte[] minVersion = new byte[4];
        private readonly byte[] firstImageIndex = new byte[4];
        private readonly byte[] huffmanRecordOffset = new byte[4];
        private readonly byte[] huffmanRecordCount = new byte[4];
        private readonly byte[] huffmanTableOffset = new byte[4];
        private readonly byte[] huffmanTableLength = new byte[4];
        private readonly byte[] exthFlags = new byte[4];
        //132	0x84	32	?	32 unknown bytes, if MOBI is long enough
        private readonly int unknown1 = 32;
        //164	0xa4	4	Unknown Use 0xFFFFFFFF
        private readonly int unknown2 = 4;
        //168	0xa8	4	DRM Offset  Offset to DRM key info in DRMed files. 0xFFFFFFFF if no DRM
        private readonly int drmOffset = 4;
        //172	0xac	4	DRM Count   Number of entries in DRM info. 0xFFFFFFFF if no DRM
        private readonly int drmCount = 4;
        //176	0xb0	4	DRM Size    Number of bytes in DRM info.
        private readonly int drmSize = 4;
        //180	0xb4	4	DRM Flags   Some flags concerning the DRM info.
        private readonly int drmFlags = 4;
        //184	0xb8	8	Unknown Bytes to the end of the MOBI header, including the following if the header length >= 228 (244 from start of record). Use 0x0000000000000000.
        private readonly int unknown3 = 8;
        //192	0xc0	2	First content record number Number of first text record. Normally 1.
        private readonly byte[] firstContentRecordNumber = new byte[2];
        //194	0xc2	2	Last content record number  Number of last image record or number of last text record if it contains no images.Includes Image, DATP, HUFF, DRM.
        private readonly byte[] lastContentRecordNumber = new byte[2];

        private readonly EXTHHead exthHeader = null;
        private readonly byte[] fullName;

        public MobiHead(Stream stream, long previousHeaderPos)
        {
            var mobiHeaderOffset = stream.Position;

            stream.Read(identifier, 0, identifier.Length);
            if (IdentifierAsString != "MOBI")
            {
                throw new AzwMetadataException("Did not get expected MOBI identifier");
            }

            stream.Read(headerLength, 0, headerLength.Length);
            stream.Read(mobiType, 0, mobiType.Length);
            stream.Read(textEncoding, 0, textEncoding.Length);
            stream.Read(uniqueID, 0, uniqueID.Length);
            stream.Read(fileVersion, 0, fileVersion.Length);
            stream.Read(orthographicIndex, 0, orthographicIndex.Length);
            stream.Read(inflectionIndex, 0, inflectionIndex.Length);
            stream.Read(indexNames, 0, indexNames.Length);
            stream.Read(indexKeys, 0, indexKeys.Length);
            stream.Read(extraIndex0, 0, extraIndex0.Length);
            stream.Read(extraIndex1, 0, extraIndex1.Length);
            stream.Read(extraIndex2, 0, extraIndex2.Length);
            stream.Read(extraIndex3, 0, extraIndex3.Length);
            stream.Read(extraIndex4, 0, extraIndex4.Length);
            stream.Read(extraIndex5, 0, extraIndex5.Length);
            stream.Read(firstNonBookIndex, 0, firstNonBookIndex.Length);
            stream.Read(fullNameOffset, 0, fullNameOffset.Length);
            stream.Read(fullNameLength, 0, fullNameLength.Length);
            stream.Read(locale, 0, locale.Length);
            stream.Read(inputLanguage, 0, inputLanguage.Length);
            stream.Read(outputLanguage, 0, outputLanguage.Length);
            stream.Read(minVersion, 0, minVersion.Length);
            stream.Read(firstImageIndex, 0, firstImageIndex.Length);
            stream.Read(huffmanRecordOffset, 0, huffmanRecordOffset.Length);
            stream.Read(huffmanRecordCount, 0, huffmanRecordCount.Length);
            stream.Read(huffmanTableOffset, 0, huffmanTableOffset.Length);
            stream.Read(huffmanTableLength, 0, huffmanTableLength.Length);
            stream.Read(exthFlags, 0, exthFlags.Length);
            stream.Position += unknown1;
            stream.Position += unknown2;
            stream.Position += drmOffset;
            stream.Position += drmCount;
            stream.Position += drmSize;
            stream.Position += drmFlags;
            stream.Position += unknown3;
            stream.Read(firstContentRecordNumber, 0, firstContentRecordNumber.Length);
            stream.Read(lastContentRecordNumber, 0, lastContentRecordNumber.Length);

            //If bit 6 (0x40) is set, then there's an EXTH record 
            bool exthExists = (Converter.ToUInt32(exthFlags) & 0x40) != 0;
            if (exthExists)
            {
                // The EXTH header immediately follows the EXTH header, but as the MOBI header is of
                // variable length, we have to calculate the EXTH header offset.
                var exthOffset = mobiHeaderOffset + HeaderLength;
                stream.Position = exthOffset;

                exthHeader = new EXTHHead(stream);
            }
            else
            {
                exthHeader = new EXTHHead();
            }

            //Read the fullname
            var fullnamePos = previousHeaderPos + FullNameOffset;
            stream.Position = fullnamePos;

            fullName = new byte[FullNameLength];
            stream.Read(fullName, 0, fullName.Length);
        }

        //Properties
        public int ExthHeaderSize => exthHeader.Size;

        public string FullName => Encoding.UTF8.GetString(fullName);

        public string IdentifierAsString => Encoding.UTF8.GetString(identifier).Replace("\0", string.Empty);

        public uint HeaderLength => Converter.ToUInt32(headerLength);

        public uint FirstImageIndex => Converter.ToUInt32(firstImageIndex);

        public uint MobiType => Converter.ToUInt32(mobiType);

        public string MobiTypeAsString => MobiType switch
        {
            2 => "Mobipocket Book",
            3 => "PalmDoc Book",
            4 => "Audio",
            257 => "News",
            258 => "News Feed",
            259 => "News Magazine",
            513 => "PICS",
            514 => "WORD",
            515 => "XLS",
            516 => "PPT",
            517 => "TEXT",
            518 => "HTML",
            _ => $"Unknown (0)",
        };

        public uint TextEncoding => Converter.ToUInt32(textEncoding);

        public string TextEncodingAsString => TextEncoding switch
        {
            1252 => "Cp1252",
            65001 => "UTF-8",
            _ => null,
        };

        public uint UniqueID => Converter.ToUInt32(uniqueID);

        public uint FileVersion => Converter.ToUInt32(fileVersion);

        public uint OrthographicIndex => Converter.ToUInt32(orthographicIndex);

        public uint InflectionIndex => Converter.ToUInt32(inflectionIndex);

        public uint IndexNames => Converter.ToUInt32(indexNames);

        public uint IndexKeys => Converter.ToUInt32(indexKeys);

        public uint ExtraIndex0 => Converter.ToUInt32(extraIndex0);

        public uint ExtraIndex1 => Converter.ToUInt32(extraIndex1);

        public uint ExtraIndex2 => Converter.ToUInt32(extraIndex2);

        public uint ExtraIndex3 => Converter.ToUInt32(extraIndex3);

        public uint ExtraIndex4 => Converter.ToUInt32(extraIndex4);

        public uint ExtraIndex5 => Converter.ToUInt32(extraIndex5);

        public uint FirstNonBookIndex => Converter.ToUInt32(firstNonBookIndex);

        public uint FullNameOffset => Converter.ToUInt32(fullNameOffset);

        public uint FullNameLength => Converter.ToUInt32(fullNameLength);

        public uint MinVersion => Converter.ToUInt32(minVersion);

        public uint HuffmanRecordOffset => Converter.ToUInt32(huffmanRecordOffset);

        public uint HuffmanRecordCount => Converter.ToUInt32(huffmanRecordCount);

        public uint HuffmanTableOffset => Converter.ToUInt32(huffmanTableOffset);

        public uint HuffmanTableLength => Converter.ToUInt32(huffmanTableLength);

        public ushort FirstContentRecordNumber => Converter.ToUInt16(firstContentRecordNumber);

        public ushort LastContentRecordNumber => Converter.ToUInt16(lastContentRecordNumber);

        public EXTHHead EXTHHeader => exthHeader;
    }
}
