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

        public static void Done(string message)
        {
            Show(message, ConsoleColor.Green);
        }

        public static void Info(string message)
        {
           Show(message, ConsoleColor.White);
        }

        public static void Warning(string message)
        {
            Show(message, ConsoleColor.Yellow);
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
            Show(message, ConsoleColor.Red);
        }

        private static readonly object showLock = new();

        private static void Show(string message, ConsoleColor color)
        {
            lock (showLock)
            {
                if (color != Console.ForegroundColor)
                {
                    Console.ForegroundColor = color;
                }

                Console.WriteLine(message);

                if (Console.ForegroundColor != ConsoleColor.White)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
    }
}
