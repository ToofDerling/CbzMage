namespace AzwConverter.Metadata
{
    public class MobiMetadata
    {
        private readonly PDBHead pdbHeader;
        private readonly PalmDOCHead palmDocHeader;
        private readonly MobiHead mobiHeader;
        private readonly PageRecords pageRecords;

        public PDBHead PDBHeader => pdbHeader;

        public PalmDOCHead PalmDocHeader => palmDocHeader;

        public MobiHead MobiHeader => mobiHeader;

        public PageRecords PageRecords => pageRecords;

        public PageRecords? PageRecordsHD { get; private set; }


        public MobiMetadata(Stream stream)
        {
            pdbHeader = new PDBHead(stream);
            palmDocHeader = new PalmDOCHead(stream);
            mobiHeader = new MobiHead(stream, palmDocHeader.Position);

            if (mobiHeader.EXTHHeader.IsEmpty)
            {
                throw new Exception($"{mobiHeader.FullName}: No EXTHHeader");
            }

            var coverIndexOffset = mobiHeader.EXTHHeader.CoverOffset;
            var thumbIndexOffset = mobiHeader.EXTHHeader.ThumbOffset;

            pageRecords = new PageRecords(stream, pdbHeader.Records, ImageType.SD,
                mobiHeader.FirstImageIndex, mobiHeader.LastContentRecordNumber,
                coverIndexOffset, thumbIndexOffset);

            pageRecords.AnalyzePageRecords();
        }

        public void ReadHDImageRecords(Stream hdContainerStream)
        {
            var pdbHead = new PDBHead(hdContainerStream);
            if (!pdbHead.IsHDImageContainer)
            {
                throw new InvalidOperationException("Not a HD image container");
            }

            PageRecordsHD = new PageRecords(hdContainerStream, pdbHead.Records, ImageType.HD,
                1, (ushort)(pdbHead.Records.Length - 1),
                /* Are these two relevant in this context? */
                MobiHeader.EXTHHeader.CoverOffset, MobiHeader.EXTHHeader.ThumbOffset);

            PageRecordsHD.AnalyzePageRecordsHD();
        }
    }
}
