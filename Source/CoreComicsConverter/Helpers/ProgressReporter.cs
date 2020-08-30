using System;
using System.Collections.Generic;

namespace CoreComicsConverter.Helpers
{
    public class ProgressReporter
    {
        private readonly object lockObject = new object();

        private readonly int _total;

        private int _current;

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
            LogProgressToConsole(progress);
        }

        public void ShowMessage(string message)
        {
            message = $">> {message}";
            LogProgressToConsole(message);
        }

        private void LogProgressToConsole(string message)
        {
            lock (lockObject)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(message);
            }
        }

        public static void Done(string message)
        {
            Show(message, ConsoleColor.Green);
        }

        public static void Info(string message)
        {
            Console.WriteLine(message);
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

        public static void Error(string message)
        {
            Show(message, ConsoleColor.Red);
        }

        private static void Show(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }
    }
}
