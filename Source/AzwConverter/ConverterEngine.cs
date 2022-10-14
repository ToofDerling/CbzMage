﻿using AzwMetadata;
using CbzMage.Shared.Helpers;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter
{
    public class ConverterEngine
    {
        private bool _createCbz = true;

        private bool _saveCover = false;

        public CbzState ScanBook(string bookId, FileInfo[] dataFiles)
        {
            _createCbz = false;
            return ConvertBook(bookId, dataFiles, null, saveCover: false);
        }

        public CbzState ConvertBook(string bookId, FileInfo[] dataFiles, string cbzFile, bool saveCover)
        {
            _saveCover = saveCover;
            var azwFile = dataFiles.First(file => file.IsAzwFile());

            using var mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
            using var stream = mappedFile.CreateViewStream();

            var metadata = new MobiMetadata(stream);

            var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResFile());
            if (hdContainer != null)
            {
                using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer.FullName);
                using var hdStream = hdMappedFile.CreateViewStream();

                metadata.ReadHDImageRecords(hdStream);
                if (_createCbz)
                {
                    return CreateCbz(cbzFile, metadata.PageRecordsHD, metadata.PageRecords);
                }
                else
                {
                    return ReadPages(metadata.PageRecordsHD, metadata.PageRecords);
                }
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.Append(bookId).Append(" / ").Append(metadata.MobiHeader.FullName);
                sb.Append(": no HD image container");
                ProgressReporter.Warning(sb.ToString());

                if (_createCbz)
                {
                    return CreateCbz(cbzFile, null, metadata.PageRecords);
                }
                else
                {
                    return ReadPages(null, metadata.PageRecords);
                }
            }
        }

        private CbzState CreateCbz(string cbzFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var tempFile = $"{cbzFile}.temp";
            File.Delete(tempFile);

            var state = ReadAndCompress(tempFile, hdImageRecords, sdImageRecords);

            File.Delete(cbzFile);
            File.Move(tempFile, cbzFile);

            return state;
        }

        private CbzState ReadAndCompress(string cbzFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            using var zipArchive = ZipFile.Open(cbzFile, ZipArchiveMode.Create);
            return ReadAndCompressPages(zipArchive, hdImageRecords, sdImageRecords, cbzFile);
        }

        private CbzState ReadPages(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            // Pass a null ziparchiver to work in readonly mode
            return ReadAndCompressPages(null, hdImageRecords, sdImageRecords);
        }

        private CbzState ReadAndCompressPages(ZipArchive? zipArchive, PageRecords? hdImageRecords, PageRecords sdImageRecords, string cbzFile = null)
        {
            var state = new CbzState();

            // Cover
            PageRecord coverRecord = null;
            const string coverName = "cover.jpg";

            // First try HD cover
            state.HdCover = hdImageRecords != null && hdImageRecords.CoverRecord != null && hdImageRecords.CoverRecord.IsCresRecord();
            if (state.HdCover)
            {
                coverRecord = hdImageRecords.CoverRecord;
            }
            // Then the SD cover
            else if ((state.SdCover = sdImageRecords.CoverRecord != null))
            {
                coverRecord = sdImageRecords.CoverRecord;
            }
            
            if (coverRecord != null)
            {
                Write(zipArchive, coverName, coverRecord, isCover: true);
            }

            // Pages
            PageRecord pageRecord;
            var firstPage = true;

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;
                var page = state.PageName();

                // First try the HD image
                if (hdImageRecords != null && hdImageRecords.ContentRecords[i].IsCresRecord())
                {
                    state.HdImages++;
                    pageRecord = hdImageRecords.ContentRecords[i];

                }
                // Else merge in the SD image
                else
                {
                    state.SdImages++;
                    pageRecord = sdImageRecords.ContentRecords[i];
                }

                Write(zipArchive, page, pageRecord, isCover: coverRecord == null && firstPage);
                firstPage = false;
            }

            return state;

            void Write(ZipArchive? zip, string name, PageRecord record, bool isCover = false)
            {
                if (zip != null)
                {
                    var data = record.ReadData();

                    if (_saveCover && isCover && cbzFile != null)
                    {
                        var coverFile = Path.ChangeExtension(cbzFile, ".jpg");
                        using var coverStrem = File.Open(coverFile, FileMode.Create, FileAccess.Write);

                        coverStrem.Write(data);
                    }

                    var entry = zip.CreateEntry(name, Settings.CompressionLevel);
                    using var stream = entry.Open();

                    stream.Write(data);
                }
            }
        }
    }
}