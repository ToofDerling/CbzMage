namespace AzwMetadata
{
    public class PageRecords
    {
        private readonly List<PageRecord> _allRecords;

        public List<PageRecord> ContentRecords { get; set; }

        public List<PageRecord> RestOfRecords { get; set; }

        public ImageType ImageType { get; private set; }

        public PageRecord? CoverRecord { get; private set; }

        public PageRecord? ThumbImage { get; private set; }

        public PageRecord? DatpRecord { get; set; }

        public PageRecord? Len1992Record { get; set; }

        public PageRecord? KindleEmbedRecord { get; set; }

        public RescRecord RescRecord { get; set; }

        public PageRecords(Stream stream, PDBRecordInfo[] pdbRecords, ImageType imageType,
            uint firstImageIndex, ushort lastImageIndex,
            uint coverIndexOffset, uint thumbIndexOffset)
        {
            ImageType = imageType;

            CoverRecord = null;
            ThumbImage = null;

            var coverIndex = firstImageIndex + coverIndexOffset;
            var thumbIndex = firstImageIndex + thumbIndexOffset;

            _allRecords = new List<PageRecord>();
            for (var index = firstImageIndex; index < lastImageIndex; index++)
            {
                var pdbRecord = pdbRecords[index];

                var next = index + 1;
                var nextRecord = pdbRecords[next];

                var dataOffset = pdbRecord.RecordDataOffset;
                var nextRecordOffset = nextRecord.RecordDataOffset;

                PageRecord imageRecord;
                if (imageType == ImageType.SD)
                {
                    imageRecord = new PageRecord(stream, dataOffset, nextRecordOffset - dataOffset);
                }
                else
                {
                    imageRecord = new ImageRecordHD(stream, dataOffset, nextRecordOffset - dataOffset);
                }

                if (coverIndexOffset > 0 && index == coverIndex)
                {
                    CoverRecord = imageRecord;
                }
                else if (thumbIndexOffset > 0 && index == thumbIndex)
                {
                    ThumbImage = imageRecord;
                }
                else
                {
                    _allRecords.Add(imageRecord);
                }
            }
        }

        public void AnalyzePageRecords()
        {
            // Search backwards for the RESC record 
            var rescIndex = _allRecords.Count - 1;
            for (; rescIndex >= 0; rescIndex--)
            {
                var record = _allRecords[rescIndex];
                if (record.TryGetRescRecord(out var rescRecord))
                {
                    rescRecord.ParseXml();
                    RescRecord = rescRecord;

                    break;
                }
            }
            if (RescRecord == null)
            {
                throw new AzwMetadataException($"Found no {nameof(RescRecord)}");
            }
            _allRecords.RemoveAt(rescIndex);

            // Pagecount fixup for some manga books
            if (_allRecords.Count == RescRecord.PageCount)
            {
                var lastRecord = _allRecords.Count - 1;
                if (_allRecords[lastRecord].IsDatpRecord())
                {
                    _allRecords.RemoveAt(lastRecord);
                    RescRecord.AdjustPageCountBy(-1);
                }
            }

            // If we have more records than pagecount filter out the known types
            if (_allRecords.Count > RescRecord.PageCount)
            {
                var restOfRecords = _allRecords.Skip(RescRecord.PageCount).ToList();

                for (int i = 0, sz = restOfRecords.Count; i < sz; i++)
                {
                    var rec = restOfRecords[i];

                    // The DATP record
                    if (rec.IsDatpRecord())
                    {
                        DatpRecord = rec;
                        restOfRecords[i] = null;
                    }
                    // The 1992 bytes image
                    else if (rec.IsLen1992Record())
                    {
                        Len1992Record = rec;
                        restOfRecords[i] = null;
                    }
                }

                // Set the "real" rest and the content records
                RestOfRecords = restOfRecords.Where(rec => rec != null).ToList();
                ContentRecords = _allRecords.Take(RescRecord.PageCount).ToList();
            }
            else
            {
                ContentRecords = _allRecords;
            }

            if (ContentRecords.Count < RescRecord.PageCount)
            {
                throw new AzwMetadataException($"{nameof(ContentRecords)} {ContentRecords.Count} < {nameof(RescRecord.PageCount)} {RescRecord.PageCount}");
            }
        }

        public void AnalyzePageRecordsHD(int pageCount)
        {
            if (_allRecords.Count > pageCount)
            {
                var restOfRecords = _allRecords.Skip(pageCount).ToList();

                for (int i = 0, sz = restOfRecords.Count; i < sz; i++)
                {
                    var rec = restOfRecords[i];

                    // The kindle:embed record
                    if (rec.IsKindleEmbedRecord())
                    {
                        KindleEmbedRecord = rec;
                        restOfRecords[i] = null;
                    }

                    // Set the "real" rest and the content records
                    RestOfRecords = restOfRecords.Where(rec => rec != null).ToList();
                    ContentRecords = _allRecords.Take(pageCount).ToList();
                }
            }
            else
            {
                ContentRecords = _allRecords;
            }

            if (ContentRecords.Count != pageCount)
            {
                throw new AzwMetadataException($"HD container pageCount {ContentRecords.Count} vs SD pageCount {pageCount} mismatch?");
            }
        }
    }
}
