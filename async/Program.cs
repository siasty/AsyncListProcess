using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace async
{
    class Program
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

        class MyProcess : Process
        {
            public void Stop(TaskCompletionSource<int> tcs,int lp)
            {
                Console.WriteLine("LP: {2}\t Total time:    {0} sec.\t\t Exit code:    {1}\r\n", this.TotalProcessorTime.TotalSeconds, this.ExitCode,lp);

                tcs.SetResult(this.ExitCode);
                this.Dispose();
                this.Close();
                OnExited();

            }
        }
        class CMD
        {
            public int lp { get; set; }
            public string cmd { get; set; }
            public string arg { get; set; }
        }
        public static Task<int> RunProcessAsync(string fName, string arg = null, int lp = 1)
        {
            var tcs = new TaskCompletionSource<int>();

            var proc = new MyProcess();
            try
            {    
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = fName,
                        Arguments = arg,
                        Verb = string.Format("Command_{0}",lp),
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true
                    };
               
                proc.Start();
                proc.WaitForExit();
                proc.Stop(tcs,lp);

            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                Console.WriteLine("LP: {0} \t Error: {1}\r\n", lp, ex.Message);
            }

            return tcs.Task;
        }
        private static List<CMD> CommandList()
        {
            List<CMD> cmd = new List<CMD>
            {
               new CMD{ lp = 1 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 2 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 3 ,cmd = "cmds", arg  = "/C dir"},
               new CMD{ lp = 4 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 5 ,cmd = "cmd", arg  = "/C dir"},
            };
            return cmd;
        }
        static async Task AccessTheProcesAsync()
        {
 
            List<CMD> CmdlList = CommandList();

            IEnumerable<Task<int>> downloadTasksQuery =
                from url in CmdlList select RunProcessAsync(url.cmd, url.arg, url.lp);

            List<Task<int>> downloadTasks = downloadTasksQuery.ToList();

            while (downloadTasks.Count > 0)
            {
                // Identify the first task that completes.
                Task<int> firstFinishedTask = await Task.WhenAny(downloadTasks);

                // ***Remove the selected task from the list so that you don't
                downloadTasks.Remove(firstFinishedTask);

                // Await the completed task.
                await firstFinishedTask;
            }
        }
    }
}
