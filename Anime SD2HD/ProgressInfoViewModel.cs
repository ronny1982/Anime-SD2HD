using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace AnimeSD2HD
{
    internal enum ProgressStatus
    {
        None,
        Active,
        Success,
        Failed
    }

    internal class ProgressInfoViewModel : ViewModel
    {
        private static readonly Dictionary<ProgressStatus, Brush> StatusColorMap = new()
        {
            [ProgressStatus.None] = new SolidColorBrush(Colors.Gray),
            [ProgressStatus.Active] = new SolidColorBrush(Colors.RoyalBlue),
            [ProgressStatus.Success] = new SolidColorBrush(Colors.LimeGreen),
            [ProgressStatus.Failed] = new SolidColorBrush(Colors.Firebrick)
        };

        private static readonly Dictionary<ProgressStatus, string> StatusTextMap = new()
        {
            [ProgressStatus.None] = "⬤",
            [ProgressStatus.Active] = "⏳",
            [ProgressStatus.Success] = "✔",
            [ProgressStatus.Failed] = "❌",
        };

        public ProgressInfoViewModel(bool show, bool indeterminate, double min, double max, double value, TimeSpan elapsed, ProgressStatus status)
        {
            VisibleState = show ? Visibility.Visible : Visibility.Collapsed;
            StatusColor = StatusColorMap[status];
            StatusText = StatusTextMap[status];
            IsIndeterminate = indeterminate;
            Minimum = min;
            Maximum = max;
            Value = value;
            // TODO: handle minimum != 0 ...
            var remaining = (IsIndeterminate || Value == 0d ? -TimeSpan.Zero : elapsed * (Maximum - Value) / Value);
            //var duration = (IsIndeterminate || Value == 0d ? -TimeSpan.Zero : elapsed * Maximum / Value);
            Time = string.Format(@"Elapsed: {0:hh\:mm\:ss} / Remaining: {1:hh\:mm\:ss}", elapsed, remaining);
        }

        public Visibility VisibleState
        {
            get => GetPropertyValue<Visibility>();
            private set => SetPropertyValue(value);
        }

        public Brush StatusColor
        {
            get => GetPropertyValue<Brush>();
            private set => SetPropertyValue(value);
        }

        public string StatusText
        {
            get => GetPropertyValue<string>();
            private set => SetPropertyValue(value);
        }

        public bool IsIndeterminate
        {
            get => GetPropertyValue<bool>();
            private set => SetPropertyValue(value);
        }

        public double Minimum
        {
            get => GetPropertyValue<double>();
            private set => SetPropertyValue(value);
        }

        public double Maximum
        {
            get => GetPropertyValue<double>();
            private set => SetPropertyValue(value);
        }

        public double Value
        {
            get => GetPropertyValue<double>();
            private set => SetPropertyValue(value);
        }

        public string Time
        {
            get => GetPropertyValue<string>();
            private set => SetPropertyValue(value);
        }
    }
}