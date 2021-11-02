using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record MediaInfo(int VideoWidth, int VideoHeight, DisplayAspectRatio DisplayAspectRatio, string FrameRate);

    internal record DisplayAspectRatio(int Width, int Height)
    {
        public static DisplayAspectRatio Parse(string format)
        {
            var dar = format.Split(':').Select(number => Convert.ToInt32(number));
            return new DisplayAspectRatio(dar.First(), dar.Last());
        }
        public double Ratio { get; init; } = Height > 0d ? (double)Width / (double)Height : 0d;
    }

    internal class MediaInfoExtractor : IExternalProcess<MediaInfo, string>
    {
        private readonly string ffprobe;
        private readonly DisplayAspectRatio[] commonDAR = new[]
        {
            new DisplayAspectRatio(1, 1),     // Square
            new DisplayAspectRatio(4, 3),     // Silent Film/NTSC
            new DisplayAspectRatio(137, 100), // Academy Ratio
            new DisplayAspectRatio(143, 100), // IMAX
            new DisplayAspectRatio(3, 2),     // Classic 35mm
            new DisplayAspectRatio(16, 10),   // ...
            new DisplayAspectRatio(7, 4),     // Metroscope
            new DisplayAspectRatio(16, 9),    // ...
            new DisplayAspectRatio(185, 100), // Vistavision
            new DisplayAspectRatio(2, 1),     // Panascope & RED
            new DisplayAspectRatio(22, 10),   // Todd AI
            new DisplayAspectRatio(21, 9),    // ...
            new DisplayAspectRatio(235, 100), // Cinemascope
            new DisplayAspectRatio(239, 100), // Theatrical & Blu-ray
            new DisplayAspectRatio(255, 100), // Vintage Cinemascope
            new DisplayAspectRatio(275, 100), // Ultra Panavision
            new DisplayAspectRatio(276, 100), // MGM Camera 65
            new DisplayAspectRatio(3, 1),     // Extreme Scope
            new DisplayAspectRatio(32, 9),    // ...
            new DisplayAspectRatio(4, 1),     // PolyVision
        };

        public event EventHandler<ProgressInfoViewModel> ProgressUpdate;
        public event EventHandler<string> StandardOutputReceived;
        public event EventHandler<string> StandardErrorReceived;

        public MediaInfoExtractor(string application)
        {
            ffprobe = application;
        }

        private (int exitcode, MediaInfo info) Extract(string mediaFile)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(ffprobe)
            {
                Arguments = $"-hide_banner -i \"{mediaFile}\" -select_streams v:0 -show_entries format=duration:stream=duration,width,height,r_frame_rate,display_aspect_ratio,sample_aspect_ratio -loglevel quiet -of json",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            StandardOutputReceived?.Invoke(this, process.StartInfo.FileName + " " + process.StartInfo.Arguments);
            process.OutputDataReceived += (_, args) => StandardOutputReceived?.Invoke(this, args.Data);
            process.ErrorDataReceived += (_, args) => StandardErrorReceived?.Invoke(this, args.Data);
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var json = JsonDocument.Parse(stdout);
            var stream = json.RootElement.GetProperty("streams")[0];
            var mediaDAR = DisplayAspectRatio.Parse(stream.GetProperty("display_aspect_ratio").GetString());
            var dar = commonDAR.OrderBy(curDAR => Math.Abs(curDAR.Ratio - mediaDAR.Ratio)).FirstOrDefault(curDAR => Math.Abs(curDAR.Ratio - mediaDAR.Ratio) < 0.01d) ?? mediaDAR;

            return (process.ExitCode, new MediaInfo(
                stream.GetProperty("width").GetInt32(),
                stream.GetProperty("height").GetInt32(),
                dar,
                stream.GetProperty("r_frame_rate").GetString()));
        }

        public async Task<MediaInfo> Run(string mediaFile)
        {
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, true, 0d, 1d, 1d, TimeSpan.Zero, ProgressStatus.Active));
            var measure = new Stopwatch();
            measure.Start();
            var (exitcode, info) = await Task.Run(() => Extract(mediaFile));
            measure.Stop();
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, 1d, 1d, measure.Elapsed, ProgressStatus.Success));
            return exitcode == 0 ? info : throw new Exception($"Process failed with exit code: {exitcode}");
        }

        public Task Abort()
        {
            return Task.CompletedTask;
        }

        public Task Cleanup()
        {
            return Task.CompletedTask;
        }
    }
}