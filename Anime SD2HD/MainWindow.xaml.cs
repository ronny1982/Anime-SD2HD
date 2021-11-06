using AnimeSD2HD.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AnimeSD2HD
{
    internal sealed partial class MainWindow : Window
    {
        private ConfigurationViewModel Configuration { get; }

        public MainWindow(ConfigurationViewModel configuration)
        {
            Title = Resources.WindowTitle;
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

        private void TextChangedHandler(object sender, TextChangedEventArgs _)
        {
            if (sender as TextBox == ConsoleTextBox && ConsoleTextBox.FocusState == FocusState.Unfocused)
            {
                ConsoleScrollViewer.ScrollToVerticalOffset(ConsoleScrollViewer.ScrollableHeight);
            }
        }

        private void WindowClosedHandler(object sender, WindowEventArgs args)
        {
            args.Handled = !Configuration.IsIdle;
        }
    }
}