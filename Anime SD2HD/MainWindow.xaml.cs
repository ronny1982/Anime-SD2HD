using AnimeSD2HD.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AnimeSD2HD
{
    internal sealed partial class MainWindow : Window
    {
        const int IMAGE_ICON = 1;
        const int LR_LOADFROMFILE = 0x10;
        const int WM_SETICON = 0x80;
        const int ICON_SMALL = 0;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadImage(IntPtr hInst, string lpsz, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);

        private ConfigurationViewModel Configuration { get; }

        public MainWindow(ConfigurationViewModel configuration)
        {
            LoadIcon(@"Properties\App.ico");
            Title = Resources.WindowTitle;
            Configuration = configuration;
            Configuration.Dispatcher = DispatcherQueue;
            Configuration.MediaFilePicker = CreateFilePicker(".mkv", ".mp4");
            InitializeComponent();
        }

        private void LoadIcon(string identifier)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var icon = LoadImage(IntPtr.Zero, identifier, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
            SendMessage(hwnd, WM_SETICON, ICON_SMALL, icon);
        }

        private FileOpenPicker CreateFilePicker(params string[] extensions)
        {
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
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