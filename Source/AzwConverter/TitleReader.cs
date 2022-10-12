using System.Collections.Concurrent;

namespace AzwConverter
{
    public class TitleReader
    {
        public Dictionary<string, FileInfo[]> ReadBooks()
        {
            var books = new Dictionary<string, FileInfo[]>();

            var dirs = Directory.GetDirectories(Settings.AzwDir, "*_EBOK");
            foreach (var bookDir in dirs)
            {
                var dirInfo = new DirectoryInfo(bookDir);
                var files = dirInfo.GetFiles();

                if (files.Any(file => file.IsAzwFile()))
                {
                    books[Path.GetFileName(bookDir)] = files;
                }
            }

            return books;
        }

        public Dictionary<string, FileInfo> ReadTitles()
        {
            var directoryInfo = new DirectoryInfo(Settings.TitlesDir);
            var files = directoryInfo.EnumerateFiles().Where(f => f.Name != ArchiveDb.DbName);

            return ReadFiles(files);
        }

        public Dictionary<string, FileInfo> ReadConvertedTitles()
        {
            var directoryInfo = new DirectoryInfo(Settings.ConvertedTitlesDir);

            return ReadFiles(directoryInfo.EnumerateFiles());
        }

        private Dictionary<string, FileInfo> ReadFiles(IEnumerable<FileInfo> files)
        {
            var dict = new ConcurrentDictionary<string, FileInfo>();

            Parallel.ForEach(files, Settings.ParallelOptions, file =>
            {
                dict[File.ReadAllText(file.FullName)] = file;
            });

            return new Dictionary<string, FileInfo>(dict);
        }
    }
}
