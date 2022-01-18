using System;
using System.Diagnostics;
using System.IO;
using Blowaunch.Library;
using Blowaunch.Library.Downloader;
using Serilog;

namespace Blowaunch.ConsoleApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("Enter version: ");
            var ver = Console.ReadLine();
            var main = MojangFetcher.GetMain(ver);
            MainDownloader.DownloadAll(main, logger);
            var cmd = Runner.GenerateCommand(main, new Runner.Configuration {
                UserName = "TheAirBlow",
                Auth = new Runner.Configuration.AuthClass {
                    Type = Runner.Configuration.AuthClass.AuthType.None
                },
                RamMax = "1024m"
            }, logger);
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                FileName = "java",
                Arguments = cmd,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            proc.OutputDataReceived += (_, e) => {
                Console.WriteLine(e.Data);
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            Console.WriteLine("\nPress any key to close...");
            Console.ReadKey();
        }
    }
}