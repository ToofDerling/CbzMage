using CbzMage.Shared.Extensions;

namespace CbzMage.Shared.Helpers
{
    public class ProgressReporter
    {
        private readonly object _progressLock = new();

        private readonly int _total;

        private volatile int _current;

        public ProgressReporter(int total)
        {
            _total = total;
        }

        public void ShowProgress(string message)
        {
            var current = ++_current;

            var progressPercentage = current / (_total / 100d);

            var convertedProgress = Convert.ToInt32(progressPercentage);
            convertedProgress = Math.Min(convertedProgress, 100);

            var progress = $"{message} {convertedProgress}%";

            lock (_progressLock)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(progress);
            }
        }

        public void EndProgress()
        {
            Console.WriteLine();
        }

        public static void ShowMessage(string message)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($">> {message}");
        }

        public static void EndMessages()
        {
            Console.WriteLine();
        }

        public static void DoneOrInfo(string message, int count)
        {
            if (count > 0)
            {
                Done(message);
            }
            else
            {
                Info(message);
            }
        }

        public static void Done(string message)
        {
            Show(message, ConsoleColor.DarkGreen);
        }

        public static void Info(string message)
        {
            Console.WriteLine(message);
        }

        public static void Line()
        {
            Console.WriteLine();
        }

        public static void Warning(string message)
        {
            Show(message, ConsoleColor.DarkYellow);
        }

        public static void DumpWarnings(IEnumerable<string> warningLines)
        {
            var warnings = string.Join(Environment.NewLine, warningLines);
            Warning(warnings);
        }

        public static void DumpErrors(IEnumerable<string> errorLines)
        {
            var errors = string.Join(Environment.NewLine, errorLines);
            Error(errors);
        }

        public static void Error(string message, Exception ex)
        {
            var errorMessage = $"{message} {ex.TypeAndMessage()}";

#if DEBUG
            errorMessage = $"{message}{Environment.NewLine}{ex}";
#endif

            Error(errorMessage);
        }

        public static void Error(string message)
        {
            Show(message, ConsoleColor.DarkRed);
        }

        private static readonly object _showLock = new();

        private static void Show(string message, ConsoleColor color)
        {
            lock (_showLock)
            {
                ConsoleColor? previousColor = null;

                if (color != Console.ForegroundColor)
                {
                    previousColor = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                }

                Console.WriteLine(message);

                if (previousColor.HasValue)
                {
                    Console.ForegroundColor = previousColor.Value;
                }
            }
        }
    }
}
