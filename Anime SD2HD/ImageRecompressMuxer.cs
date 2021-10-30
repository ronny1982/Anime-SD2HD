﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal record ImageRecompressMuxerArgs(string MediaFile, string InputDirectory, string OutputFile, string FrameRate, string SubtitleCodec, string AudioCodec, string AudioBitrate, string VideoCodec, int VideoCRF, string VideoPreset, string VideoTuning);

    internal class ImageRecompressMuxer : IExternalProcess<Void, ImageRecompressMuxerArgs>
    {
        private readonly string ffmpeg;
        private const string globImage = "*.png";
        private readonly Regex rgxFrame = new(@"frame=\s*(?<FRAME>\d+)");

        public event EventHandler<ProgressInfoViewModel> ProgressUpdate;
        public event EventHandler<string> StandardOutputReceived;
        public event EventHandler<string> StandardErrorReceived;

        public ImageRecompressMuxer(string application)
        {
            ffmpeg = application;
        }

        private (int exitcode, TimeSpan runtime) Extract(ImageRecompressMuxerArgs args)
        {
            var measure = new Stopwatch();
            var imageCount = Directory.EnumerateFiles(args.InputDirectory, globImage).Count();
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(ffmpeg)
            {
                Arguments = $"-hide_banner -framerate {args.FrameRate} -i \"{Path.Join(args.InputDirectory, "%06d.png")}\" -i \"{args.MediaFile}\" -map 0:v -map 1:a? -map 1:s? -f matroska -c:s {args.SubtitleCodec} -c:a {args.AudioCodec} -b:a {args.AudioBitrate} -c:v {args.VideoCodec} -crf {args.VideoCRF} -preset {args.VideoPreset} -tune {args.VideoTuning} -pix_fmt yuv420p -y \"{args.OutputFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.OutputDataReceived += (_, args) => StandardOutputReceived?.Invoke(this, args.Data);
            process.ErrorDataReceived += (_, args) =>
            {
                var matchFrame = rgxFrame.Match(args.Data ?? string.Empty);
                if (matchFrame.Success && matchFrame.Groups.TryGetValue("FRAME", out var group) && int.TryParse(group.Value, out var frame))
                {
                    ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, imageCount, frame, measure.Elapsed));
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
            process.WaitForExit();
            measure.Stop();
            return (process.ExitCode, measure.Elapsed);
        }

        public async Task<Void> Run(ImageRecompressMuxerArgs args)
        {
            var (exitcode, runtime) = await Task.Run(() => Extract(args));
            ProgressUpdate?.Invoke(this, new ProgressInfoViewModel(true, false, 0d, 1d, 1d, runtime));
            return exitcode == 0 ? Void.Default : throw new Exception($"Process failed with exit code: {exitcode}");
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