using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace AnimeSD2HD
{
    internal sealed partial class MainWindow : Window
    {
        private ConfigurationViewModel Configuration { get; }

        public MainWindow(ConfigurationViewModel configuration)
        {
            Title = "Animeᴴᴰ (Super Resolution SD ➔ HD Upscaler GUI)";
            Configuration = configuration;
            Configuration.Dispatcher = DispatcherQueue;
            Configuration.MediaFilePicker = CreateFilePicker(".mkv", ".mp4");
            InitializeComponent();
        }

        private FileOpenPicker CreateFilePicker(params string[] extensions)
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            foreach(var extension in extensions)
            {
                picker.FileTypeFilter.Add(extension);
            }
            return picker;
        }
    }
}