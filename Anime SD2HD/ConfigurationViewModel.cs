using Microsoft.UI.Dispatching;
using System;
using System.Linq;
using System.Windows.Input;
using Windows.Storage.Pickers;

namespace AnimeSD2HD
{
    internal class ConfigurationViewModel : ViewModel
    {
        // TODO: get from application directory ...
        //private readonly string ffmpeg = @"D:\Video Tools\ffmpeg.exe";
        //private readonly string waifu = @"D:\Video Tools\waifu2x-ncnn-vulkan\waifu2x-ncnn-vulkan.exe";

        private readonly IExternalProcess<MediaInfo, string> mediaInfoExtractor;
        private readonly IExternalProcess<Void, ImageExtractorArgs> imageExtractor;
        private readonly IExternalProcess<Void, ImageUpscalerArgs> imageUpscaler;
        private readonly IExternalProcess<Void, ImageRecompressMuxerArgs> mediaRecompressMuxer;

        public ConfigurationViewModel(
            IExternalProcess<MediaInfo, string> mediaInfoExtractor,
            IExternalProcess<Void, ImageExtractorArgs> imageExtractor,
            IExternalProcess<Void, ImageUpscalerArgs> imageUpscaler,
            IExternalProcess<Void, ImageRecompressMuxerArgs> mediaRecompressMuxer)
        {
            VideoCRF = 20;
            ScalingFactor = 2;
            OutputMediaHeight = 1080;
            DisplayAspectRatio = (16, 9);
            UpscaleModel = AvailableUpscaleModels.First();
            OpenFileCommand = new RelayCommand(OpenFileExecute, OpenFileCanExecute);
            StartStopCommand = new RelayCommand(StartExecute, StartCanExecute);
            StartStopLabel = "Start";

            this.mediaInfoExtractor = mediaInfoExtractor;
            this.imageExtractor = imageExtractor;
            this.imageExtractor.ProgressUpdate += ImageExtractorProgressHandler;
            this.imageUpscaler = imageUpscaler;
            this.imageUpscaler.ProgressUpdate += ImageUpscalerProgressHandler;
            this.mediaRecompressMuxer = mediaRecompressMuxer;
            this.mediaRecompressMuxer.ProgressUpdate += MediaRecompressMuxerProgressHandler;
        }

        public DispatcherQueue Dispatcher { get; set; }
        public FileOpenPicker MediaFilePicker { get; set; }

        private void ImageExtractorProgressHandler(object sender, ProgressInfoViewModel progress)
        {
            Dispatcher.TryEnqueue(() => ImageExtractorProgress = progress);
        }

        private void ImageUpscalerProgressHandler(object sender, ProgressInfoViewModel progress)
        {
            Dispatcher.TryEnqueue(() => ImageUpscalerProgress = progress);
        }

        private void MediaRecompressMuxerProgressHandler(object sender, ProgressInfoViewModel progress)
        {
            Dispatcher.TryEnqueue(() => MediaRecompressMuxerProgress = progress);
        }

        private bool OpenFileCanExecute(object parameter)
        {
            return true;
        }

        private async void OpenFileExecute(object parameter)
        {
            var file = await MediaFilePicker.PickSingleFileAsync();
            if (file != null)
            {
                var info = await mediaInfoExtractor.Run(file.Path);
                InputMediaFile = file.Path;
                FrameRate = info.FrameRate;
                InputMediaWidth = info.VideoWidth;
                InputMediaHeight = info.VideoHeight;
                DisplayAspectRatio = info.DisplayAspectRatio;
            }
        }

        public ICommand OpenFileCommand
        {
            get => GetPropertyValue<ICommand>();
            set => SetPropertyValue(value);
        }

        public ProgressInfoViewModel ImageExtractorProgress
        {
            get => GetPropertyValue<ProgressInfoViewModel>();
            set => SetPropertyValue(value);
        }

        public ProgressInfoViewModel ImageUpscalerProgress
        {
            get => GetPropertyValue<ProgressInfoViewModel>();
            set => SetPropertyValue(value);
        }

        public ProgressInfoViewModel MediaRecompressMuxerProgress
        {
            get => GetPropertyValue<ProgressInfoViewModel>();
            set => SetPropertyValue(value);
        }

        private bool StartCanExecute(object parameter)
        {
            return true;
        }

        private async void StartExecute(object parameter)
        {
            StartStopLabel = "Stop"; // Properties.Resources.StartButtonLabel;
            StartStopCommand = new RelayCommand(StopExecute, StopCanExecute);
            //imageExtractor.ProgressUpdate += (_, args) => { };
            await imageExtractor.Run(new ImageExtractorArgs(InputMediaFile, ExtractionDirectory, ExtractMediaWidth, ExtractMediaHeight));
            //imageUpscaler.ProgressUpdate += (_, args) => { };
            await imageUpscaler.Run(new ImageUpscalerArgs(ExtractionDirectory, UpscaleDirectory, UpscaleModel.ID, ScalingFactor, DenoiseLevel));
            //mediaRecompressMuxer.ProgressUpdate += (_, args) => { };
            await mediaRecompressMuxer.Run(new ImageRecompressMuxerArgs(InputMediaFile, UpscaleDirectory, OutputMediaFile, FrameRate, SubtitleCodec, AudioCodec, AudioBitrate, VideoCodec, VideoCRF, VideoPreset, VideoTuning));
            // TODO: clean-up ...
            StopExecute(parameter);
        }

        private bool StopCanExecute(object parameter)
        {
            return true;
        }

        private void StopExecute(object parameter)
        {
            imageExtractor.Abort();
            imageUpscaler.Abort();
            mediaRecompressMuxer.Abort();
            StartStopCommand = new RelayCommand(StartExecute, StartCanExecute);
            StartStopLabel = "Start";
        }

        public ICommand StartStopCommand
        {
            get => GetPropertyValue<ICommand>();
            set => SetPropertyValue(value);
        }

        public string StartStopLabel
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string InputMediaFile
        {
            get => GetPropertyValue<string>();
            set
            {
                SetPropertyValue(value);
                OutputMediaFile = value + "ᴴᴰ";
            }
        }

        private string ExtractionDirectory => InputMediaFile + ".extracted";

        private string UpscaleDirectory => InputMediaFile + ".upscaled";

        public string OutputMediaFile
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        private (int width, int height) DisplayAspectRatio
        {
            get => GetPropertyValue<(int width, int height)>();
            set
            {
                SetPropertyValue(value);
                RaisePropertyChanged(nameof(OutputMediaWidth), nameof(ExtractMediaWidth), nameof(ExtractMediaHeight));
            }
        }

        public string FrameRate
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public int InputMediaWidth
        {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }

        public int InputMediaHeight
        {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }

        public int ExtractMediaWidth => ScalingFactor > 0 ? OutputMediaWidth / ScalingFactor : 0;

        public int ExtractMediaHeight => ScalingFactor > 0 ? OutputMediaHeight / ScalingFactor : 0;

        public int OutputMediaWidth {
            get {
                var width = DisplayAspectRatio.height > 0 ? DisplayAspectRatio.width * OutputMediaHeight / DisplayAspectRatio.height : 0;
                return width % 2 == 0 ? width : width + 1;
            }
        }

        public int OutputMediaHeight
        {
            get => GetPropertyValue<int>();
            set
            {
                SetPropertyValue(value);
                RaisePropertyChanged(nameof(OutputMediaWidth), nameof(ExtractMediaWidth), nameof(ExtractMediaHeight));
            }
        }

        public WaifuModel[] AvailableUpscaleModels { get; } = WaifuModels.AvailableModels;

        public WaifuModel UpscaleModel
        {
            get => GetPropertyValue<WaifuModel>();
            set => SetPropertyValue(value);
        }

        public int DenoiseLevel
        {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }

        public int ScalingFactor
        {
            get => GetPropertyValue<int>();
            set
            {
                SetPropertyValue(value);
                RaisePropertyChanged(nameof(ExtractMediaWidth), nameof(ExtractMediaHeight));
            }
        }

        public string VideoCodec
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string VideoPreset
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string VideoTuning
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public int VideoCRF
        {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }

        public string AudioCodec
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string AudioBitrate
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string SubtitleCodec
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }
    }
}