using CbzMage.Shared.Extensions;

namespace CbzMage.Shared.Helpers
{
    public class ProgressReporter
    {
        private readonly object lockObject = new();

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

            var progress = $">> {message} {convertedProgress}%";

            lock (lockObject)
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

        public static void Warning(string message)
        {
            Show(message, ConsoleColor.DarkYellow);
        }

        public static void DumpErrors(IEnumerable<string> errorLines)
        {
            foreach (var message in errorLines)
            {
                Error(message);
            }
        }

        public static void Error(string message, Exception ex)
        {
            var errorMessage = $"{message}: {ex.TypeAndMessage()}";
            Error(errorMessage);
        }

        public static void Error(string message)
        {
            Show(message, ConsoleColor.DarkRed);
        }

        private static readonly object showLock = new();

        private static void Show(string message, ConsoleColor color)
        {
            lock (showLock)
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
