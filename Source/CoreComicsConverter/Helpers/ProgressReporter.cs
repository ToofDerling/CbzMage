using System;

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
    }
}
