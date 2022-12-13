using System.Collections.Concurrent;

namespace AzwConverter
{
    public class TitleReader
    {
        public IDictionary<string, FileInfo[]> ReadBooks()
        {
            var dict = new ConcurrentDictionary<string, FileInfo[]>();

            var directoryInfo = new DirectoryInfo(Settings.AzwDir);
            var allFiles = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);

            allFiles.ToLookup(dir => dir.DirectoryName).AsParallel().ForAll(files => 
            {
                if (files.Any(file => file.IsAzwFile()))
                {
                    dict[Path.GetFileName(files.Key)!] = files.ToArray();
                }
            });

            return new Dictionary<string, FileInfo[]>(dict);
        }

        public async Task<IDictionary<string, FileInfo>> ReadTitlesAsync()
        {
            var directoryInfo = new DirectoryInfo(Settings.TitlesDir);
            var files = directoryInfo.EnumerateFiles().AsParallel().Where(f => f.Name != ArchiveDb.DbName);

            return await ReadFilesAsync(files);
        }

        public async Task<IDictionary<string, FileInfo>> ReadConvertedTitlesAsync()
        {
            var directoryInfo = new DirectoryInfo(Settings.ConvertedTitlesDir);

            return await ReadFilesAsync(directoryInfo.EnumerateFiles());
        }

        private async Task<IDictionary<string, FileInfo>> ReadFilesAsync(IEnumerable<FileInfo> files)
        {
            var dict = new ConcurrentDictionary<string, FileInfo>();

            await Parallel.ForEachAsync(files, async (file, ct) => 
            {
                var bookId = await File.ReadAllTextAsync(file.FullName, ct);
                dict[bookId] = file;
            });

            return new Dictionary<string, FileInfo>(dict);
        }
    }
}
