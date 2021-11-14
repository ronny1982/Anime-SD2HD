using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record ImageExtractorArgs(string MediaFile, string OutputDirectory, int Width, int Height, int EstimatedFrameCount);

    internal class ImageExtractor : IExternalProcess<Void, ImageExtractorArgs>
    {
        private readonly string ffmpeg;
        private readonly Regex rgxFrame = new(@"frame=\s*(?<FRAME>\d+)");

        public event EventHandler<ProgressInfoViewModel> ProgressUpdate;
        public event EventHandler<string> StandardOutputReceived;
        public event EventHandler<string> StandardErrorReceived;

        public ImageExtractor(string application)
        {
            ffmpeg = application;
        }

        private (int exitcode, TimeSpan runtime) Extract(ImageExtractorArgs args)
        {
            var framecount = args.EstimatedFrameCount;
            var measure = new Stopwatch();
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(ffmpeg)
            {
                Arguments = $"-hide_banner -i \"{args.MediaFile}\" -filter:v \"scale={args.Width}x{args.Height}:flags=lanczos,setsar=1:1\" -f image2 \"{Path.Join(args.OutputDirectory, "%06d.png")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            StandardOutputReceived?.Invoke(this, process.StartInfo.FileName + " " + process.StartInfo.Arguments);
            process.OutputDataReceived += (_, args) => StandardOutputReceived?.Invoke(this, args.Data);
            process.ErrorDataReceived += (_, args) =>
            {
                var matchFrame = rgxFrame.Match(args.Data ?? string.Empty);
                if (matchFrame.Success && matchFrame.Groups.TryGetValue("FRAME", out var group) && int.TryParse(group.Value, out var frame))
                {
                    ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, framecount, frame, measure.Elapsed, ProgressStatus.Active));
                }
                else
                {
                    StandardErrorReceived?.Invoke(this, args.Data);
                }
            };
            measure.Start();
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
            measure.Stop();
            return (process.ExitCode, measure.Elapsed);
        }

        public async Task<Void> Run(ImageExtractorArgs args)
        {
            var (exitcode, runtime) = await Task.Run(() => {
                if (Directory.Exists(args.OutputDirectory))
                {
                    Directory.Delete(args.OutputDirectory, true);
                }
                Directory.CreateDirectory(args.OutputDirectory);
                return Extract(args);
            });
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, 1d, 1d, runtime, exitcode == 0 ? ProgressStatus.Success : ProgressStatus.Failed));
            return exitcode == 0 ? Void.Default : throw new Exception($"Process failed with exit code: {exitcode}");
        }

        private Func<Task> Kill;
        public Task Abort()
        {
            return Kill?.Invoke() ?? Task.CompletedTask;
        }
    }
}