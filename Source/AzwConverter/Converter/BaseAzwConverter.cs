using System.Diagnostics;
using System.Text;
using CbzMage.Shared;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;

namespace AzwConverter.Converter
{
    public class BaseAzwConverter
    {
        protected int _totalBooks;

        // Global veriables updated by processing threads
        protected volatile int _bookCount;
        protected volatile int _pagesCount;

        protected CbzMageAction Action { get; private set; }

        private readonly Stopwatch _stopWatch = new();

        public BaseAzwConverter(CbzMageAction action)
        {
            Action = action;

            var config = new AzwConvertSettings();
            config.CreateSettings();
        }

        protected void ConversionBegin()
        {
            _stopWatch.Start();
        }

        protected void ConversionEnd(int unconvertedCount)
        {
            _stopWatch.Stop();
            ProgressReporter.Line();

            if (Action == CbzMageAction.AzwConvert && _pagesCount > 0)
            {
                var elapsed = _stopWatch.Elapsed;
                var secsPerPage = elapsed.TotalSeconds / _pagesCount;

                if (Settings.SaveCoverOnly)
                {
                    ProgressReporter.Info($"{_bookCount} covers saved in {elapsed.Hhmmss()}");
                }
                else if (unconvertedCount > 0)
                {
                    ProgressReporter.Info($"{_pagesCount} pages converted in {elapsed.Hhmmss()} ({secsPerPage:F2} sec/page)");
                }
                else
                {
                    ProgressReporter.Info("Done");
                }
            }
            else
            {
                ProgressReporter.Info("Done");
            }
        }

        protected string BookCountOutputHelper(string path, out StringBuilder sb)
        {
            sb = new StringBuilder();
            sb.AppendLine();

            var count = Interlocked.Increment(ref _bookCount);
            var str = $"{count}/{_totalBooks} - ";

            sb.Append(str).Append(Path.GetFileName(path));

            var insert = " ".PadLeft(str.Length);
            return insert;
        }

        protected void PrintCbzState(string cbzFile, CbzItem state,
            bool showPagesAndCover = true, bool showAllCovers = false,
            DateTime? convertedDate = null, string? doneMsg = null, string? errorMsg = null)
        {
            Interlocked.Add(ref _pagesCount, state.Pages);

            var insert = BookCountOutputHelper(cbzFile, out var sb);
            sb.AppendLine();

            if (convertedDate.HasValue)
            {
                sb.Append(insert).Append("Converted: ").Append(convertedDate.Value.Date.ToShortDateString());
                sb.AppendLine();
            }

            sb.Append(insert);
            sb.Append(state.Pages).Append(" pages");

            if (showPagesAndCover)
            {
                sb.Append(" (");

                if (state.HdImages > 0)
                {
                    sb.Append(state.HdImages).Append(" HD");
                    if (state.SdImages > 0)
                    {
                        sb.Append('/');
                    }
                }
                if (state.SdImages > 0)
                {
                    sb.Append(state.SdImages).Append(" SD");
                }

                sb.Append(". ");
                if (showAllCovers)
                {
                    if (state.HdCover)
                    {
                        sb.Append("HD");
                        if (state.SdCover)
                        {
                            sb.Append('/');
                        }
                    }
                    if (state.SdCover)
                    {
                        sb.Append("SD");
                    }
                    if (!state.HdCover && !state.SdCover)
                    {
                        sb.Append("No");
                    }
                    sb.Append(" cover");
                }
                else
                {
                    if (state.HdCover)
                    {
                        sb.Append("HD cover");
                    }
                    else if (state.HdCover)
                    {
                        sb.Append("SD cover)");
                    }
                    else
                    {
                        sb.Append("No cover");
                    }
                }
                sb.Append(')');
            }

            PrintMsg(sb, insert, doneMsg, errorMsg);
        }

        private static void PrintMsg(StringBuilder sb, string insert, string? doneMsg, string? errorMsg)
        {
            if (doneMsg != null || errorMsg != null)
            {
                lock (_msgLock)
                {
                    ProgressReporter.Info(sb.ToString());

                    if (doneMsg != null)
                    {
                        ProgressReporter.Done($"{insert}{doneMsg}");
                    }
                    if (errorMsg != null)
                    {
                        ProgressReporter.Error($"{insert}{errorMsg}");
                    }
                }
            }
            else
            {
                ProgressReporter.Info(sb.ToString());
            }
        }

        private static readonly object _msgLock = new();

        protected void PrintCoverString(string coverFile, string coverString)
        {
            var insert = BookCountOutputHelper(coverFile, out var sb);

            sb.AppendLine();
            sb.Append(insert).Append(coverString);

            ProgressReporter.Info(sb.ToString());
        }
    }
}