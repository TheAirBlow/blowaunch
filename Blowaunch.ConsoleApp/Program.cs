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
                RamMax = "1GB"
            }, logger);
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                FileName = "java",
                Arguments = cmd,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            proc.Start();
            while (!proc.HasExited) {
                if (!proc.StandardOutput.EndOfStream)
                    Console.Write(proc.StandardOutput.ReadLine());
                if (!proc.StandardError.EndOfStream)
                    Console.Write(proc.StandardError.ReadLine());
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}