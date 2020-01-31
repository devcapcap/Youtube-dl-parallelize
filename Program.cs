using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Youtubedlparallelize
{
    sealed class Program
    {
        const string Exec = "youtube-dl.exe";
        static void Usage()
        {
            Console.WriteLine($"{ Assembly.GetAssembly(typeof(Program)).GetName().Name} <filesList.txt>");
        }
        static void Main(string[] args)
        {
            try
            {
                if(!File.Exists(Exec))
                {
                    Console.WriteLine($"{Exec} can't not be found");
                    Usage();
                    return;
                }
                string urlsList = null;
                if ( args.Length == 0 
                    || (string.IsNullOrWhiteSpace(args[0])
                    && !File.Exists(args[0])))
                {
                    Console.WriteLine($"{urlsList} can't not be found");
                    Usage();
                    return;
                }
                urlsList = args[0];
                RunAsync(urlsList).Wait();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("press a key to exit program..");
            Console.ReadLine();
        }

        private static Task<Process> RunProcessAsync(string exeFile,string arguments=null,
                                                    string workingDirecty = null,CancellationToken cancellationToken = default(CancellationToken))
        {
            TaskCompletionSource<Process> taskCompletionSource = new TaskCompletionSource<Process>();
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = exeFile,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = workingDirecty
                },
                EnableRaisingEvents = true
            };
            process.Exited += (s, e) => {
                taskCompletionSource.SetResult(process);
            };
            cancellationToken.ThrowIfCancellationRequested();

            process.Start();

            cancellationToken.Register(() => {
                process.CloseMainWindow();
            });

            return taskCompletionSource.Task;
        }
        private static async Task<int> RunAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"{filePath} doesn't exit.");
            }
            IEnumerable<string> lines = File.ReadLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l));
            if (!lines.Any())
                return 0;

            Task<Process>[] tasks = new Task<Process>[lines.Count()];
            for (int i = 0; i < tasks.Length; ++i)
            {
                int tempI = i;
                tasks[tempI] = RunProcessAsync(Exec,lines.ElementAt(tempI),Environment.CurrentDirectory);
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (AggregateException age)
            {
                throw new Exception("error", age.Flatten());
            }
            return 0;
        }
    }
}
