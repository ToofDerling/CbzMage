using MobiMetadata;
using CbzMage.Shared.Helpers;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter.Engine
{
    public abstract class AbstractImageEngine
    {
        protected CbzState? ReadMetaData(string bookId, FileInfo[] dataFiles)
        {
            var azwFile = dataFiles.First(file => file.IsAzwFile());

            //TODO: Handle IOException when Kdle is running.
            using var mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
            using var stream = mappedFile.CreateViewStream();

            var pdbHeader = new PDBHead();
            var palmDocHeader = new PalmDOCHead();
            var mobiHeader = new MobiHead();
            var exthHeader = new EXTHHead();

            // Want the record data of course
            pdbHeader.SetAttrsToRead(pdbHeader.NumRecordsAttr);
            // Nothing from this one
            palmDocHeader.SetAttrsToRead(null);
            
            // Want the exth header, fullname (for the error message below),
            // index of record with first image, index of last content record 
            mobiHeader.SetAttrsToRead(mobiHeader.ExthFlagsAttr, mobiHeader.FullNameOffsetAttr,
                mobiHeader.FirstImageIndexAttr, mobiHeader.LastContentRecordNumberAttr);
            
            // Want the record index offsets for the cover and the thumbnail 
            exthHeader.SetAttrsToRead(exthHeader.CoverOffsetAttr, exthHeader.ThumbOffsetAttr);

            var metadata = new MobiMetadata.MobiMetadata(stream, pdbHeader, palmDocHeader, mobiHeader, exthHeader, throwIfNoExthHeader: true);

            var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResFile());
            if (hdContainer != null)
            {
                using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer.FullName);
                using var hdStream = hdMappedFile.CreateViewStream();

                metadata.ReadHDImageRecords(hdStream);

                return ProcessImages(metadata.PageRecordsHD, metadata.PageRecords);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.Append(bookId).Append(" / ").Append(metadata.MobiHeader.FullName);
                sb.Append(": no HD image container");

                ProgressReporter.Warning(sb.ToString());

                return ProcessImages(null, metadata.PageRecords);
            }
        }

        protected abstract CbzState? ProcessImages(PageRecords? pageRecordsHd, PageRecords pageRecords);

        protected void SaveFile(Span<byte> data, string file)
        {
            using var fileStream = File.Open(file, FileMode.Create, FileAccess.Write);
            fileStream.Write(data);
        }
    }
}