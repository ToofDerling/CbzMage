using CbzMage.Shared.Extensions;
using CbzMage.Shared.Jobs;
using PdfConverter.Exceptions;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class Polly : JobWaiter
    {
        private const int _interval = 1000;

        private readonly string _jpgDir;

        private readonly string _outputDir;

        private readonly Pdf _pdf;

        private readonly HashSet<string> _savedPages;

        private readonly List<int>[] _pageList;

        // Key is pagename, Value is imagepath
        private readonly ConcurrentDictionary<string, object> _convertedPages;

        public Polly(Pdf pdf, List<int>[] pageList, string saveDir, string outputId,
            ConcurrentDictionary<string, object> convertedPages)
        {
            _pdf = pdf;
            _jpgDir = Path.Combine(saveDir, "jpg");
            
            _outputDir = Path.Combine(_jpgDir, outputId);
            _outputDir.CreateDirIfNotExists();

            _savedPages = new HashSet<string>(_pdf.PageCount);

            _pageList = pageList;
            _convertedPages = convertedPages;

            //    var pollThread = new Thread(PollLoop)
            //    {
            //        Priority = ThreadPriority.AboveNormal
            //    };
            //    pollThread.Start();

            var watcher = new FileSystemWatcher(_outputDir, "*.png")
            {
                InternalBufferSize = 16384,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            watcher.Created += WatcherOnCreated;
            watcher.Changed += WatcherOnChanged;


        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void WatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void WaitForPagesSaved()
        {
            WaitForJobsToFinish();
        }

        //private void PollLoop()
        //{
        //    while (_savedPages.Count < _pdf.PageCount)
        //    {
        //        Thread.Sleep(_interval);

        //        var files = _saveDir.EnumerateFiles("*.png", SearchOption.AllDirectories)
        //            .OrderBy(f => f.FullName).AsList();

        //        var invoke = false;

        //        foreach (var file in files)
        //        {
        //            //The format is:
        //            // \<title>.temp\<0 based pagelist idx>\<1 based gs pagecount>.png

        //            var listIdx = int.Parse(file.Directory.Name);
        //            var list = _pageLists[listIdx];

        //            var pageIdxStr = file.Name.Split('.')[0];
        //            var pageIdx = int.Parse(pageIdxStr) - 1;

        //            var pageNumber = list[pageIdx];
        //            var page = _pdf.GetPageString(pageNumber);

        //            if (!_convertedPages.TryAdd(page, file.FullName))
        //            {
        //                throw new SomethingWentWrongSorryException($"{file.FullName} already in _convertedFiles???");
        //            }

        //            _savedPages.Add(file.FullName);

        //            // Only invoke the eventhandler once per batch of files
        //            invoke = true;
        //        }

        //        if (invoke)
        //        {
        //            PageSaved?.Invoke(this, new PageConvertedEventArgs("trut"));
        //        }
        //    }

        //    _waitingQueue.Add("Bye from Pollux");
        //}

        public event EventHandler<PageConvertedEventArgs> PageSaved;
    }
}
