using System;
using System.Diagnostics;

namespace CoreComicsConverter.Helpers
{
    public static class ProcessHelper
    {
        public static void RunAndWaitForProcess(string switches, Action<string> outputLineReader, string outputDirectory, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
        {
            using var process = GetProcess(switches, outputDirectory);

            process.OutputDataReceived += (s, e) => outputLineReader(e.Data);

            process.Start();

            process.PriorityClass = priorityClass;

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
        }

        private static Process GetProcess(string args, string outputDirectory)
        {
            var process = new Process();

            process.StartInfo.FileName = Settings.SevenZipPath;
            process.StartInfo.Arguments = args;

            process.StartInfo.WorkingDirectory = outputDirectory;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };

            return process;
        }
    }
}
