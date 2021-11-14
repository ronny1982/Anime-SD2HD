using AnimeSD2HD.Properties;
using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Linq;
using Windows.Storage.Pickers;

namespace AnimeSD2HD
{
    internal class ConfigurationViewModel : ViewModel
    {
        private readonly IResidualsCleaner cleaner;
        private readonly IExternalProcess<MediaInfo, string> mediaInfoExtractor;
        private readonly IExternalProcess<Void, ImageExtractorArgs> imageExtractor;
        private readonly IExternalProcess<Void, ImageUpscalerArgs> imageUpscaler;
        private readonly IExternalProcess<Void, ImageRecompressMuxerArgs> mediaRecompressMuxer;

        public ConfigurationViewModel(
            IResidualsCleaner cleaner,
            IExternalProcess<MediaInfo, string> mediaInfoExtractor,
            IExternalProcess<Void, ImageExtractorArgs> imageExtractor,
            IExternalProcess<Void, ImageUpscalerArgs> imageUpscaler,
            IExternalProcess<Void, ImageRecompressMuxerArgs> mediaRecompressMuxer)
        {
            VideoCRF = 20;
            ScalingFactor = 2;
            OutputMediaHeight = 1080;
            DisplayAspectRatio = new DisplayAspectRatio(16, 9);
            UpscaleModel = AvailableUpscaleModels.First();
            OpenSourceMediaCommand = new RelayCommand(Resources.OpenSourceMediaButton_Label, OpenFileExecute, OpenFileCanExecute);
            OpenTargetMediaCommand = new RelayCommand(Resources.OpenTargetMediaButton_Label, _ => {}, _ => false);
            StartCommand = new RelayCommand(Resources.StartButton_Label, StartExecute, StartCanExecute);
            StopCommand = new RelayCommand(Resources.StopButton_Label, StopExecute, StopCanExecute);
            StartStopCommand = StartCommand;

            this.cleaner = cleaner;
            this.mediaInfoExtractor = mediaInfoExtractor;
            this.mediaInfoExtractor.StandardOutputReceived += ProcessStandardOutputReceived;
            this.mediaInfoExtractor.StandardErrorReceived += ProcessStandardErrorReceived;
            this.imageExtractor = imageExtractor;
            this.imageExtractor.StandardOutputReceived += ProcessStandardOutputReceived;
            this.imageExtractor.StandardErrorReceived += ProcessStandardErrorReceived;
            this.imageExtractor.ProgressUpdate += ImageExtractorProgressHandler;
            this.imageUpscaler = imageUpscaler;
            this.imageUpscaler.StandardOutputReceived += ProcessStandardOutputReceived;
            this.imageUpscaler.StandardErrorReceived += ProcessStandardErrorReceived;
            this.imageUpscaler.ProgressUpdate += ImageUpscalerProgressHandler;
            this.mediaRecompressMuxer = mediaRecompressMuxer;
            this.mediaRecompressMuxer.StandardOutputReceived += ProcessStandardOutputReceived;
            this.mediaRecompressMuxer.StandardErrorReceived += ProcessStandardErrorReceived;
            this.mediaRecompressMuxer.ProgressUpdate += MediaRecompressMuxerProgressHandler;
        }

        public DispatcherQueue Dispatcher { get; set; }
        public FileOpenPicker MediaFilePicker { get; set; }

        private void ProcessStandardOutputReceived(object sender, string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                Dispatcher.TryEnqueue(() => ConsoleOutput += $"[{sender.GetType().Name}] <STDOUT> {data.TrimEnd() + Environment.NewLine}");
            }
        }

        private void ProcessStandardErrorReceived(object sender, string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                Dispatcher.TryEnqueue(() => ConsoleOutput += $"[{sender.GetType().Name}] <STDERR> {data.TrimEnd() + Environment.NewLine}");
            }
        }

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
            return IsIdle;
        }

        private async void OpenFileExecute(object parameter)
        {
            var file = await MediaFilePicker.PickSingleFileAsync();
            if (file != null && file.Path != InputMediaFile)
            {
                ResetProgress();
                var info = await mediaInfoExtractor.Run(file.Path);
                InputMediaFile = file.Path;
                FrameRate = info.FrameRate;
                InputMediaWidth = info.VideoWidth;
                InputMediaHeight = info.VideoHeight;
                DisplayAspectRatio = info.DisplayAspectRatio;
            }
            StartCommand.RaiseCanExecuteChanged();
        }

        private void ResetProgress()
        {
            ConsoleOutput = string.Empty;
            ImageExtractorProgress = new ProgressInfoViewModel(true, false, 0d, 0d, 1d, TimeSpan.Zero, ProgressStatus.None);
            ImageUpscalerProgress = new ProgressInfoViewModel(true, false, 0d, 0d, 1d, TimeSpan.Zero, ProgressStatus.None);
            MediaRecompressMuxerProgress = new ProgressInfoViewModel(true, false, 0d, 0d, 1d, TimeSpan.Zero, ProgressStatus.None);
        }

        public string ConsoleOutput
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public RelayCommand OpenSourceMediaCommand
        {
            get => GetPropertyValue<RelayCommand>();
            set => SetPropertyValue(value);
        }

        public RelayCommand OpenTargetMediaCommand
        {
            get => GetPropertyValue<RelayCommand>();
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

        public bool IsIdle
        {
            get => StartStopCommand == StartCommand;
            set
            {
                StartStopCommand = value ? StartCommand : StopCommand;
                OpenSourceMediaCommand.RaiseCanExecuteChanged();
                OpenTargetMediaCommand.RaiseCanExecuteChanged();
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }

        private bool StartCanExecute(object _)
        {
            return IsIdle && !string.IsNullOrWhiteSpace(InputMediaFile);
        }

        private async void StartExecute(object _)
        {
            ResetProgress();
            IsIdle = false;
            try
            {
                await imageExtractor.Run(new ImageExtractorArgs(InputMediaFile, ExtractionDirectory, ExtractMediaWidth, ExtractMediaHeight));
                ConsoleOutput += Environment.NewLine;
                await imageUpscaler.Run(new ImageUpscalerArgs(ExtractionDirectory, UpscaleDirectory, UpscaleModel.ID, ScalingFactor, DenoiseLevel));
                ConsoleOutput += Environment.NewLine;
                await mediaRecompressMuxer.Run(new ImageRecompressMuxerArgs(InputMediaFile, UpscaleDirectory, OutputMediaFile, FrameRate, SubtitleCodec, AudioCodec, AudioBitrate, VideoCodec, VideoCRF, VideoPreset, VideoTuning));
            }
            catch
            {
                // TODO: error handling ...
            }
            finally
            {
                cleaner.Cleanup(ExtractionDirectory, UpscaleDirectory);
                IsIdle = true;
            }
        }

        private bool StopCanExecute(object _)
        {
            return !IsIdle;
        }

        private void StopExecute(object _)
        {
            try
            {
                imageExtractor.Abort();
                imageUpscaler.Abort();
                mediaRecompressMuxer.Abort();
            }
            catch
            {
                // TODO: error handling ...
            }
        }

        public void OnClose()
        {
            if (StopCanExecute(null))
            {
                StopExecute(null);
            }
        }

        private RelayCommand StartCommand { get; }
        private RelayCommand StopCommand { get; }

        public RelayCommand StartStopCommand
        {
            get => GetPropertyValue<RelayCommand>();
            private set
            {
                SetPropertyValue(value);
                RaisePropertyChanged(nameof(IsIdle));
            }
        }

        public string InputMediaFile
        {
            get => GetPropertyValue<string>();
            set
            {
                SetPropertyValue(value);
                OutputMediaFile = string.IsNullOrWhiteSpace(value) ? string.Empty : Path.ChangeExtension(value, ".mkvᴴᴰ");
            }
        }

        private string ExtractionDirectory => InputMediaFile + ".extracted";

        private string UpscaleDirectory => InputMediaFile + ".upscaled";

        public string OutputMediaFile
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        private DisplayAspectRatio DisplayAspectRatio
        {
            get => GetPropertyValue<DisplayAspectRatio>();
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
                var width = (int)Math.Round(OutputMediaHeight * DisplayAspectRatio.Ratio, MidpointRounding.AwayFromZero);
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