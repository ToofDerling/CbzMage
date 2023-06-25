using CbzMage.Shared.Extensions;
using System.Collections.Concurrent;
using System.Text;

namespace EpubConverter
{
    public class EpubParser
    {
        private readonly Epub _epub;

        public EpubParser(Epub epub)
        {
            _epub = epub;
        }

        public async Task ParseAsync()
        {
            var allFiles = Directory.GetFiles(_epub.Path, "*", SearchOption.AllDirectories);

            var parallelFiles = allFiles.AsParallel();

            var nav = FindMetadataFile(parallelFiles, "nav.xhtml", "nav.xhtml");
            PrintMetaFile("nav", nav);

            if (nav != null)
            {
                _epub.NavXhtml = nav;
            }

            var opf = FindMetadataFile(parallelFiles, ".opf", "package.opf");
            PrintMetaFile("opf", opf);

            void PrintMetaFile(string type, string? file)
            {
                Console.WriteLine($"{type}: {(file == null ? "not found" : file.Replace(_epub.Path, null))}");
            }

            if (opf != null)
            {
                var opfReader = new OpfReader();
                await opfReader.ReadOpfAsync(opf);

                _epub.PageList = opfReader.PageList;
                _epub.ImageList = opfReader.ImageList;
            }
            else
            {
                throw new Exception("No opf file");

                GetFileLists(allFiles, out var xhtmlList, out var jpgList);

                _epub.PageList = xhtmlList;
                _epub.ImageList = jpgList;
            }

            var pageMap = new ConcurrentDictionary<string, string>();

            await Parallel.ForEachAsync(_epub.PageList, async (page, _) =>
            {
                var pageFile = Path.Combine(_epub.Path, page);

                var xhtmlStr = await File.ReadAllTextAsync(pageFile, _);
                pageMap.TryAdd(page, xhtmlStr);
            });

        }

        private static string? FindMetadataFile(ParallelQuery<string> files, string endsWith, string defaultName)
        {
            string? metaFile = null;
            var metaFiles = files.Where(f => f.EndsWithIgnoreCase(endsWith)).AsList();
            if (metaFiles.Count > 0)
            {
                if (metaFiles.Count == 1)
                {
                    metaFile = metaFiles[0];
                }
                else
                {
                    metaFile = metaFiles.FirstOrDefault(f => Path.GetFileName(f).EqualsIgnoreCase(defaultName));
                    metaFile ??= metaFiles.Select(f => new FileInfo(f)).OrderByDescending(fi => fi.Length).First().FullName;
                }
            }

            return metaFile;
        }

        private static void GetFileLists(string[] allFiles, out List<string> xhtmlList, out List<string> jpgList)
        {
            xhtmlList = new List<string>();
            jpgList = new List<string>();

            foreach (var lookup in allFiles.ToLookup(f => Path.GetDirectoryName(f)))
            {
                var tmpXhtmlList = lookup.Where(f => f.EndsWithIgnoreCase(".xhtml") && !f.EndsWithIgnoreCase("nav.xhtml")).ToList();
                if (tmpXhtmlList.Count > xhtmlList.Count)
                {
                    xhtmlList = tmpXhtmlList;
                }

                var tmpJpgList = lookup.Where(f => f.EndsWithIgnoreCase(".jpg") || f.EndsWithIgnoreCase(".jpeg")).ToList();
                if (tmpJpgList.Count > jpgList.Count)
                {
                    jpgList = tmpJpgList;
                }
            }

            xhtmlList = SortFileList(xhtmlList);
            jpgList = SortFileList(jpgList);
        }

        private static List<string> SortFileList(List<string> fileList)
        {
            var sortData = new List<(string file, string baseFile, string? numStr, int? num)>(fileList.Count);

            var numBuilder = new StringBuilder();

            foreach (var file in fileList)
            {
                var baseFile = Path.GetFileNameWithoutExtension(file);

                for (var i = baseFile.Length - 1; i >= 0; i--)
                {
                    if (char.IsDigit(baseFile, i))
                    {
                        numBuilder.Insert(0, baseFile[i]);
                    }
                    else
                    {
                        break;
                    }
                }

                string? numStr = null;
                int? num = null;

                if (numBuilder.Length > 0)
                {
                    numStr = numBuilder.ToString();
                    numBuilder.Length = 0; // Prepare builder for reuse

                    num = int.Parse(numStr);
                    baseFile = baseFile.Replace(numStr, null);
                }

                sortData.Add((file, baseFile, numStr, num));
            }

            var sortedList = new List<string>(fileList.Count);

            foreach (var sort in sortData.ToLookup(s => s.baseFile))
            {
                var sortBatch = sort.ToList();

                // Special case the cover
                if (sort.Key.EqualsIgnoreCase("cover") && sortBatch.Count == 1)
                {
                    sortedList.Insert(0, sortBatch[0].file);
                    continue;
                }

                if (sortBatch.Count > 1 && sortBatch[0].numStr != null)
                {
                    var doSort = false;
                    var numStrLen = sortBatch[0].numStr!.Length;

                    for (int i = 1, sz = sortBatch.Count; i < sz; i++)
                    {
                        // page1.jpg vs page10.jpg vs page100.jpg
                        if (sortBatch[i].numStr!.Length != numStrLen)
                        {
                            doSort = true;
                            break;
                        }
                    }

                    if (doSort)
                    {
                        sortBatch = sortBatch.OrderBy(s => s.num).ToList();
                    }
                }

                sortedList.AddRange(sortBatch.Select(s => s.file));
            }

            // A last attempt to fix cover sorting
            if (!sortedList[0].ContainsIgnoreCase("cover"))
            {
                var swapIdx = -1;
                string? potentialCover = null;

                for (int i = 1, sz = sortedList.Count; i < sz; i++)
                {
                    if (sortedList[i].ContainsIgnoreCase("cover"))
                    {
                        potentialCover = sortedList[i];
                        swapIdx = i;
                        break;
                    }
                }

                if (swapIdx != -1)
                {
                    sortedList.RemoveAt(swapIdx);
                    sortedList.Insert(0, potentialCover!);
                }
            }

            return sortedList;
        }
    }
}
