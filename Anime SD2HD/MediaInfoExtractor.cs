using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record MediaInfo(int VideoWidth, int VideoHeight, (int width, int height) DisplayAspectRatio, string FrameRate);

    internal class MediaInfoExtractor : IExternalProcess<MediaInfo, string>
    {
        private readonly string ffprobe;

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
            process.OutputDataReceived += (_, args) => StandardOutputReceived?.Invoke(this, args.Data);
            process.ErrorDataReceived += (_, args) => StandardErrorReceived?.Invoke(this, args.Data);
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var json = JsonDocument.Parse(stdout);
            var stream = json.RootElement.GetProperty("streams")[0];
            var dar = stream.GetProperty("display_aspect_ratio").GetString().Split(':').Select(number => Convert.ToInt32(number));

            return (process.ExitCode, new MediaInfo(
                stream.GetProperty("width").GetInt32(),
                stream.GetProperty("height").GetInt32(),
                (dar.First(), dar.Last()),
                stream.GetProperty("r_frame_rate").GetString()));
        }

        public async Task<MediaInfo> Run(string mediaFile)
        {
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, true, 0d, 1d, 1d, TimeSpan.Zero));
            var measure = new Stopwatch();
            measure.Start();
            var (exitcode, info) = await Task.Run(() => Extract(mediaFile));
            measure.Stop();
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, 1d, 1d, measure.Elapsed));
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