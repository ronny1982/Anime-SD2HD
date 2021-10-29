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

        public MediaInfoExtractor(string application)
        {
            ffprobe = application;
        }

        private MediaInfo Extract(string mediaFile)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(ffprobe)
            {
                Arguments = $"-hide_banner -i \"{mediaFile}\" -select_streams v:0 -show_entries format=duration:stream=duration,width,height,r_frame_rate,display_aspect_ratio,sample_aspect_ratio -loglevel quiet -of json",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var json = JsonDocument.Parse(stdout);
            var stream = json.RootElement.GetProperty("streams")[0];
            var dar = stream.GetProperty("display_aspect_ratio").GetString().Split(':').Select(number => Convert.ToInt32(number));

            return new MediaInfo(
                stream.GetProperty("width").GetInt32(),
                stream.GetProperty("height").GetInt32(),
                (dar.First(), dar.Last()),
                stream.GetProperty("r_frame_rate").GetString());
        }

        public async Task<MediaInfo> Run(string mediaFile)
        {
            var task = Task.Run(() => Extract(mediaFile));
            await task;
            return task.Result;
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