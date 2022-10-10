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

            return directoryInfo.EnumerateFiles().Where(f => f.Name != "archive.db").ToDictionary(f => File.ReadAllText(f.FullName), f => f);
        }

        public Dictionary<string, FileInfo> ReadConvertedTitles()
        {
            var directoryInfo = new DirectoryInfo(Settings.ConvertedTitlesDir);

            return directoryInfo.EnumerateFiles().ToDictionary(f => File.ReadAllText(f.FullName), f => f);
        }
    }
}
