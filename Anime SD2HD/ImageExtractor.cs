using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record ImageExtractorArgs(string MediaFile, string OutputDirectory, int Width, int Height);

    internal class ImageExtractor : IExternalProcess<Void, ImageExtractorArgs>
    {
        private readonly string ffmpeg;
        private const string matchTimeSpan = @"(?<TIME>\d{2}:\d{2}:\d{2}\.\d{2})";
        private readonly Regex rgxDuration = new(@"DURATION\s*:\s*" + matchTimeSpan);
        private readonly Regex rgxTime = new(@"time=" + matchTimeSpan);

        public event EventHandler<ProgressInfoViewModel> ProgressUpdate;
        public event EventHandler<string> StandardOutputReceived;
        public event EventHandler<string> StandardErrorReceived;

        public ImageExtractor(string application)
        {
            ffmpeg = application;
        }

        private (int exitcode, TimeSpan runtime) Extract(ImageExtractorArgs args)
        {
            var duration = 0d;
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
                // NOTE: use the duration from the last (output) stream, since these are the images extracted from the video
                var matchDuration = rgxDuration.Match(args.Data ?? string.Empty);
                if (matchDuration.Success && matchDuration.Groups.TryGetValue("TIME", out var groupDuration) && TimeSpan.TryParse(groupDuration.Value, out var valueDuration))
                {
                    duration = valueDuration.TotalSeconds;
                }
                var matchTime = rgxTime.Match(args.Data ?? string.Empty);
                if (matchTime.Success && matchTime.Groups.TryGetValue("TIME", out var groupTime) && TimeSpan.TryParse(groupTime.Value, out var valueTime))
                {
                    ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, duration, valueTime.TotalSeconds, measure.Elapsed, ProgressStatus.Active));
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
                process.WaitForExit();
            };
            process.WaitForExit();
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

        private Action Kill;
        public async Task Abort()
        {
            Kill?.Invoke();
            await Cleanup();
        }

        public async Task Cleanup()
        {
            // TODO: Remove temp data ...
            await Task.Delay(2500);
        }
    }
}