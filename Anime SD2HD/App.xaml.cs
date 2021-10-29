using Microsoft.UI.Xaml;
using System.IO;

namespace AnimeSD2HD
{
    public partial class App : Application
    {
        private Window window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // TODO: find all external binaries relative to current application directory ...
            var dir = @"D:\Video Tools";
            var config = new ConfigurationViewModel(
                new MediaInfoExtractor(Path.Join(dir, "ffprobe.exe")),
                new ImageExtractor(Path.Join(dir, "ffmpeg.exe")),
                new ImageUpscaler(Path.Join(dir, "waifu2x-ncnn-vulkan", "waifu2x-ncnn-vulkan.exe")),
                new ImageRecompressMuxer(Path.Join(dir, "ffmpeg.exe")));
            window = new MainWindow(config);
            window.Activate();
        }
    }
}