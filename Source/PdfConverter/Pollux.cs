using CbzMage.Shared.Extensions;
using CbzMage.Shared.Jobs;
using PdfConverter.Exceptions;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class Pollux : JobWaiter
    {
        private const int _interval = 1000;

        private readonly DirectoryInfo _saveDir;
        private readonly string _jpgDir;

        private readonly Pdf _pdf;

        // Key is pagename, Value is imagepath
        private readonly ConcurrentDictionary<string, object> _convertedPages;

        private readonly HashSet<string> _savedPages;

        private readonly List<int>[] _pageLists;

        private readonly string _oldCurrentDirectory;

        public Pollux(Pdf pdf, List<int>[] pageLists, ConcurrentDictionary<string, object> convertedPages)
        {
            _pdf = pdf;

            var dir = Path.ChangeExtension(_pdf.Path, $".temp");

            _saveDir = new DirectoryInfo(dir);
            if (_saveDir.Exists)
            {
                _saveDir.Delete(true);
            }
            _saveDir.Create();

            _jpgDir = Path.Combine(dir, "jpg");
            Directory.CreateDirectory(_jpgDir); 

            // This is needed because Ghostscript has no notion of outputdirectory
            _oldCurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _saveDir.FullName;

            _convertedPages = convertedPages;
            _savedPages = new HashSet<string>(_pdf.PageCount);

            _pageLists = pageLists;

            var pollThread = new Thread(PollLoop)
            {
                Priority = ThreadPriority.AboveNormal
            };
            pollThread.Start();
        }

        public void CreateOutputIdDir(string outputId)
        {
            _saveDir.CreateSubdirectory(outputId);
        }

        public void Cleanup()
        {
            Environment.CurrentDirectory = _oldCurrentDirectory;
            _saveDir.Delete(recursive: true);
        }

        public void WaitForPagesSaved()
        {
            WaitForJobsToFinish();
        }

        private void PollLoop()
        {
            while (_savedPages.Count < _pdf.PageCount)
            {
                Thread.Sleep(_interval);

                var files = _saveDir.EnumerateFiles("*.jpg", SearchOption.AllDirectories)
                    .Where(file => !_savedPages.Contains(file.FullName)).AsList();

                var invoke = false;

                foreach (var file in files)
                {
                    //The format is:
                    // \<title>.temp\<0 based pagelist idx>\<1 based gs pagecount>.png

                    var listIdx = int.Parse(file.Directory.Name);
                    var list = _pageLists[listIdx];

                    var pageIdxStr = file.Name.Split('.')[0];
                    var pageIdx = int.Parse(pageIdxStr) - 1;

                    var pageNumber = list[pageIdx];
                    var page = _pdf.GetPageString(pageNumber);

                    if (!_convertedPages.TryAdd(page, file.FullName))
                    {
                        throw new SomethingWentWrongSorryException($"{file.FullName} already in _convertedFiles???");
                    }

                    _savedPages.Add(file.FullName);

                    // Only invoke the eventhandler once per batch of files
                    invoke = true;
                }

                if (invoke)
                {
                    PageSaved?.Invoke(this, new PageConvertedEventArgs("trut"));
                }
            }

            _waitingQueue.Add("Bye from Pollux");
        }

        public event EventHandler<PageConvertedEventArgs> PageSaved;
    }
}
