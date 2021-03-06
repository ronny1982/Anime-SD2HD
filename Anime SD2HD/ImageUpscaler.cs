using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record ImageUpscalerArgs(string InputDirectory, string OutputDirectory, string Model, int Scale, int Denoise);

    internal class ImageUpscaler : IExternalProcess<Void, ImageUpscalerArgs>
    {
        private readonly string waifu;
        private const string globImage = "*.png";

        public event EventHandler<ProgressInfoViewModel> ProgressUpdate;
        public event EventHandler<string> StandardOutputReceived;
        public event EventHandler<string> StandardErrorReceived;

        public ImageUpscaler(string application)
        {
            waifu = application;
        }

        private int Upscale(ImageUpscalerArgs args)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(waifu)
            {
                Arguments = $"-i \"{args.InputDirectory}\" -o \"{args.OutputDirectory}\" -f png -m {args.Model} -s {args.Scale} -n {args.Denoise}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            StandardOutputReceived?.Invoke(this, process.StartInfo.FileName + " " + process.StartInfo.Arguments);
            process.OutputDataReceived += (_, args) => StandardOutputReceived?.Invoke(this, args.Data);
            process.ErrorDataReceived += (_, args) => StandardErrorReceived?.Invoke(this, args.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Kill = () =>
            {
                Kill = null;
                process.Kill();
                return process.WaitForExitAsync();
            };
            process.WaitForExit();
            Kill = null;
            return process.ExitCode;
        }

        public async Task<Void> Run(ImageUpscalerArgs args)
        {
            var measure = new Stopwatch();
            var imageCount = Directory.EnumerateFiles(args.InputDirectory, globImage).Count();
            var timer = new System.Timers.Timer(2500)
            {
                AutoReset = true
            };
            timer.Elapsed += (_, __) =>
            {
                var processedCount = Directory.EnumerateFiles(args.OutputDirectory, globImage).Count();
                ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, imageCount, processedCount, measure.Elapsed, ProgressStatus.Active));
            };
            var exitcode = await Task.Run(() => {
                if (Directory.Exists(args.OutputDirectory))
                {
                    Directory.Delete(args.OutputDirectory, true);
                }
                Directory.CreateDirectory(args.OutputDirectory);
                timer.Start();
                measure.Start();
                var exitcode = Upscale(args);
                measure.Stop();
                timer.Stop();
                return exitcode;
            });
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, 1d, 1d, measure.Elapsed, exitcode == 0 ? ProgressStatus.Success : ProgressStatus.Failed));
            return exitcode == 0 ? Void.Default : throw new Exception($"Process failed with exit code: {exitcode}");
        }

        private Func<Task> Kill;
        public Task Abort()
        {
            return Kill?.Invoke() ?? Task.CompletedTask;
        }
    }
}