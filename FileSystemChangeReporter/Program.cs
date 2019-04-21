using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystemChangeReporter
{
    class Program
    {
        public static ConcurrentQueue<string> myFileQueue = new ConcurrentQueue<string>();

        static void Main(string[] args)
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives.Where(drive => drive.IsReady))
            {
                WatchDrive(drive.RootDirectory.FullName);
            }

            Console.WriteLine("Press 'q' to quit the sample.");

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    myFileQueue.TryDequeue(out string message);
                    if (!String.IsNullOrWhiteSpace(message))
                    {
                        Console.WriteLine(message);
                    }
                    Thread.Sleep(10);
                }
            }, TaskCreationOptions.LongRunning);

            Console.ReadKey();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void WatchDrive(string drive)
        {
            var watcher = new FileSystemWatcher()
            {
                Path = drive,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                InternalBufferSize = 65535
            };
    
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            throw e.GetException();
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            var message = $"File: {e.FullPath} {e.ChangeType}";
            myFileQueue.Enqueue(message);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            var message = $"File: {e.OldFullPath} renamed to {e.FullPath}";
            myFileQueue.Enqueue(message);
        }
    }
}
