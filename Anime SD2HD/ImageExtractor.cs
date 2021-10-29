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

        public ImageExtractor(string application)
        {
            ffmpeg = application;
        }

        private TimeSpan Extract(ImageExtractorArgs args)
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
            process.OutputDataReceived += (_, args) => Debug.WriteLine("STDOUT [ImageExtractor] => " + args.Data);
            process.ErrorDataReceived += (_, args) =>
            {
                Debug.WriteLine("STDERR [ImageExtractor] => " + args.Data);
                {
                    // NOTE: use the duration from the last (output) stream, since these are the images extracted from the video
                    var matchDuration = rgxDuration.Match(args.Data ?? string.Empty);
                    if (matchDuration.Success && matchDuration.Groups.TryGetValue("TIME", out var group) && TimeSpan.TryParse(group.Value, out var time))
                    {
                        duration = time.TotalSeconds;
                    }
                }
                {
                    var matchTime = rgxTime.Match(args.Data ?? string.Empty);
                    if (matchTime.Success && matchTime.Groups.TryGetValue("TIME", out var group) && TimeSpan.TryParse(group.Value, out var time))
                    {
                        ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, duration, time.TotalSeconds, measure.Elapsed));
                    }
                }
            };
            measure.Start();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            measure.Stop();
            return measure.Elapsed;
        }

        public async Task<Void> Run(ImageExtractorArgs args)
        {
            var elapsed = await Task.Run(() => {
                if (Directory.Exists(args.OutputDirectory))
                {
                    Directory.Delete(args.OutputDirectory, true);
                }
                Directory.CreateDirectory(args.OutputDirectory);
                return Extract(args);
            });
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, 1d, 1d, elapsed));
            return Void.Default;
        }

        public async Task Abort()
        {
            // TODO: Kill process ..
            await Cleanup();
        }

        public async Task Cleanup()
        {
            // TODO: Remove temp data ...
            await Task.Delay(2500);
        }
    }
}