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
        const string KeyExec = "ExecutableName";
        const string msg = "can't not be found.";
        private static string _executableName = null;
        public static string ExecutableName
        {
            get
            {
                if (_executableName == null)
                {
                    var appConfs = System.Configuration.ConfigurationManager.AppSettings;
                    if (appConfs.AllKeys.Contains(KeyExec))
                        _executableName = appConfs[KeyExec];
                }
                return _executableName;
            }
        }
        static void Usage()
        {
            Console.WriteLine($"{ Assembly.GetAssembly(typeof(Program)).GetName().Name} <filesList.txt> optional : <destination path>");
        }
        static void Main(string[] args)
        {
            try
            {
                if (!ParseArgument(ExecutableName, File.Exists))
                {
                    Console.WriteLine($"{ExecutableName ?? "executable file"} {msg}");
                    Usage();
                    return;
                }
                string urlsList = null;
                string diretoryForDownload = null;
                if (args.Length == 0)
                {
                    Usage();
                    return;
                }
                if (args.Length > 0)
                {
                    bool hasError = false;
                    urlsList = args[0];
                    if ((hasError = !ParseArgument(urlsList, File.Exists)))
                        Console.WriteLine($"{urlsList} {msg}");

                    if (args.Length > 1)
                    {
                        diretoryForDownload = args[1];
                        if ((hasError |= !ParseArgument(diretoryForDownload, Directory.Exists)))
                            Console.WriteLine($"{diretoryForDownload} {msg}");
                    }
                    if (hasError)
                    {
                        Usage();
                        return;
                    }
                }
                RunAsync(urlsList, diretoryForDownload).Wait();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("press a key to exit program..");
            Console.ReadLine();
        }
        private static bool ParseArgument(string value, Func<string, bool> predicate)
        {
            if (!string.IsNullOrWhiteSpace(value) && predicate != null)
                return predicate(value);
            return false;
        }
        private static Task<Process> RunProcessAsync(string exeFile,
                                                    string arguments = null,
                                                    string workingDirecty = null,
                                                    CancellationToken cancellationToken = default)
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

        private static async Task<int> RunAsync(string filePath, string directoryForDownload)
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
                tasks[tempI] = RunProcessAsync(ExecutableName, lines.ElementAt(tempI), directoryForDownload);
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
