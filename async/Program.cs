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
            CancellationTokenSource cts = new CancellationTokenSource();

            await AccessTheProcesAsync().WithWaitCancellation(cts.Token);


            Console.ReadKey();
        }

        class CMD
        {
            public int lp { get; set; }
            public string cmd { get; set; }
            public string arg { get; set; }
        }

        public static int RunProcess(string fName, string arg = null, int lp = 1)
        {
            int result = 0;
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
                    result = process.ExitCode;
                    if (!process.HasExited)
                    {
                        process.Dispose();
                        process.Kill();
                    }
                };

                if (process.Start())
                {
                    process.WaitForExit();
                }
            }catch(Exception ex)
            {
                Console.WriteLine("LP: {2}\t Process Id: {3}\t Total time:    {0} sec.\t\t Exit code:    {1}\r\n", 0, 1, lp, 0);
                result = 1;
            }
            return result;

        }

        private static List<CMD> CommandList()
        {
            List<CMD> cmd = new List<CMD>
            {
               new CMD{ lp = 1 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 2 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 3 ,cmd = "cmd", arg  = "/C timeout 20"},
               new CMD{ lp = 4 ,cmd = "cmd", arg  = "/C dir"},
               new CMD{ lp = 5 ,cmd = "cmd", arg  = "/C dir"},
            };
            return cmd;
        }

        public static async Task<bool> AccessTheProcesAsync()
        {
            var guid = Guid.NewGuid();
            var tcs = new TaskCompletionSource<bool>();

            List<CMD> CmdlList = CommandList();
            int i = 0;
            int count = CmdlList.Count();
            int exe = 0;


            foreach(var url in CmdlList)
            {
                var _exe = RunProcess(url.cmd, url.arg, url.lp);
                i = i + _exe;
            }
            exe = count - i;
            Console.Write("Executed {0}/{1} command. ", exe, count);
            if (i != 0)
            {
                tcs.SetResult(false);
                Console.WriteLine("Task {0} fail.",guid.ToString());
            } else
            {
                tcs.SetResult(true);
                Console.WriteLine("Task {0} complited.",guid.ToString());
            }

            return await tcs.Task;
        }

        public static async Task<T> WithWaitCancellation<T>( this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task)) throw new OperationCanceledException(cancellationToken);
            }

            return await task;
        }

    }
}
