namespace AzwConverter
{
    public class TitleReader
    {
        public Dictionary<string, string[]> ReadBooks()
        {
            var books = new Dictionary<string, string[]>();

            var dirs = Directory.GetDirectories(Settings.AzwDir, "*_EBOK");
            foreach (var bookDir in dirs)
            {
                var files = Directory.GetFiles(bookDir);
                if (files.Any(f => f.EndsWith(Settings.AzwExt)))
                {
                    books[Path.GetFileName(bookDir)] = files;
                }
            }

            return books;
        }

        public Dictionary<string, string> ReadTitles()
        {
            return ReadDir(Settings.TitlesDir);
        }

        public Dictionary<string, string> ReadConvertedTitles()
        {
            return ReadDir(Settings.ConvertedTitlesDir, SearchOption.TopDirectoryOnly);
        }

        private static Dictionary<string, string> ReadDir(string dir, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(dir, "*", searchOption)
                .Where(f => Path.GetFileName(f) != "archive.db").ToDictionary(f => File.ReadAllText(f), f => f);
        }
    }
}
