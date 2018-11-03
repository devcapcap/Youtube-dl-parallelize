using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Youtube_dl_parallelize
{
    class Program
    {
        //const string urlsList = @"power_links.txt";
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
                    && !File.Exists((urlsList = args[0]))))
                {
                    Console.WriteLine($"{urlsList} can't not be found");
                    Usage();
                    return;
                }
                
                RunAscyn(urlsList).Wait();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("press a key to exit program..");
            Console.ReadLine();
        }

        private async static Task<int> RunAscyn(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"{filePath} doesn't exit.");
            }
            IEnumerable<string> lines = File.ReadLines(filePath).Where(l=> !string.IsNullOrWhiteSpace(l) );
            if (!lines.Any())
                return 0;

            List<Task> task = new List<Task>();
            for( int i = 0; i < lines.Count(); ++i)
            {
                int tempI = i;
                task.Add(Task.Run(() =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Exec,
                        Arguments = lines.ElementAt(tempI),
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = Environment.CurrentDirectory
                    });//.WaitForExit();
                }));
            }
            try
            {
                await Task.WhenAll(task.ToArray());
            }
            catch (AggregateException age)
            {
                throw new Exception("error", age.Flatten());
            }
            return 0;
        }
    }
}
