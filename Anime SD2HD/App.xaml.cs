using Microsoft.UI.Xaml;
using System.IO;
using System.Reflection;

namespace AnimeSD2HD
{
    public partial class App : Application
    {
        private const string toolsFolder = "Tools";
        private static readonly (string ffprobe, string ffmpeg, string waifu) toolsFolders = (
            Path.Join(toolsFolder, "ffprobe.exe"),
            Path.Join(toolsFolder, "ffmpeg.exe"),
            Path.Join(toolsFolder, "waifu2x-ncnn-vulkan.exe"));
        private Window window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var config = new ConfigurationViewModel(
                new MediaInfoExtractor(Path.Join(baseDirectory, toolsFolders.ffprobe)),
                new ImageExtractor(Path.Join(baseDirectory, toolsFolders.ffmpeg)),
                new ImageUpscaler(Path.Join(baseDirectory, toolsFolders.waifu)),
                new ImageRecompressMuxer(Path.Join(baseDirectory, toolsFolders.ffmpeg)));
            window = new MainWindow(config);
            window.Activate();
        }
    }
}