using CoreComicsConverter.CbzFlow;
using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.PdfFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreComicsConverter
{
    public static class Program
    {

#if DEBUG
        //private const string _testPdf = @"D:\Data\Pdf\Test\Voices of a Distant Star - Makoto Shinkai and Mizu Sahara";
        //private const string _testPdf = @"D:\Data\Pdf\Test\Hawkworld New Edition";
        private const string _testPdf = @"D:\Data\Pdf\Test\";
#else
        private const string _testPdf = null;
#endif

        public static void Main(string[] args)
        {
            //var comic = new DirectoryComic(_testPdf);
            ////pdfComic.ExtractPages(_testPdf);

            //var dconverter = new PdfComicConverter();
            //dconverter.ConvertToCbz(comic, null);

            //var cbzConverter = new ArchiveConverter();
            //cbzConverter.ConvertToPdf(comic);

            var bitness = Environment.Is64BitProcess;

            var path = GetPath(args);
            if (path == null || !StartConvert(path))
            {
                Console.WriteLine("CoreComicsConverter <directory|comic>");
                return;
            }

            converter.WaitForOutputFile(outputFileTask);
            Console.ReadLine();
        }

        private static readonly ComicConverter converter = new ComicConverter();

        private static CreateOutputFileTask outputFileTask;

        private static bool Convert(List<PdfComic> pdfComics)
        {
            pdfComics.ForEach(comic => { outputFileTask = converter.ConversionFlow(comic, outputFileTask); });
            return true;
        }

        private static bool Convert(List<DirectoryComic> directoryComics)
        {
            directoryComics.ForEach(comic => { outputFileTask = converter.ConversionFlow(comic, outputFileTask); });
            return true;
        }

        private static bool Convert(List<CbzComic> cbzComics)
        {
            cbzComics.ForEach(comic => { outputFileTask = converter.ConversionFlow(comic, outputFileTask); });
            return true;
        }

        private static bool StartConvert(string path)
        {
            var directory = new DirectoryInfo(path);
            if (directory.Exists)
            {
                return StartConvertDirectory(directory);

            }
            else if (File.Exists(path))
            {
                return StartConvertFile(path);
            }

            // Nothing to do
            return false;
        }

        private static bool StartConvertDirectory(DirectoryInfo directory)
        {
            var entries = directory.GetFileSystemInfos();
            if (entries.Length == 0)
            {
                // Nothing to do
                return false;
            }

            if (entries.All(e => e.IsDirectory()))
            {
                //TODO
                return false;
            }

            var files = entries.Select(e => e.FullName).ToArray();

            if (FilesAre(".pdf", files))
            {
                return Convert(PdfComic.List(files));
            }
            
            if (FilesAre(".cbz", files))
            {
                return Convert(CbzComic.List(files));
            }
            
            if (FilesAre(".png", files) || FilesAre(".jpg", files) || FilesAre(".jpeg", files))
            {
                return Convert(DirectoryComic.List(directory.FullName, files));
            }

            return false;
        }

        private static bool StartConvertFile(string file)
        {
            file = Path.GetFullPath(file);
            Console.WriteLine(file);

            if (FilesAre(".pdf", file))
            {
                return Convert(PdfComic.List(file));
            }

            if (FilesAre(".cbz", file))
            {
                return Convert(PdfComic.List(file));
            }

            return false;
        }

        private static bool FilesAre(string ext, params string[] files)
        {
            return files.All(f => f.EndsWithIgnoreCase(ext));
        }

        private static string GetPath(string[] args)
        {
            var path = (string)null;

            if (!string.IsNullOrEmpty(_testPdf))
            {
                path = _testPdf;
            }
            else if (args.Length > 0)
            {
                path = args[0];
            }

            return !string.IsNullOrEmpty(path) ? path : null;
        }
    }
}
