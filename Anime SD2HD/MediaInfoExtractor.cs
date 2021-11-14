using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record MediaInfo(int VideoWidth, int VideoHeight, Rational DisplayAspectRatio, Rational FrameRate);

    internal class MediaInfoExtractor : IExternalProcess<MediaInfo, string>
    {
        private readonly string ffprobe;
        private readonly Rational[] commonDAR = new[]
        {
            new Rational(1, 1),     // Square
            new Rational(4, 3),     // Silent Film/NTSC
            new Rational(137, 100), // Academy Ratio
            new Rational(143, 100), // IMAX
            new Rational(3, 2),     // Classic 35mm
            new Rational(16, 10),   // ...
            new Rational(7, 4),     // Metroscope
            new Rational(16, 9),    // ...
            new Rational(185, 100), // Vistavision
            new Rational(2, 1),     // Panascope & RED
            new Rational(22, 10),   // Todd AI
            new Rational(21, 9),    // ...
            new Rational(235, 100), // Cinemascope
            new Rational(239, 100), // Theatrical & Blu-ray
            new Rational(255, 100), // Vintage Cinemascope
            new Rational(275, 100), // Ultra Panavision
            new Rational(276, 100), // MGM Camera 65
            new Rational(3, 1),     // Extreme Scope
            new Rational(32, 9),    // ...
            new Rational(4, 1),     // PolyVision
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
            var mediaDAR = Rational.Parse(stream.GetProperty("display_aspect_ratio").GetString());
            var dar = commonDAR.OrderBy(curDAR => Math.Abs(curDAR - mediaDAR)).FirstOrDefault(curDAR => Math.Abs(curDAR - mediaDAR) < 0.01d) ?? mediaDAR;
            var framerate = Rational.Parse(stream.GetProperty("r_frame_rate").GetString());

            return (process.ExitCode, new MediaInfo(stream.GetProperty("width").GetInt32(), stream.GetProperty("height").GetInt32(), dar, framerate));
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
    }
}