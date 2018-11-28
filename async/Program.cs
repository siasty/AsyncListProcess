using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace async
{
    static class Program
    {

        static async Task Main(string[] args)
        {
            try
            {
               await AccessTheProcesAsync();
               Console.WriteLine("\r\nComplete.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\r\nCanceled.\r\n");
            }
            catch (Exception)
            {
                Console.WriteLine("\r\nFailed.\r\n");
            }



            Console.ReadKey();
        }

        class CMD
        {
            public int lp { get; set; }
            public string cmd { get; set; }
            public string arg { get; set; }
        }
        public static async Task<int> RunProcessAsync(string fName, string arg = null, int lp = 1)
        {
            var tcs = new TaskCompletionSource<int>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fName,
                        Arguments = arg,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                process.Exited += (sender, args) =>
                {
                    Console.WriteLine("LP: {2}\t Process Id: {3}\t Total time:    {0} sec.\t\t Exit code:    {1}\r\n", process.TotalProcessorTime.TotalSeconds, process.ExitCode, lp, process.Id);
                    if (process.ExitCode != 0)
                    {
                        tcs.SetException(new InvalidOperationException("The process did not exit correctly."));
                        process.Dispose();
                    }
                    else
                    {
                        tcs.SetResult(process.ExitCode);
                        process.Dispose();
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return await tcs.Task;

        }
        private static List<CMD> CommandList()
        {
            List<CMD> cmd = new List<CMD>
            {
               new CMD{ lp = 1 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 2 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 3 ,cmd = "cmd", arg  = "/C timeout 10"},
               new CMD{ lp = 4 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 5 ,cmd = "cmd", arg  = "/C dir"},
            };
            return cmd;
        }
        private static async Task AccessTheProcesAsync()
        {
 
            List<CMD> CmdlList = CommandList();

            IEnumerable<Task<int>> downloadTasksQuery =
                from url in CmdlList select RunProcessAsync(url.cmd, url.arg, url.lp) ;

            
            List<Task<int>> downloadTasks = downloadTasksQuery.ToList();

            while (downloadTasks.Count > 0)
            {
                Task<int> firstFinishedTask = await Task.WhenAny(downloadTasks);
                downloadTasks.Remove(firstFinishedTask);
                await firstFinishedTask;
            }
        }
        public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>();

            void Process_Exited(object sender, EventArgs e)
            {
                tcs.TrySetResult(true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    await tcs.Task;
                }
            }
            finally
            {
                process.Exited -= Process_Exited;
            }
        }

    }
}
